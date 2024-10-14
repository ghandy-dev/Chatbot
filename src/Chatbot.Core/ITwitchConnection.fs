namespace Clients

[<AllowNullLiteral>]
type ITwitchConnection =
    inherit System.IDisposable
    abstract member Connected : bool
    abstract member ConnectAsync : System.Threading.CancellationToken -> Async<unit>
    abstract member ReadAsync : System.Threading.CancellationToken -> Async<string option>
    abstract member SendAsync : string -> Async<unit>
    abstract member AuthenticateAsync : string * string * string array -> Async<unit>