namespace Commands

[<AutoOpen>]
module RiotGames =

    open System

    open RiotGames.Api

    let private regions =
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

    let private parseRiotId (riotId: string seq) =
        riotId |> String.concat " "  |> _.Split("#", StringSplitOptions.TrimEntries)
        |> function
        | [| gameName ; tagLine |] -> Ok (gameName, tagLine)
        | _ -> Error "Bad username/#tag provided"

    let private parseRegion (region: string) =
        regions
        |> Map.tryFind (region.ToLower())
        |> Result.fromOption "Invalid region specified"

    let private parse (region: string) (riotId: string seq) =
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
                        |> Result.bindZipAsync (fun account -> getSummoner region account.PUUID)
                        |> Result.bindZipAsync (fun (_, summoner) -> getLeagueEntries region summoner.Id)
                    with
                    | Error (err, statusCode) ->
                        match int statusCode with
                        | 404 -> return Message "Account/Summoner not found"
                        | _ -> return Message err
                    | Ok ((account, _), leagueEntries) ->
                        match leagueEntries |> List.tryFind (fun e -> e.QueueType = "RANKED_SOLO_5x5") with
                        | None -> return Message "Player has not played ranked solo this season, or hasn't finished their ranked placement matches yet"
                        | Some leagueEntry ->
                            let tier = leagueEntry.Tier
                            let rank = leagueEntry.Rank
                            let lp = leagueEntry.LeaguePoints
                            let wins = leagueEntry.Wins
                            let losses = leagueEntry.Losses
                            let winRate =  int <| float leagueEntry.Wins / (float leagueEntry.Wins + float leagueEntry.Losses) * 100.0

                            return Message $"%s{account.GameName |?? gameName}#%s{account.TagLine |?? tagLine} (Summoners Rift 5v5 Ranked Solo), Rank: %s{tier} %s{rank} (%d{lp} LP). W/L: %d{wins}/%d{losses}, W/R: %d{winRate}%%"
        }
