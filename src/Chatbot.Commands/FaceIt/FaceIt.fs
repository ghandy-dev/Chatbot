namespace Commands

[<AutoOpen>]
module FaceIt =

    open System
    open System.Text

    open FaceIt.Api

    let private stats playerName =
        async {
            match!
                getPlayer playerName
                |> Result.bindZipAsync (fun p -> getPlayerStats p.PlayerId)
            with
            | Error error -> return Message error
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

                return Message message
        }

    let private history playerName =
        async {
            match!
                getPlayer playerName
                |> Result.bindZipAsync (fun p -> getPlayerMatchHistory p.PlayerId 5)
            with
            | Error error -> return Message error
            | Ok(player, history) ->
                match!
                    history.Items
                    |> List.map (fun m -> m.MatchId |> getMatchStats)
                    |> Async.Parallel
                    |-> Array.choose Result.toOption
                    |-> List.ofArray
                with
                | [] -> return Message "No recent games played!"
                | matches ->
                    let matchResults =
                        matches
                        |> List.map (fun m ->
                            m.Rounds
                            |> List.map (fun r ->
                                let outcome =
                                    r.Teams
                                    |> List.filter (fun t -> t.TeamId = r.RoundStats.Winner)
                                    |> List.map (fun t -> if t.Players |> List.exists (fun p -> p.PlayerId = player.PlayerId) then "Win" else "Loss")
                                    |> List.exactlyOne // the player should only be on one team

                                let score = $"Map: {r.RoundStats.Map}, Score: [{r.RoundStats.Score}]"

                                score, outcome
                            )
                        )

                    let results =
                        List.zip matchResults history.Items
                        |> List.map (fun (roundResults, h) ->
                            roundResults
                            |> List.map (fun (outcome, score) ->
                                $"{DateTimeOffset.FromUnixTimeSeconds(h.FinishedAt).Date.ToShortDateString()} {outcome}, {score}"
                            )
                            |> String.concat " | "
                        )
                        |> String.concat " | "

                    return Message results
        }

    let private lastGame playerName =
        async {
            match!
                getPlayer playerName
                |> Result.bindZipAsync (fun p -> getPlayerMatchHistory p.PlayerId 1)
            with
            | Error error -> return Message error
            | Ok(player, lastMatch) ->
                match lastMatch.Items with
                | [] -> return Message "No matches played!"
                | m :: _ ->
                    let playerTeam, otherTeam =
                        m.Teams
                        |> Map.toList
                        |> List.partition (fun (_, ps) -> ps.Players |> List.exists (fun p -> p.PlayerId = player.PlayerId))
                        |> function
                            | [ p ], [ o ] -> p, o
                            | _ -> failwith "Expected 2 lists with one item each"

                    match! getMatch m.MatchId with
                    | Error error -> return Message error
                    | Ok matchData ->
                        let gameMap = matchData.Voting.Map.Pick |> List.head

                        let date =
                            DateTimeOffset
                                .FromUnixTimeSeconds(m.FinishedAt)
                                .DateTime.ToString("dd MMM yyyy HH:mm")

                        let duration = DateTimeOffset.FromUnixTimeSeconds(m.FinishedAt) - DateTimeOffset.FromUnixTimeSeconds(m.StartedAt)
                        let durationFormatted = duration.ToString("hh\h\:mm\m\:ss\s")

                        let winner =
                            if m.Results.Winner = (fst playerTeam) then
                                $"Winner: team_{player.Nickname}"
                            else
                                $"Winner: {(snd otherTeam).Nickname}"

                        let! teamElos =
                            [ playerTeam ; otherTeam ]
                            |> List.map (fun (_, team) -> team.Players |> List.map (fun p -> getPlayerById p.PlayerId) |> Async.Parallel)
                            |> Async.Parallel
                            |-> Array.map (Array.choose Result.toOption >> Array.averageBy (fun p -> p.Games["cs2"].FaceItElo |> float))

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

                        return Message message
        }

    let faceit (args: string list) =
        async {
            match args with
            | [] -> return Message $"No subcommand/player specified"
            | command :: player :: _ ->
                match command with
                | "stats" -> return! stats player
                | "history" -> return! history player
                | _ -> return Message "Unknown subcommand."
            | player :: _ -> return! lastGame player
        }
