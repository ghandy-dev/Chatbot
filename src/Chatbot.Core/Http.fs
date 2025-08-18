module Http

open System.Net
open System.Net.Http
open System.Net.Http.Headers

open Configuration
open System.Net.Http.Json

[<AutoOpen>]
module Types =

    type Header = (string * string)

    type Method =
        | Get
        | Post
        | Delete
        | Put
        | Patch
        | Head
        | Options
        | Trace
        | Connect

    type SimpleHttpError =
        | BadRequest
        | NotFound
        | InternalServerError

    type Content =
        | Empty
        | String of string
        | ByteArray of byte array
        | FormUrlEncoded of (string * string) seq

    type Request = {
        Url: string
        Method: Method
        Headers: Header list
        Content: Content
        ContentType: string option
    }

    type Response = {
        RequestUrl: string
        Content: string
        Bytes: byte array
        Headers: Map<string, string seq>
        StatusCode: int
    }

module Method =

    let toString =
        function
        | Get -> "GET"
        | Post -> "POST"
        | Delete -> "DELETE"
        | Put -> "PUT"
        | Patch -> "PATCH"
        | Head -> "HEAD"
        | Options -> "OPTIONS"
        | Trace -> "TRACE"
        | Connect -> "CONNECT"

    let toHttpMethod method = new System.Net.Http.HttpMethod(method |> toString)

module SimpleHttpError =

    let fromStatusCode (statusCode: HttpStatusCode) =
        match int statusCode with
        | sc when sc = 404 -> NotFound
        | sc when sc >= 400 && sc < 500 -> BadRequest
        | sc when sc > 500 -> InternalServerError
        | sc -> failwith $"Unexpected status code: {sc}"

    let fromValue statusCode =
        match statusCode with
        | sc when sc = 404 -> NotFound
        | sc when sc >= 400 && sc < 500 -> BadRequest
        | sc when sc > 500 -> InternalServerError
        | sc -> failwith $"Unexpected status code: {sc}"

module AuthenticationScheme =

    let bearer token = $"Bearer {token}"
    let basic (username, password) = $"""Basic {base64 $"%s{username}:%s{password}"}"""

module ContentType =

    let applicationJson = "application/json"
    let applicationXml = "application/xml"
    let applicationPdf = "application/pdf"
    let applicationOctetStream = "application/octet-stream"
    let applicationFormUrlEncoded = "application/x-www-form-urlencoded"
    let textHtml = "text/html"
    let textPlain = "text/plain"
    let textCss = "text/css"
    let textJavascript = "text/javascript"
    let imageJpeg = "image/jpeg"
    let imagePng = "image/png"
    let imageGif = "image/gif"
    let imageWebp = "image/webp"
    let imageSvgXml = "image/svg+xml"
    let audioMpeg = "audio/mpeg"
    let audioWav = "audio/wav"
    let videoMp4 = "video/mp4"
    let videoWebm = "video/webp"
    let multipartFormData = "multipart/form-data"
    let multipartMixed = "multipart/mixed"

    let toMediaHeaderValue contentType =  MediaTypeHeaderValue.Parse(contentType)

module Header =

    let accept value = "Accept", value
    let authorization value = "Authorization", value
    let contentType value = "Content-Type", value

module Request =

    let empty = {
        Url = System.String.Empty
        Method = Method.Get
        Headers = []
        Content = Content.Empty
        ContentType = None
    }

    let request url = { empty with Url = url }

    let withUrl url (request: Request) = { request with Url = url }
    let withMethod method (request: Request) = { request with Method = method }
    let withHeader header (request: Request) = { request with Headers = header :: request.Headers }
    let withHeaders headers (request: Request) = { request with Headers = request.Headers @ headers }
    let withBody body (request: Request) = { request with Content = body }
    let withContentType contentType (request: Request) = { request with ContentType = Some contentType }

module Response =

    let create requestUrl content bytes headers statusCode = {
        RequestUrl = requestUrl
        Content = content
        Bytes = bytes
        Headers = headers
        StatusCode = statusCode
    }

    let deserializeJson<'T> (response: Response) = response.Content |> Json.deserializeJson<'T>

    let toResult (response: Response) =
        match int response.StatusCode with
        | sc when sc >= 200 && sc < 300 -> Ok response
        | _ -> Error response

    let toJsonResult<'T> response =
        response
        |> toResult
        |> Result.bind (fun r -> Ok <| deserializeJson<'T> r)

let applyHeaders (headers: (string * string) seq) (req: HttpRequestMessage) =
    headers
    |> Seq.iter req.Headers.Add

let send (client: HttpClient) (request: Request) =
    async {
        use content: HttpContent =
            match request.Content with
            | Empty -> null
            | String s -> new StringContent(s)
            | ByteArray bs -> new ByteArrayContent(bs)
            | FormUrlEncoded m -> new FormUrlEncodedContent(m |> Map.ofSeq)

        match request.ContentType with
        | None -> ()
        | Some contentType -> content.Headers.ContentType <- contentType |> ContentType.toMediaHeaderValue

        use httpRequest = new HttpRequestMessage(
            method = (request.Method |> Method.toHttpMethod),
            requestUri = request.Url,
            Content = content
        )

        applyHeaders request.Headers httpRequest

        use! httpResponse = client.SendAsync(httpRequest) |> Async.AwaitTask

        let requestUrl = httpRequest.RequestUri.ToString()
        let! content = httpResponse.Content.ReadAsStringAsync() |> Async.AwaitTask
        let! bytes = httpResponse.Content.ReadAsByteArrayAsync() |> Async.AwaitTask

        if not <| httpResponse.IsSuccessStatusCode then
            Logging.error $"Http Error: %d{int httpResponse.StatusCode} %A{httpRequest.Method} %s{requestUrl} %s{content}" (exn())

        let responseHeaders =
            httpResponse.Headers
            |> Seq.map (function KeyValue (k, v) -> k, seq v)
            |> Map.ofSeq

        let statusCode = int httpResponse.StatusCode

        let response = Response.create requestUrl content bytes responseHeaders statusCode

        return response
    }

let getUserAgent =
    let parseProduct (p: string) =
        p.Split("/")
        |> function
        | [| product ; version |] -> product, version
        | _ -> failwith "Invalid Product Info"

    let parseUserAgent (u: string) =
        u.Split(" ", 2)
        |> function
        | [| product |] ->
            let product, version = parseProduct product
            product, version, ""
        | [| product ; comment |] ->
            let product, version = parseProduct product
            product, version, comment
        | _ -> failwith "Invalid User-Agent format"

    parseUserAgent (configuration.Item("UserAgent"))

let client =
    let client = new HttpClient()
    let product, version, comment = getUserAgent
    client.DefaultRequestHeaders.UserAgent.Add(new Headers.ProductInfoHeaderValue(product, version))
    client.DefaultRequestHeaders.UserAgent.Add(new Headers.ProductInfoHeaderValue(comment))
    client
