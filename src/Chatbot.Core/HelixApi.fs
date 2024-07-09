namespace Chatbot

open TTVSharp.Helix

module HelixApi =

    open Chatbot.Configuration

    open Microsoft.Extensions.Options

    let private options =
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

    let tryGetData result =
        match toResult result with
        | Ok e -> Some (e :> HelixResponse<'a>).Data
        | Error _ -> None

    let tryGetDataResult result =
        match toResult result with
        | Ok e -> Ok (e :> HelixResponse<'a>).Data
        | Error err -> Error err

    let tryHead result =
        match tryGetData result with
        | None -> None
        | Some data -> data |> Array.ofSeq |> Array.tryHead

    let tryHeadResult error f =
        match tryGetDataResult f with
        | Error err -> Error err
        | Ok data ->  data |> Array.ofSeq |> Array.tryHead |> Result.fromOption error

    module User =

        let selectUserId (user: User) = user.Id
