namespace Commands

[<AutoOpen>]
module Braille =

    open System.Text

    open SkiaSharp

    open FsHttp
    open FsHttp.Request
    open FsHttp.Response

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

    let private colorIntensityFunctions =
        [
            "luminance", (fun (s: SKColor) -> (0.299 * float s.Red) + (0.587 * float s.Green) + (0.114 * float s.Blue))
            "average", (fun (s: SKColor) -> ((s.Red + s.Green + s.Blue) |> float) / 3.0)
            "max", (fun (s: SKColor) -> [ s.Red ; s.Green ; s.Blue ] |> List.max |> float)
            "lightness",
            fun (s: SKColor) ->
                let max = [ s.Red ; s.Green ; s.Blue ] |> List.max |> float
                let min = [ s.Red ; s.Green ; s.Blue ] |> List.min |> float
                (max + min) / 2.0

        ]
        |> Map.ofList

    let [<Literal>] private DefaultColorFunction = "luminance"

    let private getPixelValue (bitmap: SKBitmap) x y f =
        let pixel = bitmap.GetPixel(x, y)
        colorIntensityFunctions.[f] pixel

    let private calcAverage (bitmap: SKBitmap) f =
        let average =
            List.fold (fun acc y ->
                List.fold (fun innerAcc x ->
                    innerAcc + getPixelValue bitmap x y f
                ) acc [ 0 .. bitmap.Width - 1 ]
            ) 0.0 [ 0 .. bitmap.Height - 1 ]

        average / (float bitmap.Height * float bitmap.Width)

    let private toBraille (bitmap: SKBitmap) x y f average : int =
        let initialBrailleValue = 10240

        let brailleValue =
            offsets
            |> List.fold
                (fun acc (dx, dy, offsetValue) ->
                    if getPixelValue bitmap (x + dx) (y + dy) f > average then
                        acc + offsetValue
                    else
                        acc
                )
                initialBrailleValue

        if brailleValue = 10240 then 10241 else brailleValue

    let private roundUpToMultiple number x = number - (number % x)

    let private crop (bitmap: SKBitmap) =
        let croppedBitmap = new SKBitmap(bitmap.Width, bitmap.Width) // max height (*for clean look)
        let dest = new SKRectI(0, 0, croppedBitmap.Width, croppedBitmap.Width)

        use canvas = new SKCanvas(croppedBitmap)
        canvas.DrawBitmap(bitmap, dest)

        croppedBitmap

    let private imageToBraille width (image: byte array) setting =
        use bitmap = SKBitmap.Decode(image)

        use bitmap = if bitmap.Height > bitmap.Width then crop bitmap else bitmap

        // 2 pixels per braille ascii symbol
        let width = width * 2
        let ratio = float bitmap.Width / (float width)

        // 4 pixels per braille ascii symbol, max 15 lines (15 * 4 = 60 vertical dots)
        let height =
            min ((roundUpToMultiple (float bitmap.Height / ratio) 4.0) |> int) (15 * 4)

        let imageInfo = new SKImageInfo(width, height)
        use resized = bitmap.Resize(imageInfo, SKFilterQuality.High)

        let average = calcAverage resized setting
        let sb = new StringBuilder()

        for y in 0..4 .. height - 1 do
            for x in 0..2 .. width - 1 do
                let brailleValue = toBraille resized x y setting average
                let braille = System.Convert.ToChar(brailleValue)
                sb.Append(braille) |> ignore

            sb.Append(" ") |> ignore

        sb.ToString()

    let private png: byte[] = [| 0x89uy ; 0x50uy ; 0x4Euy ; 0x47uy |]

    let private jpg: byte[] = [|
        0xFFuy
        0xD8uy
        0xFFuy
        0xE0uy
        0x00uy
        0x10uy
        0x4Auy
        0x46uy
        0x49uy
        0x46uy
    |]

    let private bmp: byte[] = [| 0x42uy ; 0x4Duy |]

    let private webp: (byte[] * byte[]) =
        [| 0x52uy ; 0x49uy ; 0x46uy ; 0x46uy |], [| 0x57uy ; 0x45uy ; 0x42uy ; 0x50uy |]

    let private isPng (bytes: byte[]) = bytes.Length > 3 && bytes[0..3] = png

    let private isJpg (bytes: byte[]) = bytes.Length > 8 && bytes[0..8] = jpg

    let private isBmp (bytes: byte[]) = bytes.Length > 1 && bytes[0..1] = bmp

    let private isWebp (bytes: byte[]) =
        bytes.Length > 11 && (bytes[0..3], bytes[8..11]) = webp

    let private isImage (bytes: byte[]) =
        isPng bytes || isJpg bytes || isBmp bytes || isWebp bytes

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

    let private internalBraille url setting =
        async {
            match! getImage url with
            | None -> return Message "Couldn't retrieve image, invalid url provided, or an unsupported image format is used"
            | Some image ->
                let brailleAscii = imageToBraille 30 image setting
                return Message brailleAscii
        }

    let braille args context =
        async {
            let url, colorFunc =
                match args with
                | [] -> None, None
                | [ value ] ->
                    match context.Emotes.TryFind value with
                    | Some emote -> Some emote.DirectUrl, Some DefaultColorFunction
                    | None -> Some value, Some DefaultColorFunction
                | colorFunc :: value :: _ ->
                    match
                        colorIntensityFunctions |> Map.containsKey colorFunc,
                        context.Emotes.TryFind value
                    with
                    | true, Some emote -> Some emote.DirectUrl, Some colorFunc
                    | false, Some emote -> None, Some emote.DirectUrl
                    | true, None -> Some colorFunc, None
                    | _ -> None, None

            match url, colorFunc with
            | None, None -> return Message "Invalid setting and url/emote specified"
            | None, Some _ -> return Message "Invalid url/emote specified"
            | Some _, None-> return Message "Invalid setting specified"
            | Some u, Some s -> return! internalBraille u s
        }