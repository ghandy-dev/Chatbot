module Http

open Configuration

open FsHttp
open FsHttp.Request
open FsHttp.Response

let userAgent = configuration.Item("UserAgent")

let getFromJsonAsync<'T> (url: string) =
    async {
        use! response =
            http {
                GET url
                Accept MimeTypes.applicationJson
                UserAgent userAgent
            }
            |> sendAsync

        match toResult response with
        | Ok response ->
            let! deserialized = response |> deserializeJsonAsync<'T>
            return Ok deserialized
        | Error err ->
            let! content = response.content.ReadAsStringAsync() |> Async.AwaitTask
            return Error(content, err.statusCode)
    }

let postAsJsonAsync<'T> (url: string, value: 'T) =
    async {
        use! response =
            http {
                POST url
                UserAgent userAgent
                body
                jsonSerialize value
            }
            |> sendAsync

        let! content = response.content.ReadAsStringAsync() |> Async.AwaitTask

        match toResult response with
        | Ok _ -> return Ok content
        | Error err -> return Error(content, err.statusCode)
    }

let postAsync (url: string, data: (string * string) list) =
    async {
        use! response =
            http {
                POST url
                UserAgent userAgent
                body
                formUrlEncoded data
            }
            |> sendAsync

        let! content = response.content.ReadAsStringAsync() |> Async.AwaitTask

        match toResult response with
        | Ok _ -> return Ok content
        | Error err -> return Error(content, err.statusCode)
    }
