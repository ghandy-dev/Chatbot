namespace Chatbot.Commands

[<AutoOpen>]
module SubAge =

    open FsToolkit.ErrorHandling

    open Chatbot.Common
    open Chatbot.Core.Domain.Commands
    open Chatbot.Core.Domain
    open Chatbot.Core.Services.Ivr

    let subAge (ivrService: IvrService) context =
        asyncResult {
            let maybeData: (string * string) option =
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

            match maybeData with
            | None -> return [ Message "You must specify a user and channel when using this command in whispers" ]
            | Some (user, channel) ->

                let! subage = ivrService.GetSubAge user channel |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "IVR")
                match subage.StatusHidden with
                | None -> return [ Message $"Unable to look up subscription status to channel {channel}" ]
                | Some true -> return [ Message "Subscription status hidden" ]
                | Some false ->
                    let self = if System.String.Compare(user, context.Username, ignoreCase = true) = 0 then true else false

                    let message =
                        match subage.Cumulative, subage.Streak, self with
                        | Some stats, Some streak, true ->
                            $"You have been subscribed to %s{subage.Channel.DisplayName} for %d{stats.Months} month(s) (%d{streak.Months} month streak)"
                        | Some stats, Some streak, false ->
                            $"%s{user} has been subscribed to %s{subage.Channel.DisplayName} for %d{stats.Months} month(s) (%d{streak.Months} month streak)"
                        | Some stats, None, false ->
                            $"%s{user} is not currently subscribed to %s{subage.Channel.DisplayName}. Previously subscribed for %d{stats.Months} month(s). Subscription ended on %s{stats.End.ToString(Utils.DateStringFormat)}"
                        | Some stats, None, true ->
                            $"You are not currently subscribed to %s{subage.Channel.DisplayName}. Previously subscribed for %d{stats.Months} month(s). Subscription ended on %s{stats.End.ToString(Utils.DateStringFormat)}"
                        | _, _, false ->
                            $"%s{user} has not subscribed to %s{subage.Channel.DisplayName} before"
                        | _, _, true ->
                            $"You have not subscribed to %s{subage.Channel.DisplayName} before"

                    return [ Message message ]
        }