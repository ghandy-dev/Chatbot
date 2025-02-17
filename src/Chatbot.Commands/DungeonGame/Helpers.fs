module Dungeon.Helpers

open System
open Int32

let getRandomItem list (rand: Random) =
    let totalWeight = list |> List.sumBy fst
    let randomNumber = rand.Next(1, totalWeight + 1)

    let rec selectItem remainingWeight = function
        | [] -> failwith "list cannot be empty"
        | (weight, item) :: rest ->
            if randomNumber <= remainingWeight + weight then item
            else selectItem (remainingWeight + weight) rest

    selectItem 0 list

let formatNumberChange = fun n ->
    match n with
    | n when positive n -> $"+{n}"
    | n when negative n -> $"{n}"
    | _ -> failwith "Waduheck"