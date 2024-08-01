namespace Chatbot.Commands

[<AutoOpen>]
module Dungeon =

    open Dungeon
    open Player
    open Types

    let private playerKills player =
        player.Stats.Kills |> Map.toList |> List.map (fun (k, v) ->
            let enemy = k.ToString()
            $"{enemy} - {v}"
        ) |> String.concat ", "

    let dungeon args context : Async<CommandResult> =
        async {
            let userId = (context.UserId |> int)

            match args with
            | "register" :: _ ->
                match! registerPlayer userId with
                | None -> return Error "Error registering player"
                | Some player -> return Ok <| Message (player.ToString())
            | _ ->
                match! getPlayer userId with
                | None -> return Ok <| Message "You must register first"
                | Some player ->
                    match args with
                    | "stats" :: _ ->
                        let kills = playerKills player
                        return Ok <| Message $"Kills: {kills}, Deaths: {player.Stats.Deaths}, Total Gold Earned: {player.Stats.TotalGoldEarned}g, Total Gold Lost: {player.Stats.TotalGoldLost}g"
                    | "status" :: _ -> return Ok <| Message (player.ToString())
                    | "shop" :: _ -> return Ok <| Message (Shop.displayShop())
                    | "buy" :: id :: _ ->
                        let id = Option.defaultValue 0 (Int32.tryParse id)
                        match Shop.buyItem id player with
                        | Error err -> return Error err
                        | Ok (player, item) ->
                            do! updatePlayer userId player
                            return Ok <| Message ($"Bought {item.ToString()}")
                    | "fight" :: _ ->
                        let enemy = Helpers.getRandomItem Enemy.enemies (System.Random.Shared)
                        match Fight.handleFight player enemy with
                        | Error err -> return Error err
                        | Ok player' ->
                            do! updatePlayer userId player'
                            let hpChange = Helpers.formatNumberChange (player'.HP - player.HP)
                            let goldChange = Helpers.formatNumberChange (player'.Gold - player.Gold)
                            return Ok <| Message $"Defeated {enemy.Type.ToString()} (HP: {enemy.HP}, AD: {enemy.Damage}, DEF: {enemy.Armor}) AP: {player'.AP}/3, HP: {player'.HP}({hpChange}), Damage: {player'.Weapon}, Armor: {player'.Armor}, Gold: {player'.Gold}({goldChange}g)"
                    | _ -> return Error "Unknown subcommand"
        }