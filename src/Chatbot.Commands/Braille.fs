namespace Commands

module private Helper =

    open System.Text.RegularExpressions

    open FsHttp
    open FsHttp.Request
    open FsHttp.Response

    let private png: byte[] = [| 0x89uy ; 0x50uy ; 0x4Euy ; 0x47uy |]
    let private jpg: byte[] = [| 0xFFuy; 0xD8uy; 0xFFuy; 0xE0uy; 0x00uy; 0x10uy; 0x4Auy; 0x46uy; 0x49uy; 0x46uy |]
    let private bmp: byte[] = [| 0x42uy ; 0x4Duy |]
    let private webp: (byte[] * byte[]) = [| 0x52uy ; 0x49uy ; 0x46uy ; 0x46uy |], [| 0x57uy ; 0x45uy ; 0x42uy ; 0x50uy |]

    let private isPng (bytes: byte[]) = bytes.Length > 3 && bytes[0..3] = png
    let private isJpg (bytes: byte[]) = bytes.Length > 8 && bytes[0..8] = jpg
    let private isBmp (bytes: byte[]) = bytes.Length > 1 && bytes[0..1] = bmp
    let private isWebp (bytes: byte[]) = bytes.Length > 11 && (bytes[0..3], bytes[8..11]) = webp
    let private isImage (bytes: byte[]) = isPng bytes || isJpg bytes || isBmp bytes || isWebp bytes

    let getImage url =
        async {
            use! response =
                http {
                    GET url
                    Accept "image/*"
                }
                |> sendAsync

            let pattern = @"^image\/(jpeg|jpg|jfif|pjpeg|pjp|png|bmp|webp|avif|apng)$"

            match toResult response with
            | Error _ -> return None
            | Ok response ->
                let! bytes = response |> toBytesAsync

                if
                    response.originalHttpResponseMessage.Content.Headers.Contains("Content-Type")
                    && Regex.IsMatch(response.originalHttpResponseMessage.Content.Headers.ContentType.MediaType, pattern)
                    || isImage bytes
                then
                    return Some bytes
                else
                    return None
        }

    let getPixelLuminance (pixel: SkiaSharp.SKColor) =
        0.2126 * float pixel.Red +
        0.7152 * float pixel.Green +
        0.0722 * float pixel.Blue |> int

module private Dithering =

    open SkiaSharp

    let private clamp = fun v -> System.Math.Clamp(v, 0, 255)

    let private difference (a: SKColor) (b: SKColor) =
        float a.Red - float b.Red,
        float a.Green - float b.Green,
        float a.Blue - float b.Blue

    let private findClosestPaletteColor (pixel: SKColor) (threshold: int)  =
        let luminance = Helper.getPixelLuminance pixel

        if luminance > threshold then
            new SKColor(255uy, 255uy, 255uy)
        else
            new SKColor(0uy, 0uy, 0uy)

    let private addError (bitmap: SKBitmap) (x: int) (y: int) (error: float * float * float) (factor: float) =
        if x >= 0 && x < bitmap.Width && y >= 0 && y < bitmap.Height then
            let pixel = bitmap.GetPixel(x, y)
            let errorRed, errorGreen, errorBlue = error
            let r,g,b =
                clamp (float pixel.Red + errorRed * factor |> int) |> byte,
                clamp (float pixel.Green + errorGreen * factor |> int) |> byte,
                clamp (float pixel.Blue + errorBlue * factor |> int) |> byte

            bitmap.SetPixel(x, y, new SKColor(r, g, b))

    let floydSteinberg (bitmap: SKBitmap) =
        let threshold = 128

        for y in 0 .. bitmap.Height - 1 do
            for x in 0 .. bitmap.Width - 1 do
                let oldPixel = bitmap.GetPixel(x, y)
                let newPixel = findClosestPaletteColor oldPixel threshold
                bitmap.SetPixel(x, y, newPixel)
                let quantError = difference oldPixel newPixel

                addError bitmap (x + 1) y quantError (7.0 / 16.0)
                addError bitmap (x - 1) (y + 1) quantError (3.0 / 16.0)
                addError bitmap x (y + 1) quantError (5.0 / 16.0)
                addError bitmap (x + 1) (y + 1) quantError (1.0 / 16.0)

    let bayer (bitmap: SKBitmap) =
        let bayerMatrix = [|
            [| 0  ; 32 ;  8 ; 40 ;  2  ; 34 ; 10 ; 42 |]
            [| 48 ; 16 ; 56 ; 24 ; 50  ; 18 ; 58 ; 26 |]
            [| 12 ; 44 ;  4 ; 36 ; 14  ; 46 ;  6 ; 38 |]
            [| 60 ; 28 ; 52 ; 20 ; 62  ; 30 ; 54 ; 22 |]
            [|  3 ; 35 ; 11 ; 43 ;  1  ; 33 ;  9 ; 41 |]
            [| 51 ; 19 ; 59 ; 27 ; 49  ; 17 ; 57 ; 25 |]
            [| 15 ; 47 ;  7 ; 39 ; 13  ; 45 ;  5 ; 37 |]
            [| 63 ; 31 ; 55 ; 23 ; 61  ; 29 ; 53 ; 21 |]
        |]

        for y in 0 .. bitmap.Height - 1 do
            for x in 0 .. bitmap.Width - 1 do
                let threshold = bayerMatrix[y % bayerMatrix.Length][x % bayerMatrix.Length] * 255 / 64
                let oldPixel = bitmap.GetPixel(x, y)
                let newPixel = findClosestPaletteColor oldPixel threshold
                bitmap.SetPixel(x, y, newPixel)

