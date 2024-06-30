namespace Chatbot.Commands

[<AutoOpen>]
module Braille =

    open System.Text

    open SkiaSharp

    open FsHttp
    open FsHttp.Request
    open FsHttp.Response

    let private offsets = [ 1 ; 8 ; 2 ; 16 ; 4 ; 32 ; 64 ; 128 ]

    let private conversions =
        [
            "luminance", (fun (s: SKColor) -> (0.299 * float s.Red) + (0.587 * float s.Green) + (0.114 * float s.Blue))
            "average", (fun (s: SKColor) -> (s.Red + s.Green + s.Blue) |> float |> (/) 3.0)
            "max", (fun (s: SKColor) -> [ s.Red ; s.Green ; s.Blue ] |> List.max |> float)
            "lightness",
            fun (s: SKColor) ->
                let max = ([ s.Red ; s.Green ; s.Blue ] |> List.max) |> float
                let min = ([ s.Red ; s.Green ; s.Blue ] |> List.min) |> float
                (max + min) / 2.0

        ]
        |> Map.ofList

    let private getPixelValue (bitmap: SKBitmap) x y f =
        let pixel = bitmap.GetPixel(x, y)
        conversions.[f] pixel

    let private calcAverage (bitmap: SKBitmap) f =
        let average =
            List.fold (fun acc y -> List.fold (fun innerAcc x -> innerAcc + getPixelValue bitmap x y f) acc [ 0 .. bitmap.Width - 1 ]) 0.0 [
                0 .. bitmap.Height - 1
            ]

        average / (float bitmap.Height * float bitmap.Width)

    let private toBraille (bitmap: SKBitmap) x y f average : int =
        let mutable brailleValue = 10240

        if getPixelValue bitmap x y f > average then
            brailleValue <- brailleValue + offsets[0]

        if getPixelValue bitmap (x + 1) y f > average then
            brailleValue <- brailleValue + offsets[1]

        if getPixelValue bitmap x (y + 1) f > average then
            brailleValue <- brailleValue + offsets[2]

        if getPixelValue bitmap (x + 1) (y + 1) f > average then
            brailleValue <- brailleValue + offsets[3]

        if getPixelValue bitmap x (y + 2) f > average then
            brailleValue <- brailleValue + offsets[4]

        if getPixelValue bitmap (x + 1) (y + 2) f > average then
            brailleValue <- brailleValue + offsets[5]

        if getPixelValue bitmap x (y + 3) f > average then
            brailleValue <- brailleValue + offsets[6]

        if getPixelValue bitmap (x + 1) (y + 3) f > average then
            brailleValue <- brailleValue + offsets[7]

        if brailleValue = 10240 then
            brailleValue <- 10241

        brailleValue

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

        let width = width * 2 // 2 pixels per braille ascii symbol
        let ratio = float bitmap.Width / (float width)
        let height = (roundUpToMultiple (float bitmap.Height / ratio) 4.0) |> int // 4 pixels per braille ascii symbol

        let imageInfo = new SKImageInfo(width, height)
        use resized = bitmap.Resize(imageInfo, SKFilterQuality.High)

        let average = calcAverage resized setting
        let sb = new StringBuilder()

        for y in 0..4 .. height - 1 do
            for x in 0..2 .. width - 1 do
                let brailleValue = toBraille resized x y setting average
                let braille = System.Convert.ToChar(brailleValue)
                sb.Append(braille) |> ignore

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

    let private isWebp (bytes: byte[]) = bytes.Length > 11 && (bytes[0..3], bytes[8..11]) = webp

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
            | Ok response ->
                let! bytes = response |> toBytesAsync

                if
                    (response.originalHttpResponseMessage.Content.Headers.Contains("Content-Type")
                     && RegularExpressions.Regex.IsMatch(
                         response.originalHttpResponseMessage.Content.Headers.ContentType.MediaType,
                         pattern
                     ))
                    || isImage bytes
                then
                    return Some bytes
                else
                    return None
            | Error _ -> return None
        }

    let private internalBraille url setting =
        async {
            match! getImage url with
            | None -> return Error "Couldn't retrieve image, invalid url provided, or an unsupported image format is used."
            | Some image ->
                let braille = imageToBraille 30 image setting

                if braille.Length > 500 then
                    return Ok braille[0..479]
                else
                    return Ok braille
        }

    let braille args =
        async {
            match args with
            | [] -> return Ok <| Message "No url specified."
            | url :: setting ->
                let setting =
                    match setting with
                    | [] -> "luminance"
                    | setting :: _ -> setting

                match conversions |> Map.containsKey setting with
                | false -> return Error "Unknown conversion."
                | true ->
                    match! internalBraille url setting with
                    | Error err -> return Error err
                    | Ok brailleAscii -> return Ok <| Message brailleAscii
        }
