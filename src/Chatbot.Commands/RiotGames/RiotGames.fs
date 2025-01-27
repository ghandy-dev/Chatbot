namespace Commands.RiotGames

[<AutoOpen>]
module RiotGames =

    open System

    open Api
    open Commands

    let regions =
        [
            "euw", "euw1"
            "eune", "eun1"
            "kr", "kr"
            "jp", "jp1"
            "br", "br1"
            "la", "la1"
            "oce", "oc1"
            "tr", "tr1"
            "ru", "ru"
        ]
        |> Map.ofList

    let parseRiotId riotId =
        riotId |> String.concat " "  |> _.Split("#", StringSplitOptions.TrimEntries)
        |> function
        | [| gameName ; tagLine |] -> Ok (gameName, tagLine)
        | _ -> Error "Bad riot gameName/tagLine provided"

    let parseRegion region =
        regions
        |> Map.tryFind region
        |> Result.fromOption "Invalid region specified"

    let parse region riotId =
        parseRegion region
        |> Result.bindZip (fun _ -> parseRiotId riotId)

    let league (args: string list) =
        async {
            match args with
            | [] -> return Message "Arguments missing"
            | region :: riotId ->
                match parse region riotId with
                | Error err -> return Message err
                | Ok (region, (gameName, tagLine)) ->
                    match!
                        getAccount gameName tagLine
                        |> Result.bindAsync (fun account -> getSummoner region account.PUUID)
                        |> Result.bindAsync (fun summoner -> getLeagueEntries region summoner.Id)
                    with
                    | Error err -> return Message err
                    | Ok leagueEntries ->
                        match leagueEntries |> List.filter (fun e -> e.QueueType = "RANKED_SOLO_5x5") |> List.tryHead with
                        | None -> return Message "Player has not played ranked solo this season, or hasn't finished their ranked placement matches yet"
                        | Some leagueEntry ->
                            let tier = leagueEntry.Tier
                            let rank = leagueEntry.Rank
                            let lp = leagueEntry.LeaguePoints
                            let wins = leagueEntry.Wins
                            let losses = leagueEntry.Losses
                            let winRate =  int <| float leagueEntry.Wins / (float leagueEntry.Wins + float leagueEntry.Losses) * 100.0

                            return Message $"Ranked Solo, Rank: %s{tier} %s{rank} (%d{lp} LP). Wins: %d{wins}, Losses: %d{losses}, W/R: %d{winRate}%%"
        }
