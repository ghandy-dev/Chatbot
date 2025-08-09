namespace Clients

[<AllowNullLiteral>]
type ITwitchConnection =
    inherit System.IDisposable
    abstract member Connected: bool
    abstract member ConnectAsync: System.Threading.CancellationToken -> Async<unit>
    abstract member ReadAsync: System.Threading.CancellationToken -> Async<string option>
    abstract member SendAsync: message: string * cancellationToken: System.Threading.CancellationToken -> Async<unit>
    abstract member AuthenticateAsync: user: string * token: string * capabailities: string array * System.Threading.CancellationToken -> Async<unit>
