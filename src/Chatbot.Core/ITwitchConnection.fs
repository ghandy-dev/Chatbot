namespace Chatbot

[<AllowNullLiteral>]
type ITwitchConnection =
    inherit System.IDisposable
    abstract member Connected : bool
    abstract member ConnectAsync : System.Threading.CancellationToken -> Async<unit>
    abstract member PongAsync : string -> Async<unit>
    abstract member PartChannelAsync : string -> Async<unit>
    abstract member JoinChannelAsync : string -> Async<unit>
    abstract member JoinChannelsAsync : string list -> Async<unit>
    abstract member SendPrivMessageAsync : string * string -> Async<unit>
    abstract member ReadAsync : System.Threading.CancellationToken -> Async<string option>
    abstract member SendAsync : string -> Async<unit>
    abstract member AuthenticateAsync : string * string * string array -> Async<unit>