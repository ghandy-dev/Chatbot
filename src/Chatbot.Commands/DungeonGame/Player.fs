module Dungeon.Player

open System.Text.Json

open Database
open Types

let private incrementKills key map =
    match Map.tryFind key map with
    | Some value -> Map.add key (value + 1) map
    | None -> Map.add key 1 map

let apply state change : Player =
    match change with
    | HealthChange amount ->
        if state.HP + amount > MaxHP then
            { state with HP = MaxHP }
        else
            { state with HP = state.HP + amount }
    | GoldChange amount -> { state with Gold = state.Gold + amount }
    | WeaponChange weapon -> { state with Weapon = weapon }
    | ArmorChange armor -> { state with Armor = armor }
    | ActionPointChange amount -> { state with AP = state.AP + amount ; LastAction = DateOnly.today() }
    | StatChange (KillsChanged enemy) -> { state with Stats = { state.Stats with Kills = state.Stats.Kills |> incrementKills (enemy.ToString()) } }
    | StatChange (TotalGoldEarnedChanged amount) -> { state with Stats = { state.Stats with TotalGoldEarned = state.Stats.TotalGoldEarned + amount } }
    | StatChange (TotalGoldLostChanged amount) -> { state with Stats = { state.Stats with TotalGoldLost = state.Stats.TotalGoldLost + amount } }
    | StatChange (DeathsChanged amount) -> { state with Stats = { state.Stats with Deaths = state.Stats.Deaths + amount } }

let applyChanges state changes = List.fold apply state changes

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
            if player.LastAction < DateOnly.today() then
                if not <| player.IsAlive then
                    return Some { player with AP = MaxAP ; HP = StartingHP}
                else
                    let daysSinceLastAction = (DateOnly.today().DayNumber - player.LastAction.DayNumber)
                    return Some { player with AP = MaxAP ; HP = min (player.HP + (2 * daysSinceLastAction)) MaxHP}
            else
                return Some player
    }
