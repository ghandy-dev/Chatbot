namespace Commands

[<AutoOpen>]
module FaceIt =

    open System
    open System.Text

    open FSharpPlus
    open FsToolkit.ErrorHandling

    open FaceIt.Api

    let private stats playerName =
        asyncResult {
            let! player = getPlayer playerName |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "FaceIt")
            let! stats = getPlayerStats player.PlayerId |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "FaceIt")
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
        asyncResult {
            let! player = getPlayer playerName |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "FaceIt")
            let! matchHistory = getPlayerMatchHistory player.PlayerId 5 |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "FaceIt")

            let! matches =
                matchHistory.Items
                |> List.map (fun m -> m.MatchId |> getMatchStats)
                |> Async.Parallel
                |> Async.map (Array.choose Result.toOption)
                |> Async.map List.ofArray

            if matches.Length = 0 then
                return Message "No recent games played!"
            else
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
                    List.zip matchResults matchHistory.Items
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
        asyncResult {
            let! player = getPlayer playerName |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "FaceIt")

            let! history =
                getPlayerMatchHistory player.PlayerId 1
                |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "FaceIt")
                |> AsyncResult.map _.Items

            match history with
            | [] -> return Message "No matches played"
            | { MatchId = matchId ; Teams = teams ; Results = { Winner = winner } } :: _ ->
                let team1, team2 =
                    teams
                    |> Map.toList
                    |> List.map snd
                    |> List.partition (fun t -> t.Players |> List.exists (fun p -> p.PlayerId = player.PlayerId))
                    |> function
                        | [ t1 ], [ t2 ] -> t1, t2
                        | _ -> failwith "Expected 2 lists with one item each"

                let! matchData =
                    getMatch matchId
                    |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "FaceIt")

                let gameMap = matchData.Voting.Map.Pick |> List.head

                let date =
                    DateTimeOffset
                        .FromUnixTimeSeconds(matchData.FinishedAt)
                        .DateTime.ToString("dd MMM yyyy HH:mm")

                let duration = DateTimeOffset.FromUnixTimeSeconds(matchData.FinishedAt) - DateTimeOffset.FromUnixTimeSeconds(matchData.StartedAt)

                let winningTeam =
                    if winner = team1.Nickname then
                        $"team_{player.Nickname}"
                    else
                        $"{team2.Nickname}"

                let selectPlayerElo = fun (p: FaceIt.Types.Players.Player) -> p.Games["cs2"].FaceItElo |> float
                let selectPlayers = fun (t: FaceIt.Types.Players.Team) -> t.Players
                let lookUpPlayer = fun (p: FaceIt.Types.Players.TeamPlayer) -> getPlayerById p.PlayerId

                let calcTeamAverageElo =
                    selectPlayers
                    >> List.map lookUpPlayer
                    >> Async.Parallel
                    >> Async.map (Seq.choose Result.toOption)
                    >> Async.map (Seq.map selectPlayerElo)
                    >> Async.map Seq.average

                let! team1Elo = calcTeamAverageElo team1
                let! team2Elo = calcTeamAverageElo team2

                let message =
                    (new StringBuilder())
                        .Append($"Map: %s{gameMap}, ")
                        .Append($"Date: %s{date}, ")
                        .Append($"""Game Length: %s{duration.ToString("hh\h\:mm\m\:ss\s")}, """)
                        .Append($"Winner: %s{winningTeam}, ")
                        .Append($"Average Elo: team_%s{player.Nickname} %f{team1Elo}, ")
                        .Append($"%s{team2.Nickname}: %f{team2Elo}")
                        .ToString()

                return Message message
        }

    let faceit context =
        async {
            match context.Args with
            | [] -> return Error <| InvalidArgs $"No subcommand/player specified"
            | command :: player :: _ ->
                match command with
                | "stats" -> return! stats player
                | "history" -> return! history player
                | _ -> return Error <| InvalidArgs "Unknown subcommand."
            | player :: _ -> return! lastGame player
        }
