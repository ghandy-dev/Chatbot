namespace Commands

[<AutoOpen>]
module RockPaperScissors =

    open FsToolkit.ErrorHandling

    open Database

    type private Shapes =
        | Rock
        | Paper
        | Scissors

        with
            static member tryParse s =
                match s with
                | "rock" -> Some Rock
                | "paper" -> Some Paper
                | "scissors" -> Some Scissors
                | _ -> None

    let private shapes = [ Rock ; Paper ; Scissors ]

    let private calculateScore player cpu =
        match player, cpu with
        | p, c when p = c -> 3
        | Rock, Scissors -> 6
        | Scissors, Paper -> 6
        | Paper, Rock -> 6
        | _ -> 0

    let rps args (context: Context) =
        asyncResult {
            let! shape = args |> List.tryHead |> Option.bind Shapes.tryParse |> Result.requireSome (InvalidArgs """Invalid shape (valid choices are "rock" "paper" "scissors")""")
            let cpuShape = shapes |> List.randomChoice
            let score = calculateScore shape cpuShape

            let onSome = fun stats -> async.Return (Ok stats)
            let onNone = fun stats -> asyncResult {
                let onSuccess = fun _ -> Models.RpsStats.create (context.UserId |> int)
                let onFailure = fun _ -> InternalError "Error occurred creating stats"

                return!
                    RpsRepository.add stats
                    |> Async.map DatabaseResult.toResult
                    |> AsyncResult.eitherMap
                        onSuccess
                        onFailure
            }

            let! stats =
                RpsRepository.get (context.UserId |> int)
                |> AsyncOption.either
                    onSome
                    (onNone (Models.RpsStats.create (context.UserId |> int)))

            let outcome, updatedStats =
                match score with
                | 6 -> $"you win! +{score} points", stats.addWin ()
                | 3 -> $"it's a draw! +{score} points", stats.addDraw ()
                | _ -> $"you lose! +{score} points", stats.addLoss ()

            return!
                RpsRepository.update updatedStats
                |> Async.map DatabaseResult.toResult
                |> AsyncResult.eitherMap
                    (fun _ -> Message $"CPU picked {cpuShape}, {outcome}. Total points: {updatedStats.Score}")
                    (fun _ -> InternalError "Error occurred updating stats.")
        }
