namespace Commands

[<AutoOpen>]
module RiotGames =

    open System

    open FSharpPlus
    open FsToolkit.ErrorHandling

    open CommandError
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
        | _ -> invalidArgs "Username#Tag required"

    let private parseRegion (region: string) =
        regions
        |> Map.tryFind (region.ToLower())
        |> Option.toResultWith (InvalidArgs "Invalid region specified")

    let league context =
        asyncResult {
            match context.Args with
            | [] -> return! invalidArgs "Arguments missing"
            | region :: riotId ->
                let! region = parseRegion region
                let! gameName, tagLine = parseRiotId riotId
                let! account = getAccount gameName tagLine |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "RiotGames - Account")
                let! summoner = getSummoner region account.PUUID |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "RiotGames - Summoner")
                let! leagueEntries = getLeagueEntries region summoner.Id |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "RiotGames - League Entries")
                let maybeLeagueEntry = leagueEntries |> List.tryFind (fun e -> e.QueueType = "RANKED_SOLO_5x5")

                match maybeLeagueEntry with
                | None -> return Message $"{account.GameName |? gameName} has not played any ranked games this season"
                | Some leagueEntry ->
                    let tier = leagueEntry.Tier
                    let rank = leagueEntry.Rank
                    let lp = leagueEntry.LeaguePoints
                    let wins = leagueEntry.Wins
                    let losses = leagueEntry.Losses
                    let winRate =  int <| float leagueEntry.Wins / (float leagueEntry.Wins + float leagueEntry.Losses) * 100.0

                    return Message $"%s{account.GameName |? gameName}#%s{account.TagLine |? tagLine} (Summoners Rift 5v5 Ranked Solo), Rank: %s{tier} %s{rank} (%d{lp} LP). W/L: %d{wins}/%d{losses}, W/R: %d{winRate}%%"
        }
