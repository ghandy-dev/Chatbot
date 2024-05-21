namespace Chatbot.Commands

[<AutoOpen>]
module FaceIt =

    open Chatbot.Commands.Api.FaceIt

    open System
    open System.Text
    open Chatbot

    let private stats playerName =
        async {
            let! player = getPlayer playerName

            match! player |> AsyncResult.bindAsyncSync (fun p -> getPlayerStats p.PlayerId) with
            | Error error -> return Error error
            | Ok stats ->
                let recentResults =
                    stats.Lifetime.RecentResults |> List.map (fun r -> if r = "0" then "L" else "W") |> String.concat " "

                let message =
                    (new StringBuilder())
                        .Append($"Recent games: [{recentResults}] | ")
                        .Append($"Current win streak: {stats.Lifetime.CurrentWinStreak} | ")
                        .Append($"Longest win streak: {stats.Lifetime.LongestWinStreak} | ")
                        .Append($"Matches played: {stats.Lifetime.Matches} | ")
                        .Append($"Win rate: {stats.Lifetime.WinRate}%% | ")
                        .Append($"Average K/D ratio: {stats.Lifetime.AverageKDRatio}")
                        .ToString()

                return Ok <| Message message
        }

    let private history playerName =
        async {
            let! player = getPlayer playerName

            match! player |> AsyncResult.bindZipResult (fun p -> getPlayerMatchHistory p.PlayerId 20) with
            | Error error -> return Error error
            | Ok(player, history) ->

                let outcomes =
                    history.Items
                    |> List.map (fun m -> m.Teams[m.Results.Winner].Players |> List.exists (fun p -> p.PlayerId = player.PlayerId))
                    |> List.countBy id
                    |> Map.ofList

                let scores =
                    history.Items |> List.map (fun m -> $"{m.Results.Score}") |> String.concat ", "

                let message =
                    (new StringBuilder())
                        .Append($"W/L: {outcomes[true]}/{outcomes[false]} | Recent scores: {scores}")
                        .ToString()

                return Ok <| Message message
        }

    let private lastGame playerName =
        async {
            let! player = getPlayer playerName

            match! player |> AsyncResult.bindZipResult (fun p -> getPlayerMatchHistory p.PlayerId 1) with
            | Error error -> return Error error
            | Ok(player, lastMatch) ->

                match lastMatch.Items with
                | [] -> return Ok <| Message "no matches played"
                | m :: _ ->
                    let playerTeam, otherTeam =
                        m.Teams
                        |> Map.toList
                        |> List.partition (fun (team, ps) -> ps.Players |> List.exists (fun p -> p.PlayerId = player.PlayerId))
                        |> function
                            | [ p ], [ o ] -> (p, o)
                            | _ -> failwith "Expected 2 lists with one item each"

                    match! getMatch m.MatchId with
                    | Error error -> return Error error
                    | Ok matchData ->

                        let! players =
                            [ playerTeam ; otherTeam ]
                            |> List.map (fun (teamName, team) ->
                                async {
                                    return!
                                        team.Players |> List.map (fun p -> async { return! getPlayerById p.PlayerId }) |> Async.Parallel
                                }
                            )
                            |> Async.Parallel

                        let gameMap =
                            match matchData.Voting.Map.Pick |> List.tryHead with
                            | None -> ""
                            | Some map -> map

                        let date =
                            DateTimeOffset
                                .FromUnixTimeSeconds(m.FinishedAt)
                                .DateTime.ToString("dd MMM yyyy HH:mm")

                        let duration =
                            (DateTimeOffset.FromUnixTimeSeconds(m.FinishedAt) - DateTimeOffset.FromUnixTimeSeconds(m.StartedAt))

                        let durationFormatted = $"{duration.Hours}h:{duration.Minutes}m:{duration.Seconds}s"

                        let winner =
                            if m.Results.Winner = (fst playerTeam) then
                                $"winner: team_{player.Nickname}"
                            else
                                $"winner: {(snd otherTeam).Nickname}"

                        let teamElos =
                            players
                            |> Array.map (fun ps ->
                                ps
                                |> Array.choose (fun ps ->
                                    match ps with
                                    | Ok p -> Some p
                                    | Error _ -> None
                                )
                                |> Array.averageBy (fun p -> p.Games["csgo"].FaceItElo |> float)
                            )

                        let message =
                            (new StringBuilder())
                                .Append($"map: {gameMap} | ")
                                .Append($"game status: {m.Status} | ")
                                .Append($"date: {date} | ")
                                .Append($"duration: {durationFormatted} | ")
                                .Append($"{winner} | ")
                                .Append($"average elo: team_{player.Nickname} {teamElos[0]}, ")
                                .Append($"{(snd otherTeam).Nickname}: {teamElos[1]}")
                                .ToString()

                        return Ok <| Message message
        }

    let faceit (args: string list) =
        async {
            match args with
            | [] -> return Error $"Usage: >faceit FrozenBag | >faceit stats FrozenBag | >faceit history FrozenBag"
            | command :: player :: _ ->
                match command with
                | "stats" -> return! stats player
                | "history" -> return! history player
                | _ -> return Error "unknown subcommand"
            | player :: _ -> return! lastGame player
        }
