namespace Chatbot.Commands.Logs

module Api =

    open FsHttp
    open FsHttp.Request
    open FsHttp.Response

    [<Literal>]
    let private apiUrl = "https://logs.ivr.fi"

    let private randomChannelLine channel = $"channel/{channel}/random"
    let private randomUserLine channel user = $"channel/{channel}/user/{user}/random"

    let private sendRequest url =
        async {
            use! response =
                http {
                    GET url
                    Accept MimeTypes.textPlain
                }
                |> sendAsync

            match toResult response with
            | Ok response ->
                let! content = response.content.ReadAsStringAsync() |> Async.AwaitTask
                return Ok (content.TrimEnd())
            | Error e -> return Error $"Http response did not indicate success. {(int)e.statusCode} {e.reasonPhrase}"
        }

    let getChannelRandomLine channel =
        async {
            let url = $"{apiUrl}/{randomChannelLine channel}"
            return! sendRequest url
        }

    let getUserRandomLine channel user =
        async {
            let url = $"{apiUrl}/{randomUserLine channel user}"
            return! sendRequest url
        }
