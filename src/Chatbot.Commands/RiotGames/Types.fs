namespace RiotGames

module Types =

    open System
    open System.Text.Json.Serialization

    type Account = {
        PUUID: string
        GameName: string option
        TagLine: string option
    }

    type Summoner = {
        AccountId: string
        ProfileIconId: int
        [<JsonConverter(typeof<UnixEpochDateTimeOffsetConverter>)>]
        RevisionDate: DateTimeOffset
        Id: string
        PUUID: string
        SummonerLevel: int64
    }

    type LeagueEntry = {
        LeagueId: string
        SummonerId: string
        QueueType: string
        Tier: string
        Rank: string
        LeaguePoints: int
        Wins: int
        Losses: int
        HotStreak: bool
        Veteran: bool
        FreshBlood: bool
        Inactive: bool
        MiniSeries: MiniSeries option
    }

    and MiniSeries = {
        Losses: int
        Progress: string
        Target: int
        Win: int
    }

    type Match = {
        MetaData: MatchData
        Info: MatchInfo
    }

    and MatchData = {
        DataVersion: string
        MatchId: string
        Participants: string list
    }

    and MatchInfo = {
        EndOfGameResult: string
        [<JsonConverter(typeof<UnixEpochDateTimeOffsetConverter>)>]
        GameCreation: DateTimeOffset
        [<JsonConverter(typeof<UnixEpochDateTimeOffsetConverter>)>]
        GameDuration: DateTimeOffset
        [<JsonConverter(typeof<UnixEpochDateTimeOffsetConverter>)>]
        GameId: DateTimeOffset
        GameMode: string
        GameName: string
        [<JsonConverter(typeof<UnixEpochDateTimeOffsetConverter>)>]
        GameStartTimestamp: DateTimeOffset
        [<JsonConverter(typeof<UnixEpochDateTimeOffsetConverter>)>]
        GameEndTimestamp: DateTimeOffset
        GameType: string
        GameVersion: string
        MapId: int
        Participants: MatchParticipant list
        PlatformId: string
        QueueId: int
        Teams: MatchTeam list
        TournamentCode: string
    }

    and MatchParticipant = {
        AllInPings: int
        AssistMePings: int
        Assists: int
        BaronKills: int
        BountyLevel: int
        ChampExperience: int
        ChampLevel: int
        ChampionId: int
        ChampionName: string
        CommandPings: int
        ChampionTransform: int
        ConsumablesPurchased: int
        // Challenges: Challenges
        DamageDealtToBuildings: int
        DamageDealtToObjectives: int
        DamageDealtToTurrets: int
        DamageSelfMitigated: int
        Deaths: int
        DetectorWardsPlaced: int
        DoubleKills: int
        DragonKills: int
        LigibleForProgression: bool
        EnemyMissingPings: int
        EnemyVisionPings: int
        FirstBloodAssist: bool
        FirstBloodKill: bool
        FirstTowerAssist: bool
        FirstTowerKill: bool
        GameEndedInEarlySurrender: bool
        GameEndedInSurrender: bool
        HoldPings: int
        GetBackPings: int
        GoldEarned: int
        GoldSpent: int
        IndividualPosition: string
        InhibitorKills: int
        InhibitorTakedowns: int
        InhibitorsLost: int
        Item0: int
        Item1: int
        Item2: int
        Item3: int
        Item4: int
        Item5: int
        Item6: int
        ItemsPurchased: int
        KillingSprees: int
        Kills: int
        Lane: string
        LargestCriticalStrike: int
        LargestKillingSpree: int
        LargestMultiKill: int
        LongestTimeSpentLiving: int
        MagicDamageDealt: int
        MagicDamageDealtToChampions: int
        MagicDamageTaken: int
        // Missions: Missions
        NeutralMinionsKilled: int
        NeedVisionPings: int
        NexusKills: int
        NexusTakedowns: int
        NexusLost: int
        ObjectivesStolen: int
        ObjectivesStolenAssists: int
        OnMyWayPings: int
        ParticipantId: int
        PlayerScore0: int
        PlayerScore1: int
        PlayerScore2: int
        PlayerScore3: int
        PlayerScore4: int
        PlayerScore5: int
        PlayerScore6: int
        PlayerScore7: int
        PlayerScore8: int
        PlayerScore9: int
        PlayerScore10: int
        PlayerScore11: int
        PentaKills: int
        // Perks: Perks
        PhysicalDamageDealt: int
        PhysicalDamageDealtToChampions: int
        PhysicalDamageTaken: int
        Placement: int
        PlayerAugment1: int
        PlayerAugment2: int
        PlayerAugment3: int
        PlayerAugment4: int
        PlayerSubteamId: int
        PushPings: int
        ProfileIcon: int
        Puuid: string
        QuadraKills: int
        RiotIdGameName: string
        RiotIdTagline: string
        Role: string
        SightWardsBoughtInGame: int
        Spell1Casts: int
        Spell2Casts: int
        Spell3Casts: int
        Spell4Casts: int
        SubteamPlacement: int
        Summoner1Casts: int
        Summoner1Id: int
        Summoner2Casts: int
        Summoner2Id: int
        SummonerId: string
        SummonerLevel: int
        SummonerName: string
        TeamEarlySurrendered: bool
        TeamId: int
        TeamPosition: string
        TimeCCingOthers: int
        TimePlayed: int
        TotalAllyJungleMinionsKilled: int
        TotalDamageDealt: int
        TotalDamageDealtToChampions: int
        TotalDamageShieldedOnTeammates: int
        TotalDamageTaken: int
        TotalEnemyJungleMinionsKilled: int
        TotalHeal: int
        TotalHealsOnTeammates: int
        TotalMinionsKilled: int
        TotalTimeCCDealt: int
        TotalTimeSpentDead: int
        TotalUnitsHealed: int
        TripleKills: int
        TrueDamageDealt: int
        TrueDamageDealtToChampions: int
        TrueDamageTaken: int
        TurretKills: int
        TurretTakedowns: int
        TurretsLost: int
        UnrealKills: int
        VisionScore: int
        VisionClearedPings: int
        VisionWardsBoughtInGame: int
        WardsKilled: int
        WardsPlaced: int
        Win: bool
    }

    and MatchTeam = {
        // Bans: Ban list
        // Objectives: Objectives
        TeamId: int
        Win: bool
    }
