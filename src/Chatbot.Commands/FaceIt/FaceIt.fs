namespace Chatbot.Commands

[<AutoOpen>]
module FaceIt =

    open Chatbot.Commands.Api.FaceIt

    open System
    open System.Text

    let private stats playerName =
        async {

            match! getPlayer playerName |> AsyncResult.bindZip (fun p -> getPlayerStats p.PlayerId) with
            | Error error -> return Error error
            | Ok(player, stats) ->
                let recentResults =
                    stats.Lifetime.RecentResults |> List.map (fun r -> if r = "0" then "L" else "W") |> String.concat " "

                let message =
                    (new StringBuilder())
                        .Append($"Recent games: [{recentResults}], ")
                        .Append($"""Elo: {player.Games["cs2"].FaceItElo}, """)
                        .Append($"""Rank: Level {player.Games["cs2"].SkillLevel}, """)
                        .Append($"Current win streak: {stats.Lifetime.CurrentWinStreak}, ")
                        .Append($"Longest win streak: {stats.Lifetime.LongestWinStreak}, ")
                        .Append($"Matches played: {stats.Lifetime.Matches}, ")
                        .Append($"Win rate: {stats.Lifetime.WinRate}%%, ")
                        .Append($"Average K/D ratio: {stats.Lifetime.AverageKDRatio}")
                        .ToString()

                return Ok <| Message message
        }

    let private history playerName =
        async {
            match! getPlayer playerName |> AsyncResult.bindZip (fun p -> getPlayerMatchHistory p.PlayerId 5) with
            | Error error -> return Error error
            | Ok(player, history) ->

                let! matches =
                    history.Items
                    |> List.map (fun m -> m.MatchId)
                    |> List.map getMatchStats
                    |> Async.Parallel
                    |> Async.bind (fun m -> m |> Array.map Result.toOption |> Async.create)
                    |> Async.bind (fun m -> m |> Array.choose id |> Async.create)

                match matches |> List.ofArray with
                | [] -> return Ok <| Message "No recent games played!"
                | matches ->

                    let matchResults =
                        matches
                        |> List.map (fun m ->
                            m.Rounds
                            |> List.map (fun r ->
                                let winner = r.RoundStats.Winner

                                r.Teams
                                |> List.filter (fun t -> t.TeamId = winner)
                                |> Seq.head
                                |> (fun team -> team.Players |> List.exists (fun t -> t.PlayerId = player.PlayerId))
                                |> (fun outcome -> if outcome then "Win" else "Loss")
                            )
                        )

                    let scores =
                        matches
                        |> List.map (fun m ->
                            m.Rounds
                            |> List.map (fun r ->  $"Map: {r.RoundStats.Map}, Score: [{r.RoundStats.Score}]"))

                    let results =
                        List.zip3 matchResults scores history.Items
                        |> List.map (fun (rs, ss, h) ->
                            List.zip rs ss
                            |> List.map (fun (result, stats) -> $"{DateTimeOffset.FromUnixTimeSeconds(h.FinishedAt).Date.ToShortDateString()} Result: {result}, {stats}")
                            |> String.concat " | "
                        )
                        |> String.concat " | "

                    return Ok <| Message results
        }

    let private lastGame playerName =
        async {
            let! player = getPlayer playerName

            match! player |> AsyncResult.bindZipResult (fun p -> getPlayerMatchHistory p.PlayerId 1) with
            | Error error -> return Error error
            | Ok(player, lastMatch) ->

                match lastMatch.Items with
                | [] -> return Ok <| Message "No matches played!"
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
                                $"Winner: team_{player.Nickname}"
                            else
                                $"Winner: {(snd otherTeam).Nickname}"

                        let teamElos =
                            players
                            |> Array.map (fun ps ->
                                ps
                                |> Array.choose (fun ps ->
                                    match ps with
                                    | Ok p -> Some p
                                    | Error _ -> None
                                )
                                |> Array.averageBy (fun p -> p.Games["cs2"].FaceItElo |> float)
                            )

                        let message =
                            (new StringBuilder())
                                .Append($"Map: {gameMap}, ")
                                .Append($"Game Status: {m.Status}, ")
                                .Append($"Date: {date}, ")
                                .Append($"Game Length: {durationFormatted}, ")
                                .Append($"{winner}, ")
                                .Append($"Average Elo: team_{player.Nickname} {teamElos[0]}, ")
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
                | _ -> return Error "Unknown subcommand."
            | player :: _ -> return! lastGame player
        }
