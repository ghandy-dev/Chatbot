module Dungeon.Types

type Weapon = int
type Armor = int
type Fruit = int

type EnemyType =
    | Spider
    | Skeleton
    | Ghost
    | Goblin
    | Alien
    | Troll
    | Ogre
    with

        override this.ToString() =
            match this with
            | Spider -> "🕷️"
            | Skeleton -> "💀"
            | Ghost -> "👻"
            | Goblin -> "👺"
            | Alien -> "👽"
            | Troll -> "🧌"
            | Ogre -> "👹"

type Enemy = {
    Type: EnemyType
    HP: int
    Damage: int
    Armor: int
    Gold: int
}

type EnemyRarity =
    | VeryCommon = 25
    | Common = 15
    | Rare = 10
    | VeryRare = 7
    | Legendary = 3

type Player = {
    AP: int
    HP: int
    Weapon: Weapon
    Armor: Armor
    Gold: int
    Stats: Stats
} with

    static member create = {
        AP = 3
        HP = 20
        Weapon = 1
        Armor = 0
        Gold = 10
        Stats = Stats.create
    }

    override this.ToString() = $"AP: {this.AP}/3, HP: {this.HP}, Damage: {this.Weapon}, Armor: {this.Armor}, Gold: {this.Gold}g"

and Stats = {
    Kills: Map<string, int>
    Deaths: int
    TotalGoldEarned: int
    TotalGoldLost: int
} with

    static member create = {
        Kills = Map.empty
        Deaths = 0
        TotalGoldEarned = 0
        TotalGoldLost = 0
    }

type Change =
    | HealthChange of int
    | GoldChange of int
    | WeaponChange of Weapon
    | ArmorChange of Armor
    | EnemyDefeated of EnemyType
    | ActionPointChange of int

type Status =
    | NoHP of string
    | NoAP of string
    | CanPerformActions

type ShopItem =
    | Weapon of Weapon * price: int
    | Armor of Armor * price: int
    | Fruit of Fruit * price: int
    with

        override this.ToString() =
            match this with
            | Weapon (w, gold) -> $"🗡️+{w} -{gold}g"
            | Armor (a,  gold) -> $"🛡️+{a} -{gold}g"
            | Fruit (f,  gold) -> $"🍎+{f} -{gold}g"


type Shop = { Items: ShopItem list }