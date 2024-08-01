module Dungeon.Fight

open System

open Types
open Helpers

let fightAction damage = [
    HealthChange -damage
]

let handleFight (player: Player) (enemy: Enemy) =
    let roll () = Random.Shared.Next(6)

    let rec fight (player: Player) (enemy: Enemy) =
        if player.HP <= 0 then
            Player.applyChanges player [ GoldChange -((player.Gold * 3) / 100) ]
        else if enemy.HP <= 0 then
            Player.applyChanges player [ GoldChange enemy.Gold ; EnemyDefeated enemy.Type ]
        else
            let playerRoll = roll ()
            let enemyRoll = roll ()
            let playerDamage = playerRoll + player.Weapon
            let enemyDamage = enemyRoll + enemy.Damage
            fight
                (Player.applyChanges player (fightAction enemyDamage))
                (Enemy.applyChanges enemy (fightAction playerDamage))

    match Player.canPerformAction player with
    | NoHP msg -> Error msg
    | NoAP msg -> Error msg
    | CanPerformActions ->
        let player' = fight player enemy
        Ok player'