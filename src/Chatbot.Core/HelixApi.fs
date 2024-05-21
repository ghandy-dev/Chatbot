namespace Chatbot

open TTVSharp.Helix
open Chatbot.Configuration
open Microsoft.Extensions.Options

module HelixApi =

    let options =
        Options.Create<HelixApiOptions>(new HelixApiOptions(ClientId = Twitch.config.ClientId, ClientSecret = Twitch.config.ClientSecret))

    let helixApi = new HelixApi(options)

    module Users =

        let getUser username =
            helixApi.Users.GetUsersAsync(new GetUsersRequest(Logins = [ username ])) |> Async.AwaitTask

    module Whispers =

        let sendWhisper fromUserId toUserId message accessToken =
            helixApi.Whispers.SendWhisperAsync(
                new SendWhisperRequest(FromUserId = fromUserId, ToUserId = toUserId, Message = message),
                accessToken = accessToken
            )
            |> Async.AwaitTask

module TTVSharp =

    let toResult (response: TTVSharp.IApiResponse<'a>) =
        match response.StatusCode with
        | code when code >= 200 && code < 300 -> Ok response.Body
        | _ -> Error response.Error.Message

    let toResultSelector response =
        match response |> toResult with
        | Ok r -> r |> Ok
        | Error e -> Error e

    let tryGetData result =
        match toResult result with
        | Ok e -> Some (e :> HelixResponse<'a>).Data
        | Error _ -> None

    let tryHead result =
        match tryGetData result with
        | None -> None
        | Some data -> data |> Array.ofSeq |> Array.tryHead

    let tryHeadResult error f = f |> tryHead |> Result.fromOption error

    let tryGeteDataT response = tryGetData response

    let tryHeadT data = data |> tryHead

    let tryHeadResultT error data = data |> tryHeadResult error

    module User =

        let selectUserId (user: User) = user.Id
