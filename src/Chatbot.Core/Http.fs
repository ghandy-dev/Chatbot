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

    type Content =
        | Empty
        | String of string
        | ByteArray of byte array
        | FormUrlEncoded of (string * string) seq
        | File of string * string * byte array
        | MultipartFormData of (string * Content) seq

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

module AuthenticationScheme =

    let bearer token = $"Bearer {token}"
    let basic (username, password) = $"""Basic {base64 $"%s{username}:%s{password}"}"""

module Content =

    let string content =  String content
    let byteArray content = ByteArray content
    let formUrlEncoded content = FormUrlEncoded content
    let file filename contentType content = File (filename, contentType, content)
    let multipartFormData content = MultipartFormData content

    let rec toHttpContent content : HttpContent =
        match content with
            | Empty -> null
            | String s -> new StringContent(s)
            | ByteArray bs -> new ByteArrayContent(bs)
            | FormUrlEncoded m -> new FormUrlEncodedContent(m |> Map.ofSeq)
            | File (_, contentType, bs) ->
                let content = new ByteArrayContent(bs)
                content.Headers.ContentType <- MediaTypeHeaderValue(contentType)
                content
            | MultipartFormData cs ->
                let multipartContent = new MultipartFormDataContent()

                cs
                |> Seq.iter (fun (name, c) ->
                    match c with
                    | File (filename, contentType, bs) ->
                        let content = new ByteArrayContent(bs)
                        content.Headers.ContentType <- MediaTypeHeaderValue(contentType)
                        multipartContent.Add(content, name, filename)
                    | _ ->
                        multipartContent.Add(toHttpContent c, name)
                )

                multipartContent

module ContentType =

    let [<Literal>] ApplicationJson = "application/json"
    let [<Literal>] ApplicationXml = "application/xml"
    let [<Literal>] ApplicationPdf = "application/pdf"
    let [<Literal>] ApplicationOctetStream = "application/octet-stream"
    let [<Literal>] ApplicationFormUrlEncoded = "application/x-www-form-urlencoded"
    let [<Literal>] TextHtml = "text/html"
    let [<Literal>] TextPlain = "text/plain"
    let [<Literal>] TextCss = "text/css"
    let [<Literal>] TextJavascript = "text/javascript"
    let [<Literal>] ImageJpeg = "image/jpeg"
    let [<Literal>] ImagePng = "image/png"
    let [<Literal>] ImageGif = "image/gif"
    let [<Literal>] ImageWebp = "image/webp"
    let [<Literal>] ImageSvgXml = "image/svg+xml"
    let [<Literal>] AudioMpeg = "audio/mpeg"
    let [<Literal>] AudioWav = "audio/wav"
    let [<Literal>] VideoMp4 = "video/mp4"
    let [<Literal>] VideoWebm = "video/webp"
    let [<Literal>] MultipartFormData = "multipart/form-data"
    let [<Literal>] MultipartMixed = "multipart/mixed"

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

    let get url = { empty with Url = url ; Method = Get }
    let post url = { empty with Url = url ; Method = Post }
    let delete url = { empty with Url = url ; Method = Delete }
    let put url = { empty with Url = url ; Method = Put }
    let patch url = { empty with Url = url ; Method = Patch }

    let withUrl url (request: Request) = { request with Url = url }
    let withMethod method (request: Request) = { request with Method = method }
    let withHeader header (request: Request) = { request with Headers = header :: request.Headers }
    let withHeaders headers (request: Request) = { request with Headers = request.Headers @ headers }
    let withBody body (request: Request) = { request with Content = body }
    let withContentType contentType (request: Request) = { request with ContentType = Some contentType }

module Response =

    let create requestUrl content bytes headers statusCode =  {
        RequestUrl = requestUrl
        Content = content
        Bytes = bytes
        Headers = headers
        StatusCode = statusCode
    }

    let toResult response =
        match response.StatusCode with
        | sc when sc >= 200 && sc < 300 -> Ok response
        | _ -> Error response

    let toJsonResult<'T> response =
        response
        |> toResult
        |> Result.bind (fun r -> Ok <| Json.deserializeJson<'T> r.Content)

let applyHeaders (headers: (string * string) seq) (req: HttpRequestMessage) =
    headers
    |> Seq.iter req.Headers.Add

let send (client: HttpClient) (request: Request) =
    async {
        use content: HttpContent = request.Content |> Content.toHttpContent

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
        let statusCode = int httpResponse.StatusCode
        let! content = httpResponse.Content.ReadAsStringAsync() |> Async.AwaitTask
        let! bytes = httpResponse.Content.ReadAsByteArrayAsync() |> Async.AwaitTask

        let responseHeaders =
            httpResponse.Headers
            |> Seq.map (function KeyValue (k, v) -> k, seq v)
            |> Map.ofSeq

        if not <| httpResponse.IsSuccessStatusCode then
            Logging.errorEx $"Http Error: %d{statusCode} %A{httpRequest.Method} %s{requestUrl} %s{content}" (exn())

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
