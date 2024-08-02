module Dungeon.Types

type Weapon = int
type Armor = int
type Heart = int

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
    Weapon: Weapon
    Armor: Armor
    Gold: int
}

type EnemyRarity =
    | VeryCommon = 25
    | Common = 15
    | Rare = 10
    | VeryRare = 7
    | Legendary = 3

let [<Literal>] maxHP = 12
let [<Literal>] startingHP = 10
let [<Literal>] maxAP = 5

type Player = {
    AP: int
    HP: int
    Weapon: Weapon
    Armor: Armor
    Gold: int
    Stats: Stats
    LastAction: System.DateOnly
} with

    static member create = {
        AP = maxAP
        HP = startingHP
        Weapon = 1
        Armor = 0
        Gold = 10
        Stats = Stats.create
        LastAction = System.DateOnly.FromDateTime(System.DateTime.UtcNow)
    }

    override this.ToString() = $"AP: {this.AP}/{maxAP}, HP: ❤️{this.HP}/{maxHP}, AD: 🗡️+{this.Weapon}, DEF: 🛡️+{this.Armor}, Gold: {this.Gold}g"
    member this.IsAlive = this.HP > 0
    member this.HasActionPoints = this.AP > 0

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

type StatChange =
    | KillsChanged of EnemyType
    | DeathsChanged of int
    | TotalGoldEarnedChanged of int
    | TotalGoldLostChanged of int

type Change =
    | HealthChange of int
    | GoldChange of int
    | WeaponChange of Weapon
    | ArmorChange of Armor
    | ActionPointChange of int
    | StatChange of StatChange

type Status =
    | NoHP of string
    | NoAP of string
    | CanPerformActions

type ShopItem =
    | Weapon of Weapon * price: int
    | Armor of Armor * price: int
    | Heart of Heart * price: int
    with

        override this.ToString() =
            match this with
            | Weapon (w, gold) -> $"🗡️+{w} -{gold}g"
            | Armor (a,  gold) -> $"🛡️+{a} -{gold}g"
            | Heart (h,  gold) -> $"💗+{h} -{gold}g"


type Shop = { Items: ShopItem list }