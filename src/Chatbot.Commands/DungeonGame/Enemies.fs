module Dungeon.Enemy

open Types

let apply state change : Enemy =
    match change with
    | HealthChange amount -> { state with HP = state.HP + amount }
    | _ -> state

let applyChanges state changes = List.fold apply state changes

let enemies = [
    (EnemyRarity.VeryCommon |> int, { Type = Spider ; HP = 4 ; Damage = 3 ; Armor = 0 ; Gold = 1 })
    (EnemyRarity.VeryCommon |> int, { Type = Skeleton ; HP = 4 ; Damage = 4 ; Armor = 0 ; Gold = 1 })
    (EnemyRarity.Common |> int, { Type = Ghost ; HP = 6 ; Damage = 4 ; Armor = 0 ; Gold = 2 })
    (EnemyRarity.Common |> int, { Type = Goblin ; HP = 6 ; Damage = 6 ; Armor = 2 ; Gold = 4 })
    (EnemyRarity.Rare |> int, { Type = Alien ; HP = 10 ; Damage = 4 ; Armor = 2 ; Gold = 5 })
    (EnemyRarity.VeryRare |> int, { Type = Troll ; HP = 20 ; Damage = 4 ; Armor = 10 ; Gold = 15 })
    (EnemyRarity.Legendary |> int, { Type = Ogre ; HP = 25 ; Damage = 6 ; Armor = 8 ; Gold = 30 })
]