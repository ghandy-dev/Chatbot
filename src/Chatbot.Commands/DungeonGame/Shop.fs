module Dungeon.Shop

open System

open Items
open Helpers
open Types

let private seed () = DateOnly.FromDateTime(DateTime.UtcNow).GetHashCode()

let buyItemAction item  = [
    match item with
    | ShopItem.Weapon (w, cost) -> WeaponChange w ; GoldChange cost
    | ShopItem.Armor (a, cost) -> ArmorChange a ; GoldChange cost
    | ShopItem.Fruit (f, cost) -> HealthChange f ; GoldChange cost
    ActionPointChange -1
]

let getShop () =
    let rand = new Random(seed())
    { Items =
        [
            [ for _ in 0..2 -> getRandomItem weapons rand |> fun w -> ShopItem.Weapon(w, w) ]
            [ for _ in 0..2 -> getRandomItem armor rand |> fun a -> ShopItem.Armor(a, a) ]
            [ for _ in 0..1 -> getRandomItem fruit rand |> fun f -> ShopItem.Fruit(f, f) ]
        ]
        |> List.collect id }

let canBuy gold (item: ShopItem) =
    match item with
    | ShopItem.Weapon (_, price) -> gold >= price
    | ShopItem.Armor (_, price) -> gold >= price
    | ShopItem.Fruit (_, price) -> gold >= price

let buyItem id player =
    match Player.canPerformAction player with
    | NoHP msg -> Error msg
    | NoAP msg -> Error msg
    | CanPerformActions ->
        let shop = getShop ()

        match
            shop.Items
            |> List.tryItem (id-1)
            |> Option.bind (fun item -> (if canBuy player.Gold (item) then Some item else None)) with
        | None -> Error "Invalid id"
        | Some item ->
            Ok ((Player.applyChanges player (buyItemAction item)), item)

let displayShop () =
    let shop = getShop ()
    shop.Items
    |> List.mapi (fun idx item ->
        match item with
        | ShopItem.Weapon (w, price) -> $"{idx+1}. 🗡️+{w} ({price}g)"
        | ShopItem.Armor (a, price) -> $"{idx+1}. 🛡️+{a} ({price}g)"
        | ShopItem.Fruit (f, price) -> $"{idx+1}. 🍎+{f} ({price}g)"
    ) |> String.concat " | "
