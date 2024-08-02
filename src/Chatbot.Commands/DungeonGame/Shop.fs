module Dungeon.Shop

open System

open Helpers
open Types

// Rarity * (Weapon * cost)
let weapons: (int * (Weapon * int)) list = [
    (35, (2, 3))
    (30, (3, 5))
    (20, (4, 7))
    (8, (5, 9))
    (3, (6, 13))
    (2, (7, 16))
    (1, (8, 19))
    (1, (9, 22))
]

let armor: (int * (Armor * int)) list = [
    (30, (1, 3))
    (30, (2, 5))
    (20, (3, 7))
    (8, (4, 9))
    (5, (5, 13))
    (3, (6, 16))
    (2, (7, 19))
    (1, (8, 22))
    (1, (9, 26))
]

let hearts: (int * (Heart * int)) list = [
    (35, (1, 1))
    (30, (2, 2))
    (20, (3, 3))
    (10, (4, 4))
    (5, (5, 5))
]

let private seed () = DateOnly.today().GetHashCode()

let getShop () =
    let rand = new Random(seed())
    { Items =
        [
            [ for _ in 0..2 -> getRandomItem weapons rand |> fun i -> ShopItem.Weapon(fst i, snd i) ]
            [ for _ in 0..2 -> getRandomItem armor rand |> fun i -> ShopItem.Armor(fst i, snd i) ]
            [ for _ in 0..1 -> getRandomItem hearts rand |> fun i -> ShopItem.Heart(fst i, snd i) ]
        ]
        |> List.collect id }

let canBuy gold (item: ShopItem) =
    match item with
    | ShopItem.Weapon (_, price) -> gold >= price
    | ShopItem.Armor (_, price) -> gold >= price
    | ShopItem.Heart (_, price) -> gold >= price

let buyItem id player =
    match Actions.canPerformAction player with
    | NoHP msg -> Error msg
    | NoAP msg -> Error msg
    | CanPerformActions ->
        let shop = getShop ()

        match shop.Items |> List.tryItem (id-1) with
        | None -> Error "Invalid id"
        | Some item ->
            if canBuy player.Gold (item) then
                Ok ((Player.applyChanges player (Actions.buyItemAction item)), item)
            else
                Error "Not enough gold"

let displayShop () =
    let shop = getShop ()
    shop.Items
    |> List.mapi (fun idx item ->
        match item with
        | ShopItem.Weapon (w, price) -> $"[ {idx+1}. 🗡️+{w} ({price}g) ]"
        | ShopItem.Armor (a, price) -> $"[ {idx+1}. 🛡️+{a} ({price}g) ]"
        | ShopItem.Heart (h, price) -> $"[ {idx+1}. 💗+{h} ({price}g) ]"
    ) |> String.concat ", "
