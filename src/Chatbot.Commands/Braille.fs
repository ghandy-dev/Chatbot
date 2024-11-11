namespace Commands

[<AutoOpen>]
module Braille =

    open System.Text

    open SkiaSharp

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

    let private getImage url =
        async {
            use! response =
                http {
                    GET(url)
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
                    && RegularExpressions.Regex.IsMatch(response.originalHttpResponseMessage.Content.Headers.ContentType.MediaType, pattern)
                    || isImage bytes
                then
                    return Some bytes
                else
                    return None
        }

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
            "luminance", (fun (s: SKColor) -> (0.2126 * float s.Red) + (0.7152 * float s.Green) + (0.0722 * float s.Blue))
            "average", (fun (s: SKColor) -> (float (s.Red + s.Green + s.Blue)) / 3.0)
            "max", (fun (s: SKColor) -> [ s.Red ; s.Green ; s.Blue ] |> List.max |> float)
            "lightness",
            fun (s: SKColor) ->
                let max = [ s.Red ; s.Green ; s.Blue ] |> List.max |> float
                let min = [ s.Red ; s.Green ; s.Blue ] |> List.min |> float
                (max + min) / 2.0

        ]
        |> Map.ofList

    let private calcAverage (bitmap: SKBitmap) mode =
        let average =
            List.fold (fun acc y ->
                List.fold (fun innerAcc x ->
                    innerAcc + greyscaleMode.[mode] (bitmap.GetPixel(x, y)))
                    acc [ 0 .. bitmap.Width - 1 ])
                0.0 [ 0 .. bitmap.Height - 1 ]

        average / (float bitmap.Height * float bitmap.Width)

    let private toBraille (pixels: SKColor array array) mode (average: float) (invert: bool) : int =
        let predicate = fun (pixel: SKColor) -> if invert then greyscaleMode.[mode] pixel >= average else greyscaleMode.[mode] pixel <= average

        let brailleValue =
            List.fold (fun acc (x, y, offsetValue) ->
                let pixel = pixels[x][y]
                if pixel.Alpha > 128uy then
                    if predicate pixel then
                        acc + offsetValue
                    else
                        acc
                else
                    acc
            ) 10240 offsets

        // blank braille char
        if brailleValue = 10240 then
            10241 // single dot braille char
        else
            brailleValue

    let private roundUpToMultiple number x = number - (number % x)

    let private crop (bitmap: SKBitmap) =
        let croppedBitmap = new SKBitmap(bitmap.Width, bitmap.Width) // max height (*for clean look)
        let dest = new SKRectI(0, 0, croppedBitmap.Width, croppedBitmap.Width)

        use canvas = new SKCanvas(croppedBitmap)
        canvas.DrawBitmap(bitmap, dest)

        croppedBitmap

    let private getClosestPaletteColor (pixel: SKColor) average =
        let luminance = (0.2126 * float pixel.Red) + (0.7152 * float pixel.Green) + (0.0722 * float pixel.Blue)

        if luminance <= 128.0 then
            new SKColor(255uy, 255uy, 255uy)
        else
            new SKColor(0uy, 0uy, 0uy)

    let private colorDifference (a: SKColor) (b: SKColor) = new SKColor(a.Red - b.Red, a.Green - b.Green, a.Blue - b.Blue)

    let private colorAddError (bitmap: SKBitmap) (x: int) (y: int) (error: SKColor) (factor: float) =
        let clamp = fun v -> System.Math.Clamp(v, 0uy, 255uy)

        if (x >= 0 && x < bitmap.Width && y >= 0 && y < bitmap.Height) then
            let oldPixel = bitmap.GetPixel(x, y)
            let r, g, b =
                clamp (double oldPixel.Red + double error.Red * factor |> byte),
                clamp (double oldPixel.Green + double error.Green * factor |> byte),
                clamp (double oldPixel.Blue + double error.Blue * factor |> byte)

            bitmap.SetPixel(x, y, new SKColor(r, g, b))

    let floydSteinberdDithering (image: SKBitmap) threshold =
        for y in 0 .. image.Height - 1 do
            for x in 0 .. image.Width - 1 do
                let oldPixel = image.GetPixel(x, y)
                let newPixel = getClosestPaletteColor oldPixel threshold
                image.SetPixel(x, y, newPixel)
                let quantError = colorDifference oldPixel newPixel

                colorAddError image (x + 1) y quantError (7.0 / 16.0)
                colorAddError image (x - 1) (y + 1) quantError (3.0 / 16.0)
                colorAddError image x (y + 1) quantError (5.0 / 16.0)
                colorAddError image (x + 1) (y + 1) quantError (1.0 / 16.0)

    let loadImage (bytes: byte array) width =
        use bitmap = SKBitmap.Decode(bytes)
        use bitmap = if bitmap.Height > bitmap.Width then crop bitmap else bitmap

        // 2 horizontal pixels per braille ascii symbol
        let width = width * 2
        let ratio = float bitmap.Width / float width
        // 4 vertical pixels per braille ascii symbol, max 15 lines (15 * 4 = 60 vertical dots)
        let height = min ((roundUpToMultiple (float bitmap.Height / ratio) 4.0) |> int) (15 * 4)

        bitmap.Resize(new SKImageInfo(width, height), SKFilterQuality.High)

    let private imageToBraille (width: int) (image: byte array) (setting: string) (dithering: bool) (invert: bool) =
        use bitmap = loadImage image width
        let threshold = calcAverage bitmap setting

        if dithering then
            floydSteinberdDithering bitmap threshold

        let sb = new StringBuilder()

        for y in 0..4 .. bitmap.Height - 1 do
            for x in 0..2 .. bitmap.Width - 1 do
                let pixels = [|
                    [| bitmap.GetPixel(x, y) ; bitmap.GetPixel(x, y+1) ; bitmap.GetPixel(x, y+2) ;  bitmap.GetPixel(x, y+3) |]
                    [| bitmap.GetPixel(x+1, y) ; bitmap.GetPixel(x+1, y+1) ;  bitmap.GetPixel(x+1, y+2) ; bitmap.GetPixel(x+1, y+3) |]
                |]
                let brailleValue = toBraille pixels setting threshold invert
                let braille = System.Convert.ToChar(brailleValue)
                sb.Append(braille) |> ignore

            sb.Append(" ") |> ignore
        sb.ToString()

    let private internalBraille (url: string) (setting: string) (dithering: bool) (invert: bool) =
        async {
            match! getImage url with
            | None -> return Message "Couldn't retrieve image, invalid url provided, or an unsupported image format is used"
            | Some image ->
                let brailleAscii = imageToBraille 30 image setting dithering invert
                return Message brailleAscii
        }

    let brailleKeys = [ "greyscale" ; "dithering" ; "invert" ]

    let braille args context =
        async {
            let keyValues = KeyValueParser.parse args brailleKeys
            let args = KeyValueParser.removeKeyValues args brailleKeys

            let greyscaleMode = keyValues |> Map.tryFind "greyscale" |?? DefaultGreyscaleFunction
            let dithering = keyValues |> Map.tryFind "dithering" |> Option.bind (fun d -> Boolean.tryParse d) |?? false
            let invert = keyValues |> Map.tryFind "invert" |> Option.bind (fun i -> Boolean.tryParse i) |?? true

            let url =
                match args with
                | [] -> None
                | value :: _ ->
                    match context.Emotes.TryFind value with
                    | Some emote -> Some emote.DirectUrl
                    | None -> Some value

            match url with
            | None -> return Message "Bad url/emote specified"
            | Some url -> return! internalBraille url greyscaleMode dithering invert
        }
