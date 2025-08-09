namespace Commands

[<AutoOpen>]
module FollowAge =

    open FsToolkit.ErrorHandling

    let private ivrService = Services.ivrService

    let followAge args context =
        asyncResult {
            let maybeData =
                match context.Source with
                | Whisper _ ->
                    match args with
                    | [] -> None
                    | user :: channel :: _ -> Some (user, channel)
                    | _ -> None
                | Channel channel ->
                    match args with
                    | [] -> Some (context.Username, channel.Channel)
                    | user :: channel :: _ -> Some (user, channel)
                    | user :: _ -> Some (user, channel.Channel)

            let! user, channel = maybeData |> Result.requireSome (InvalidArgs "You must specify a user and channel when using this command in whispers")
            let! subage =  ivrService.GetSubAge user channel |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "IVR")
            let isSelf = strCompareIgnoreCase user context.Username

            return
                match subage.FollowedAt, isSelf with
                | None, false -> $"%s{user} is not following %s{channel}"
                | None, true -> $"You are not following %s{channel}"
                | Some followedAt, false ->
                    let duration = System.DateTimeOffset.UtcNow - followedAt |> formatTimeSpan
                    $"%s{user} has been following %s{channel} for %s{duration}"
                | Some followedAt, true ->
                    let duration = System.DateTimeOffset.UtcNow - followedAt |> formatTimeSpan
                    $"You have been following %s{channel} for %s{duration}"
                |> Message
        }