[<AutoOpen>]
module Braille =

    open System.Text

    open SkiaSharp

    let private offsets = [
        (0, 0, 1)
        (0, 1, 2)
        (0, 2, 4)
        (1, 0, 8)
        (1, 1, 16)
        (1, 2, 32)
        (0, 3, 64)
        (1, 3, 128)
    ]

    [<Literal>]
    let private DefaultGreyscaleFunction = "luminance"

    let private greyscaleMode =
        [
            "luminance", (fun (s: SKColor) -> 0.2126 * float s.Red + 0.7152 * float s.Green + 0.0722 * float s.Blue)
            "average", (fun (s: SKColor) -> (float s.Red + float s.Green + float s.Blue) / 3.0)
            "max", (fun (s: SKColor) -> [ s.Red ; s.Green ; s.Blue ] |> List.max |> float)
            "lightness",
            fun (s: SKColor) ->
                let max = [ s.Red ; s.Green ; s.Blue ] |> List.max |> float
                let min = [ s.Red ; s.Green ; s.Blue ] |> List.min |> float
                (max + min) / 2.0

        ]
        |> Map.ofList

    let private toBraille (pixels: SKColor array array) mode (invert: bool) (monospace: bool) (threshold: float) : int =
        let predicateGreyscale = fun (pixel: SKColor) ->
            if invert then
                if pixel.Alpha > 128uy then
                    greyscaleMode.[mode] pixel > threshold
                else
                    greyscaleMode.[mode] pixel <= threshold
            else
                if pixel.Alpha > 128uy then
                    greyscaleMode.[mode] pixel <= threshold
                else
                    greyscaleMode.[mode] pixel > threshold

        let brailleValue =
            List.fold (fun acc (x, y, offsetValue) ->
                let pixel = pixels[x][y]
                if predicateGreyscale pixel then
                    acc + offsetValue
                else
                    acc
            ) 10240 offsets

        if brailleValue = 10240 && not <| monospace then
            10241
        else
            brailleValue

    let private roundUpToMultiple number x = number - (number % x)

    let private crop (bitmap: SKBitmap) =
        let croppedBitmap = new SKBitmap(bitmap.Width, bitmap.Width) // max height (*for clean look)
        let dest = new SKRectI(0, 0, croppedBitmap.Width, croppedBitmap.Width)

        use canvas = new SKCanvas(croppedBitmap)
        canvas.DrawBitmap(bitmap, dest)

        croppedBitmap

    let loadImage (bytes: byte array) width =
        use bitmap = SKBitmap.Decode(bytes)
        use bitmap = if bitmap.Height > bitmap.Width then crop bitmap else bitmap

        // 2 horizontal pixels per braille ascii symbol
        let width = width * 2
        let ratio = float bitmap.Width / float width
        // 4 vertical pixels per braille ascii symbol, max 15 lines (15 * 4 = 60 vertical dots)
        let height = min ((roundUpToMultiple (float bitmap.Height / ratio) 4.0) |> int) (15 * 4)

        bitmap.Resize(new SKImageInfo(width, height), SKSamplingOptions.Default)

    let private drawText (text: string) =
        use font = new SKFont(SKTypeface.FromFamilyName("Segoe UI"))
        font.Size <- 70f / 100f * 20f
        font.Embolden <- true

        use paint = new SKPaint()
        paint.IsAntialias <- true
        paint.IsStroke <- true
        paint.Color <- SKColors.Black
        paint.StrokeWidth <- 0.5f

        let width = System.Math.Clamp(font.MeasureText(text, paint) |> int, 50, 63)

        let info = new SKImageInfo(width, 12)
        use surface = SKSurface.Create(info)
        let canvas = surface.Canvas

        canvas.Clear(SKColors.White)

        canvas.DrawText(text, 0f, 11f, font, paint)

        surface.Snapshot()

    let private imageToBraille (bitmap: SKBitmap) (setting: string) (dithering: (SKBitmap -> unit) option) (invert: bool) (monospace: bool) =
        match dithering with
        | None -> ()
        | Some f -> f bitmap

        let sb = new StringBuilder()

        let threshold = 128

        for y in 0..4 .. bitmap.Height - 1 do
            for x in 0..2 .. bitmap.Width - 1 do
                let pixels = [|
                    [| bitmap.GetPixel(x, y) ; bitmap.GetPixel(x, y+1) ; bitmap.GetPixel(x, y+2) ;  bitmap.GetPixel(x, y+3) |]
                    [| bitmap.GetPixel(x+1, y) ; bitmap.GetPixel(x+1, y+1) ;  bitmap.GetPixel(x+1, y+2) ; bitmap.GetPixel(x+1, y+3) |]
                |]
                let brailleValue = toBraille pixels setting invert monospace threshold
                let braille = System.Convert.ToChar(brailleValue)
                sb.Append(braille) |> ignore

            sb.Append(" ") |> ignore
        sb.ToString()

    let private textToBraille (text: string) (setting: string) (dithering: (SKBitmap -> unit) option) (invert: bool) (monospace: bool) =
        let image = drawText text
        let bitmap = SKBitmap.Decode(image.Encode(SKEncodedImageFormat.Png, 80))

        imageToBraille bitmap setting dithering invert monospace

    let private internalBraille (url: string) (setting: string) (dithering: (SKBitmap -> unit) option) (invert: bool) (monospace: bool) =
        async {
            match! Helper.getImage url with
            | None -> return Message "Couldn't retrieve image, invalid url provided, or an unsupported image format is used"
            | Some image ->
                use bitmap = loadImage image 30
                let brailleAscii = imageToBraille bitmap setting dithering invert monospace
                return Message brailleAscii
        }

    let private brailleKeys = [ "greyscale" ; "dithering" ; "invert" ; "monospace" ]

    let private parseDitheringMethod =
        function
        | "floydsteinberg" | "fs" -> Some Dithering.floydSteinberg
        | "bayer" -> Some Dithering.bayer
        | _ -> None

    let braille args context =
        async {
            let keyValues = KeyValueParser.parse args brailleKeys
            let args = KeyValueParser.removeKeyValues args brailleKeys

            let greyscaleMode = keyValues |> Map.tryFind "greyscale" |?? DefaultGreyscaleFunction
            let dithering = keyValues |> Map.tryFind "dithering" |> Option.bind parseDitheringMethod
            let invert = keyValues |> Map.tryFind "invert" |> Option.bind (fun i -> Boolean.tryParse i) |?? true
            let monospace = keyValues |> Map.tryFind "monospace" |> Option.bind (fun i -> Boolean.tryParse i) |?? false

            let url =
                match args with
                | [] -> None
                | value :: _ ->
                    context.Emotes.MessageEmotes |> Map.tryFind value
                    |> Option.orElseWith (fun _ ->
                        match context.Emotes.TryFind value with
                        | Some emote -> Some emote.DirectUrl
                        | None -> Some value
                    )

            match url with
            | None -> return Message "Bad url/emote specified"
            | Some url -> return! internalBraille url greyscaleMode dithering invert monospace
        }

    let textToAscii args =
        let keyValues = KeyValueParser.parse args brailleKeys
        let args = KeyValueParser.removeKeyValues args brailleKeys

        let greyscaleMode = keyValues |> Map.tryFind "greyscale" |?? DefaultGreyscaleFunction
        let dithering = keyValues |> Map.tryFind "dithering" |> Option.bind parseDitheringMethod
        let invert = keyValues |> Map.tryFind "invert" |> Option.bind (fun i -> Boolean.tryParse i) |?? false
        let monospace = keyValues |> Map.tryFind "monospace" |> Option.bind (fun i -> Boolean.tryParse i) |?? true

        match args with
        | [] -> Message "No text specified"
        | text ->
            let text = System.String.Join(" ", text)
            let ascii = textToBraille text greyscaleMode dithering invert monospace
            Message ascii