namespace Chatbot.Commands.Logs

module Api =

    open FsHttp
    open FsHttp.Request
    open FsHttp.Response

    let [<Literal>] private ApiUrl = "https://logs.ivr.fi"

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
            | Error err -> return Error $"Logs API HTTP error {err.statusCode |> int} {err.statusCode}"
        }

    let getChannelRandomLine channel =
        async {
            let url = $"{ApiUrl}/{randomChannelLine channel}"
            return! sendRequest url
        }

    let getUserRandomLine channel user =
        async {
            let url = $"{ApiUrl}/{randomUserLine channel user}"
            return! sendRequest url
        }
