namespace Chatbot.Commands

[<AutoOpen>]
module FollowAge =

    open FsToolkit.ErrorHandling

    open Chatbot.Common
    open Chatbot.Core.Domain.Commands
    open Chatbot.Core.Domain
    open Chatbot.Core.Services.Ivr

    let followAge (ivrService: IvrService) context =
        asyncResult {
            let maybeData =
                match context.MessageSource with
                | Whisper _ ->
                    match context.MessageArgs with
                    | [] -> None
                    | user :: channel :: _ -> Some (user, channel)
                    | _ -> None
                | Channel (channel, _) ->
                    match context.MessageArgs with
                    | [] -> Some (context.Username, channel)
                    | user :: channel :: _ -> Some (user, channel)
                    | user :: _ -> Some (user, channel)

            let! user, channel = maybeData |> Result.requireSome (InvalidArgs "You must specify a user and channel when using this command in whispers")
            let! subage =  ivrService.GetSubAge user channel |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "IVR")
            let isSelf = String.compareIgnoreCase user context.Username

            let message =
                match subage.FollowedAt, isSelf with
                | None, false -> $"%s{user} is not following %s{channel}"
                | None, true -> $"You are not following %s{channel}"
                | Some followedAt, false ->
                    let duration = System.DateTimeOffset.UtcNow - followedAt |> formatTimeSpan
                    $"%s{user} has been following %s{channel} for %s{duration}"
                | Some followedAt, true ->
                    let duration = System.DateTimeOffset.UtcNow - followedAt |> formatTimeSpan
                    $"You have been following %s{channel} for %s{duration}"

            return [ Message message ]
        }