module Http

open FsHttp
open FsHttp.Request
open FsHttp.Response

let getFromJsonAsync<'a> (url: string) =
    async {
        use! response =
            http {
                GET url
                Accept MimeTypes.applicationJson
            }
            |> sendAsync

        match toResult response with
        | Ok response ->
            let! deserialized = response |> deserializeJsonAsync<'a>
            return Ok deserialized
        | Error err ->
            let! content = response.content.ReadAsStringAsync() |> Async.AwaitTask
            return Error(content, err.statusCode)
    }

let postAsync (url: string) (data: (string * string) list) =
    async {
        use! response =
            http {
                POST url
                body
                formUrlEncoded data
            }
            |> sendAsync

        let! content = response.content.ReadAsStringAsync() |> Async.AwaitTask

        match toResult response with
        | Ok _ -> return Ok content
        | Error err -> return Error(content, err.statusCode)
    }
