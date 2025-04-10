namespace Commands

[<AutoOpen>]
module RockPaperScissors =

    open System

    open Database

    let private shapes = [ "rock" ; "paper" ; "scissors" ]

    let private valid shape = shapes |> List.contains shape

    let private calculateScore player cpu =
        match player, cpu with
        | player, cpu when cpu = player -> 3
        | player, "scissors" when player = "rock" -> 6
        | player, "paper" when player = "scissors" -> 6
        | player, "rock" when player = "paper" -> 6
        | _, _ -> 0

    let rps args (context: Context) =
        async {
            match args with
            | playerShape :: _ when valid playerShape ->
                let cpuShape = shapes |> List.randomChoice

                let score = calculateScore playerShape cpuShape

                let! stats =
                    async {
                        match! RpsRepository.get (context.UserId |> int) with
                        | None ->
                            match! RpsRepository.add (Models.RpsStats.create (context.UserId |> int)) with
                            | DatabaseResult.Failure -> return Error "Error occurred creating stats"
                            | DatabaseResult.Success _ ->
                                match! RpsRepository.get (context.UserId |> int) with
                                | Some stats -> return Ok stats
                                | None -> return Error "Couldn't retrieve stats"
                        | Some stats -> return Ok stats
                    }

                match stats with
                | Ok stats ->
                    let scoreMsg, updatedStats =
                        match score with
                        | 6 -> $"you win! +{score} points", stats.addWin ()
                        | 3 -> $"it's a draw! +{score} points", stats.addDraw ()
                        | _ -> $"you lose! +{score} points", stats.addLoss ()

                    match! RpsRepository.update updatedStats with
                    | DatabaseResult.Failure -> return Message "Error occurred updating stats."
                    | DatabaseResult.Success _ ->
                        return Message $"CPU picked {cpuShape}, {scoreMsg}. Total points: {updatedStats.Score}"
                | Error err -> return Message err
            | _ -> return Message """Invalid shape (valid choices are "rock" "paper" "scissors")"""
        }
