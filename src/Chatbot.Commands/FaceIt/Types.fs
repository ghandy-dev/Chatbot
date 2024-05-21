namespace Chatbot.Commands.Types

module FaceIt =

    open System.Text.Json.Serialization

    module Common =

        type Result = {
            Score: Map<string, int>
            Winner: string
        }

    module Players =

        open Common

        type PlayerGame = {
            Region: string
            [<JsonPropertyName("game_player_id")>]
            GamePlayerId: string
            [<JsonPropertyName("skill_level")>]
            SkillLevel: int
            [<JsonPropertyName("faceit_elo")>]
            FaceItElo: int
            [<JsonPropertyName("game_player_name")>]
            GamePlayerName: string
            [<JsonPropertyName("skill_level_label")>]
            SkillLevelLabel: string
            // Regions: string list
            [<JsonPropertyName("game_profile_id")>]
            GameProfileId: string
        }

        type Settings = { Language: string }

        type Player = {
            [<JsonPropertyName("player_id")>]
            PlayerId: string
            Nickname: string
            Avatar: string
            Country: string
            [<JsonPropertyName("cover_image")>]
            CoverImage: string
            Platforms: Map<string, string>
            Games: Map<string, PlayerGame>
            Settings: Settings
            [<JsonPropertyName("friend_ids")>]
            FriendIds: string list
            [<JsonPropertyName("new_steam_id")>]
            NewSteamId: string
            [<JsonPropertyName("steam_id_64")>]
            SteamId64: string
            [<JsonPropertyName("steam_nickname")>]
            SteamNickname: string
            Memberships: string list
            [<JsonPropertyName("faceit_url")>]
            FaceItUrl: string
            [<JsonPropertyName("membership_type")>]
            MembershipType: string
            // Infractions: string
            [<JsonPropertyName("cover_featured_image")>]
            CoverFeaturedImage: string
        }

        type PlayerMatchPlayers = {
            Avatar: string
            [<JsonPropertyName("faceit_url")>]
            FaceItUrl: string
            [<JsonPropertyName("game_player_id")>]
            GamePlayerId: string
            [<JsonPropertyName("game_player_name")>]
            GamePlayerName: string
            Nickname: string
            [<JsonPropertyName("player_id")>]
            PlayerId: string
            [<JsonPropertyName("skill_level")>]
            SkillLevel: int
        }

        type PlayerMatchTeam = {
            Avatar: string
            Nickname: string
            Players: PlayerMatchPlayers list
            [<JsonPropertyName("team_id")>]
            TeamId: string
            Type: string
        }

        type PlayerMatch = {
            [<JsonPropertyName("competition_id")>]
            CompetitionId: string
            [<JsonPropertyName("competition_name")>]
            CompetitionName: string
            [<JsonPropertyName("competition_type")>]
            CompetitionType: string
            [<JsonPropertyName("faceit_url")>]
            FaceItUrl: string
            [<JsonPropertyName("finished_at")>]
            FinishedAt: int
            [<JsonPropertyName("game_id")>]
            GameId: string
            [<JsonPropertyName("game_mode")>]
            GameMode: string
            [<JsonPropertyName("match_id")>]
            MatchId: string
            [<JsonPropertyName("match_type")>]
            MatchType: string
            [<JsonPropertyName("max_players")>]
            MaxPlayers: int
            [<JsonPropertyName("organizer_id")>]
            OrganizerId: string
            [<JsonPropertyName("playing_players")>]
            PlayingPlayers: string list
            Region: string
            Results: Result
            [<JsonPropertyName("started_at")>]
            StartedAt: int
            Status: string
            Teams: Map<string, PlayerMatchTeam>
            [<JsonPropertyName("teams_size")>]
            TeamSize: int
        }

        type PlayerMatchHistory = {
            End: int
            From: int
            Items: PlayerMatch list
            Start: int
            To: int
        }

        type LifetimeStats = {
            [<JsonPropertyName("Current Win Streak")>]
            CurrentWinStreak: string
            [<JsonPropertyName("Average K/D Ratio")>]
            AverageKDRatio: string
            [<JsonPropertyName("K/D Ratio")>]
            KDRatio: string
            Matches: string
            [<JsonPropertyName("Longest Win Streak")>]
            LongestWinStreak: string
            [<JsonPropertyName("Recent Results")>]
            RecentResults: string list
            [<JsonPropertyName("Total Headshots %")>]
            TotalHeadshotsPercentage: string
            [<JsonPropertyName("Win Rate %")>]
            WinRate: string
            Wins: string
            [<JsonPropertyName("Average Headshots %")>]
            AverageHeadshotsPercentage: string
        }

        type PlayerStats = {
            [<JsonPropertyName("player_id")>]
            PlayerId: string
            [<JsonPropertyName("game_id")>]
            GameId: string
            Lifetime: LifetimeStats
        }

    module Matches =

        open Common

        type MatchPlayer = {
            [<JsonPropertyName("player_id")>]
            PlayerId: string
            Nickname: string
            Avatar: string
            Membership: string
            [<JsonPropertyName("game_player_id")>]
            GamePlayerId: string
            [<JsonPropertyName("game_player_name")>]
            GamePlayerName: string
            [<JsonPropertyName("game_skill_level")>]
            GameSkillLevel: int
            AntiCheatRequired: bool
        }

        type MatchTeam = {
            [<JsonPropertyName("faction_id")>]
            FactionId: string
            Leader: string
            Avatar: string
            Roster: MatchPlayer list
            Substituted: bool
            Name: string
            Type: string
        }

        type MatchLocationEntity = {
            [<JsonPropertyName("class_name")>]
            ClassName: string
            [<JsonPropertyName("game_location_id")>]
            GameLocationId: string
            Guid: string
            [<JsonPropertyName("image_lg")>]
            ImageLg: string
            [<JsonPropertyName("image_sm")>]
            ImageSm: string
            Name: string
        }

        type MatchLocation = {
            Entities: MatchLocationEntity list
            Pick: string list
        }

        type MatchMapEntity = {
            Name: string
            [<JsonPropertyName("class_name")>]
            ClassName: string
            [<JsonPropertyName("game_map_id")>]
            GameMapId: string
            Guid: string
            [<JsonPropertyName("image_lg")>]
            ImageLg: string
            [<JsonPropertyName("image_sm")>]
            ImageSm: string
        }

        type MatchMap = {
            Entities: MatchMapEntity list
            Pick: string list
        }

        type VotingEntityType = {
            [<JsonPropertyName("voted_entity_types")>]
            VotedEntityTypes: string list
            Location: MatchLocation
            Map: MatchMap
        }

        type Match = {
            [<JsonPropertyName("match_id")>]
            MatchId: string
            Version: int
            Game: string
            Region: string
            [<JsonPropertyName("competition_id")>]
            CompetitionId: string
            [<JsonPropertyName("competition_type")>]
            CompetitionType: string
            [<JsonPropertyName("competition_name")>]
            CompetitionName: string
            [<JsonPropertyName("organizer_id")>]
            OrganizerId: string
            Teams: Map<string, MatchTeam>
            Voting: VotingEntityType
            [<JsonPropertyName("calculate_elo")>]
            CalculateElo: bool
            [<JsonPropertyName("configured_at")>]
            ConfiguredAt: int
            [<JsonPropertyName("started_at")>]
            StartedAt: int
            [<JsonPropertyName("finished_at")>]
            FinishedAt: int
            [<JsonPropertyName("demo_url")>]
            DemoUrl: string list
            [<JsonPropertyName("chat_room_id")>]
            ChatRoomId: string
            [<JsonPropertyName("best_of")>]
            BestOf: int
            Results: Result
        }
