module Dungeon.Player

open System.Text.Json

open Chatbot.Database
open Types

let [<Literal>] maxHP = 12

let private incrementKills key map =
    match Map.tryFind key map with
    | Some value -> Map.add key (value + 1) map
    | None -> Map.add key 1 map

let apply state change : Player =
    match change with
    | HealthChange amount ->
        if state.HP + amount < 0 then
            { state with HP = state.HP + amount ; Stats = { state.Stats with Deaths = state.Stats.Deaths + 1} }
        else if state.HP + amount > maxHP then
            { state with HP = maxHP }
        else
            { state with HP = state.HP + amount }
    | GoldChange amount ->
        if amount > 0 then
            { state with Gold = state.Gold + amount ; Stats = { state.Stats with TotalGoldEarned = state.Stats.TotalGoldEarned + amount } }
        else
            { state with Gold = state.Gold + amount }
    | WeaponChange weapon -> { state with Weapon = weapon }
    | ArmorChange armor -> { state with Armor = armor }
    | EnemyDefeated enemy -> { state with Stats = { state.Stats with Kills = state.Stats.Kills |> incrementKills (enemy.ToString()) } }
    | ActionPointChange amount -> { state with AP = state.AP + amount }

let applyChanges state changes = List.fold apply state changes

let isAlive player = player.HP > 0
let hasActionPoints player = player.AP > 0

let canPerformAction player =
    if not <| isAlive player then
        NoHP "No HP"
    else if not <| hasActionPoints player then
        NoAP "No Action Points"
    else
        CanPerformActions

let updatePlayer userId player =
    async {
        let data = JsonSerializer.Serialize(player)
        do! DungeonRepository.update { UserId = userId ; Data = data } |> Async.Ignore
    }

let registerPlayer userId =
    async {
        let player = Player.create
        let data = JsonSerializer.Serialize(player)
        match! DungeonRepository.add { UserId = userId ; Data = data } with
        | DatabaseResult.Failure -> return None
        | DatabaseResult.Success _ -> return Some player
    }

let getPlayer userId =
    async {
        match! DungeonRepository.get userId with
        | None -> return None
        | Some dp ->
            let player = JsonSerializer.Deserialize<Player>(dp.Data)
            return Some player
    }
