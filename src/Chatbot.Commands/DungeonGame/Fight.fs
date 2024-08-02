module Dungeon.Fight

open System

open Types

type Outcome = Victory of Player | Defeat of Player

let handleFight (player: Player) (enemy: Enemy) =
    let roll () = Random.Shared.Next(6)

    let rec fight (player: Player) (enemy: Enemy) =
        if player.HP <= 0 then
            Defeat (Player.applyChanges player (Actions.loseAction player))
        else if enemy.HP <= 0 then
            Victory (Player.applyChanges player (Actions.winAction enemy))
        else
            let playerRoll = roll ()
            let enemyRoll = roll ()
            let playerDamage = max (playerRoll + player.Weapon - enemy.Armor) 0
            let enemyDamage = max (enemyRoll + enemy.Weapon - player.Armor) 0
            fight
                (Player.applyChanges player (Actions.fightAction enemyDamage))
                (Enemy.applyChanges enemy (Actions.fightAction playerDamage))

    match Actions.canPerformAction player with
    | NoHP msg -> Error msg
    | NoAP msg -> Error msg
    | CanPerformActions ->
        let player' = fight player enemy
        Ok player'