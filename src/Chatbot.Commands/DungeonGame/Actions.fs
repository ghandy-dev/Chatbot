module Dungeon.Actions

open Types

let fightAction damage = [
    HealthChange -damage
]

let buyItemAction item  = [
    match item with
    | ShopItem.Weapon (w, cost) -> WeaponChange w ; GoldChange -cost
    | ShopItem.Armor (a, cost) -> ArmorChange a ; GoldChange -cost
    | ShopItem.Heart (f, cost) -> HealthChange f ; GoldChange -cost
    ActionPointChange -1
]

let loseAction (player: Player) =
    let goldChange = ((player.Gold * 3) / 100)
    [ GoldChange -goldChange
      StatChange (TotalGoldLostChanged goldChange)
      StatChange (DeathsChanged 1)
      WeaponChange 1
      ArmorChange 0
      ActionPointChange -1 ]

let winAction (enemy: Enemy) = [
    GoldChange enemy.Gold
    StatChange (TotalGoldEarnedChanged enemy.Gold)
    StatChange (KillsChanged enemy.Type)
    ActionPointChange -1
]

let canPerformAction (player: Player) =
    if not <| player.IsAlive then
        NoHP "No HP"
    else if not <| player.HasActionPoints then
        NoAP "No Action Points"
    else
        CanPerformActions