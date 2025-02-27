namespace IRC

module Messages =

    open IRC.Parsing.Types

    [<AutoOpen>]
    module Types =

        type NoticeEventType =
            | AlreadyBanned
            | AlreadyEmoteOnlyOff
            | AlreadyEmoteOnlyOn
            | AlreadyFollowersOff
            | AlreadyFollowersOn
            | AlreadyR9kOff
            | AlreadyR9kOn
            | AlreadySlowOff
            | AlreadySlowOn
            | AlreadySubsOff
            | AlreadySubsOn
            | AutohostReceive
            | BadBanAdmin
            | BadBanAnon
            | BadBanBroadcaster
            | BadBanMod
            | BadBanSelf
            | BadBanStaff
            | BadCommercialError
            | BadDeleteMessageBroadcaster
            | BadDeleteMessageMod
            | BadHostError
            | BadHostHosting
            | BadHostRateExceeded
            | BadHostRejected
            | BadHostSelf
            | BadModBanned
            | BadModMod
            | BadSlowDuration
            | BadTimeoutAdmin
            | BadTimeoutAnon
            | BadTimeoutBroadcaster
            | BadTimeoutDuration
            | BadTimeoutMod
            | BadTimeoutSelf
            | BadTimeoutStaff
            | BadUnbanNoBan
            | BadUnhostError
            | BadUnmodMod
            | BadVipGranteeBanned
            | BadVipGranteeAlreadyVip
            | BadVipMaxVipsReached
            | BadVipAchievementIncomplete
            | BadUnvipGranteeNotVip
            | BanSuccess
            | CmdsAvailable
            | ColorChanged
            | CommercialSuccess
            | DeleteMessageSuccess
            | DeleteStaffMessageSuccess
            | EmoteOnlyOff
            | EmoteOnlyOn
            | FollowersOff
            | FollowersOn
            | FollowersOnZero
            | HostOff
            | HostOn
            | HostReceive
            | HostReceiveNoCount
            | HostTargetWentOffline
            | HostsRemaining
            | InvalidUser
            | ModSuccess
            | MsgBanned
            | MsgBadCharacters
            | MsgChannelBlocked
            | MsgChannelSuspended
            | MsgDuplicate
            | MsgEmoteonly
            | MsgFollowersonly
            | MsgFollowersonlyFollowed
            | MsgFollowersonlyZero
            | MsgR9k
            | MsgRatelimit
            | MsgRejected
            | MsgRejectedMandatory
            | MsgRequiresVerifiedPhoneNumber
            | MsgSlowmode
            | MsgSubsonly
            | MsgSuspended
            | MsgTimedout
            | MsgVerifiedEmail
            | NoHelp
            | NoMods
            | NoVips
            | NotHosting
            | NoPermission
            | R9kOff
            | R9kOn
            | RaidErrorAlreadyRaiding
            | RaidErrorForbidden
            | RaidErrorSelf
            | RaidErrorTooManyViewers
            | RaidErrorUnexpected
            | RaidNoticeMature
            | RaidNoticeRestrictedChat
            | RoomMods
            | SlowOff
            | SlowOn
            | SubsOff
            | SubsOn
            | TimeoutNoTimeout
            | TimeoutSuccess
            | TosBan
            | TurboOnlyColor
            | UnavailableCommand
            | UnbanSuccess
            | UnmodSuccess
            | UnraidErrorNoActiveRaid
            | UnraidErrorUnexpected
            | UnraidSuccess
            | UnrecognizedCmd
            | UntimeoutBanned
            | UntimeoutSuccess
            | UnvipSuccess
            | UsageBan
            | UsageClear
            | UsageColor
            | UsageCommercial
            | UsageDisconnect
            | UsageDelete
            | UsageEmoteOnlyOff
            | UsageEmoteOnlyOn
            | UsageFollowersOff
            | UsageFollowersOn
            | UsageHelp
            | UsageHost
            | UsageMarker
            | UsageMe
            | UsageMod
            | UsageMods
            | UsageR9kOff
            | UsageR9kOn
            | UsageRaid
            | UsageSlowOff
            | UsageSlowOn
            | UsageSubsOff
            | UsageSubsOn
            | UsageTimeout
            | UsageUnban
            | UsageUnhost
            | UsageUnmod
            | UsageUnraid
            | UsageUntimeout
            | UsageUnvip
            | UsageUser
            | UsageVip
            | UsageVips
            | UsageWhisper
            | VipSuccess
            | VipsSuccess
            | WhisperBanned
            | WhisperBannedRecipient
            | WhisperInvalidLogin
            | WhisperInvalidSelf
            | WhisperLimitPerMin
            | WhisperLimitPerSec
            | WhisperRestricted
            | WhisperRestrictedRecipient
            | Unknown of string

            static member parse =
                function
                | "already_banned" -> AlreadyBanned
                | "already_emote_only_off" -> AlreadyEmoteOnlyOff
                | "already_emote_only_on" -> AlreadyEmoteOnlyOn
                | "already_followers_off" -> AlreadyFollowersOff
                | "already_followers_on" -> AlreadyFollowersOn
                | "already_r9k_off" -> AlreadyR9kOff
                | "already_r9k_on" -> AlreadyR9kOn
                | "already_slow_off" -> AlreadySlowOff
                | "already_slow_on" -> AlreadySlowOn
                | "already_subs_off" -> AlreadySubsOff
                | "already_subs_on" -> AlreadySubsOn
                | "autohost_receive" -> AutohostReceive
                | "bad_ban_admin" -> BadBanAdmin
                | "bad_ban_anon" -> BadBanAnon
                | "bad_ban_broadcaster" -> BadBanBroadcaster
                | "bad_ban_mod" -> BadBanMod
                | "bad_ban_self" -> BadBanSelf
                | "bad_ban_staff" -> BadBanStaff
                | "bad_commercial_error" -> BadCommercialError
                | "bad_delete_message_broadcaster" -> BadDeleteMessageBroadcaster
                | "bad_delete_message_mod" -> BadDeleteMessageMod
                | "bad_host_error" -> BadHostError
                | "bad_host_hosting" -> BadHostHosting
                | "bad_host_rate_exceeded" -> BadHostRateExceeded
                | "bad_host_rejected" -> BadHostRejected
                | "bad_host_self" -> BadHostSelf
                | "bad_mod_banned" -> BadModBanned
                | "bad_mod_mod" -> BadModMod
                | "bad_slow_duration" -> BadSlowDuration
                | "bad_timeout_admin" -> BadTimeoutAdmin
                | "bad_timeout_anon" -> BadTimeoutAnon
                | "bad_timeout_broadcaster" -> BadTimeoutBroadcaster
                | "bad_timeout_duration" -> BadTimeoutDuration
                | "bad_timeout_mod" -> BadTimeoutMod
                | "bad_timeout_self" -> BadTimeoutSelf
                | "bad_timeout_staff" -> BadTimeoutStaff
                | "bad_unban_no_ban" -> BadUnbanNoBan
                | "bad_unhost_error" -> BadUnhostError
                | "bad_unmod_mod" -> BadUnmodMod
                | "bad_vip_grantee_banned" -> BadVipGranteeBanned
                | "bad_vip_grantee_already_vip" -> BadVipGranteeAlreadyVip
                | "bad_vip_max_vips_reached" -> BadVipMaxVipsReached
                | "bad_vip_achievement_incomplete" -> BadVipAchievementIncomplete
                | "bad_unvip_grantee_not_vip" -> BadUnvipGranteeNotVip
                | "ban_success" -> BanSuccess
                | "cmds_available" -> CmdsAvailable
                | "color_changed" -> ColorChanged
                | "commercial_success" -> CommercialSuccess
                | "delete_message_success" -> DeleteMessageSuccess
                | "delete_staff_message_success" -> DeleteStaffMessageSuccess
                | "emote_only_off" -> EmoteOnlyOff
                | "emote_only_on" -> EmoteOnlyOn
                | "followers_off" -> FollowersOff
                | "followers_on" -> FollowersOn
                | "followers_on_zero" -> FollowersOnZero
                | "host_off" -> HostOff
                | "host_on" -> HostOn
                | "host_receive" -> HostReceive
                | "host_receive_no_count" -> HostReceiveNoCount
                | "host_target_went_offline" -> HostTargetWentOffline
                | "hosts_remaining" -> HostsRemaining
                | "invalid_user" -> InvalidUser
                | "mod_success" -> ModSuccess
                | "msg_banned" -> MsgBanned
                | "msg_bad_characters" -> MsgBadCharacters
                | "msg_channel_blocked" -> MsgChannelBlocked
                | "msg_channel_suspended" -> MsgChannelSuspended
                | "msg_duplicate" -> MsgDuplicate
                | "msg_emoteonly" -> MsgEmoteonly
                | "msg_followersonly" -> MsgFollowersonly
                | "msg_followersonly_followed" -> MsgFollowersonlyFollowed
                | "msg_followersonly_zero" -> MsgFollowersonlyZero
                | "msg_r9k" -> MsgR9k
                | "msg_ratelimit" -> MsgRatelimit
                | "msg_rejected" -> MsgRejected
                | "msg_rejected_mandatory" -> MsgRejectedMandatory
                | "msg_requires_verified_phone_number" -> MsgRequiresVerifiedPhoneNumber
                | "msg_slowmode" -> MsgSlowmode
                | "msg_subsonly" -> MsgSubsonly
                | "msg_suspended" -> MsgSuspended
                | "msg_timedout" -> MsgTimedout
                | "msg_verified_email" -> MsgVerifiedEmail
                | "no_help" -> NoHelp
                | "no_mods" -> NoMods
                | "no_vips" -> NoVips
                | "not_hosting" -> NotHosting
                | "no_permission" -> NoPermission
                | "r9k_off" -> R9kOff
                | "r9k_on" -> R9kOn
                | "raid_error_already_raiding" -> RaidErrorAlreadyRaiding
                | "raid_error_forbidden" -> RaidErrorForbidden
                | "raid_error_self" -> RaidErrorSelf
                | "raid_error_too_many_viewers" -> RaidErrorTooManyViewers
                | "raid_error_unexpected" -> RaidErrorUnexpected
                | "raid_notice_mature" -> RaidNoticeMature
                | "raid_notice_restricted_chat" -> RaidNoticeRestrictedChat
                | "room_mods" -> RoomMods
                | "slow_off" -> SlowOff
                | "slow_on" -> SlowOn
                | "subs_off" -> SubsOff
                | "subs_on" -> SubsOn
                | "timeout_no_timeout" -> TimeoutNoTimeout
                | "timeout_success" -> TimeoutSuccess
                | "tos_ban" -> TosBan
                | "turbo_only_color" -> TurboOnlyColor
                | "unavailable_command" -> UnavailableCommand
                | "unban_success" -> UnbanSuccess
                | "unmod_success" -> UnmodSuccess
                | "unraid_error_no_active_raid" -> UnraidErrorNoActiveRaid
                | "unraid_error_unexpected" -> UnraidErrorUnexpected
                | "unraid_success" -> UnraidSuccess
                | "unrecognized_cmd" -> UnrecognizedCmd
                | "untimeout_banned" -> UntimeoutBanned
                | "untimeout_success" -> UntimeoutSuccess
                | "unvip_success" -> UnvipSuccess
                | "usage_ban" -> UsageBan
                | "usage_clear" -> UsageClear
                | "usage_color" -> UsageColor
                | "usage_commercial" -> UsageCommercial
                | "usage_disconnect" -> UsageDisconnect
                | "usage_delete" -> UsageDelete
                | "usage_emote_only_off" -> UsageEmoteOnlyOff
                | "usage_emote_only_on" -> UsageEmoteOnlyOn
                | "usage_followers_off" -> UsageFollowersOff
                | "usage_followers_on" -> UsageFollowersOn
                | "usage_help" -> UsageHelp
                | "usage_host" -> UsageHost
                | "usage_marker" -> UsageMarker
                | "usage_me" -> UsageMe
                | "usage_mod" -> UsageMod
                | "usage_mods" -> UsageMods
                | "usage_r9k_off" -> UsageR9kOff
                | "usage_r9k_on" -> UsageR9kOn
                | "usage_raid" -> UsageRaid
                | "usage_slow_off" -> UsageSlowOff
                | "usage_slow_on" -> UsageSlowOn
                | "usage_subs_off" -> UsageSubsOff
                | "usage_subs_on" -> UsageSubsOn
                | "usage_timeout" -> UsageTimeout
                | "usage_unban" -> UsageUnban
                | "usage_unhost" -> UsageUnhost
                | "usage_unmod" -> UsageUnmod
                | "usage_unraid" -> UsageUnraid
                | "usage_untimeout" -> UsageUntimeout
                | "usage_unvip" -> UsageUnvip
                | "usage_user" -> UsageUser
                | "usage_vip" -> UsageVip
                | "usage_vips" -> UsageVips
                | "usage_whisper" -> UsageWhisper
                | "vip_success" -> VipSuccess
                | "vips_success" -> VipsSuccess
                | "whisper_banned" -> WhisperBanned
                | "whisper_banned_recipient" -> WhisperBannedRecipient
                | "whisper_invalid_login" -> WhisperInvalidLogin
                | "whisper_invalid_self" -> WhisperInvalidSelf
                | "whisper_limit_per_min" -> WhisperLimitPerMin
                | "whisper_limit_per_sec" -> WhisperLimitPerSec
                | "whisper_restricted" -> WhisperRestricted
                | "whisper_restricted_recipient" -> WhisperRestrictedRecipient
                | et -> Unknown et

        type UserNoticeEventType =
            | Sub
            | ReSub
            | SubGift
            | SubMysteryGift
            | GiftPaidUpgrade
            | RewardGift
            | NonGiftPaidUpgrade
            | Raid
            | Unraid
            | Ritual
            | BitsBadgeTier
            | Unknown of string

            static member parse =
                function
                | "sub" -> Sub
                | "resub" -> ReSub
                | "subgift" -> SubGift
                | "submysterygift" -> SubMysteryGift
                | "giftpaidupgrade" -> GiftPaidUpgrade
                | "rewardgift" -> RewardGift
                | "anongiftpaidupgrade" -> NonGiftPaidUpgrade
                | "raid" -> Raid
                | "unraid" -> Unraid
                | "ritual" -> Ritual
                | "bitsbadgetier" -> BitsBadgeTier
                | et -> Unknown et

        [<RequireQualifiedAccess>]
        type ChannelHost =
            | StoppedHosting
            | Channel of string

        type PingMessage = { message: string }

        type PrivateMessage = {
            Id: string
            Channel: string
            Message: string
            Username: string
            UserId: string
            RoomId: string
            Mod: bool
            Emotes: Map<string, string>
            ReplyParentMessageId: string option
            ReplyParentUserId: string option
            ReplyParentUserLogin: string option
            ReplyParentDisplayName: string option
            ReplyParentMessageBody: string option
            ReplyThreadParentMessageId: string option
            ReplyThreadParentUserLogin: string option
            Bits: string option
        }

        type JoinMessage = {
            Channel: string
            User: string
        }

        type PartMessage = {
            Channel: string
            User: string
        }

        type ClearChatMessage = {
            Channel: string
            RoomId: string
            TmiSentTimestamp: string
        }

        type ClearMsgMessage = {
            Channel: string
            Message: string
            Login: string
            RoomId: string
            TargetMsgId: string
            TmiSentTimestamp: string
        }

        type GlobalUserStateMessage = {
            BadgeInfo: string list
            Badges: string list
            Color: string
            DisplayName: string
            EmoteSets: string list
            UserId: string
            UserType: string
        }

        type HostTargetMessage = {
            HostingChannel: string
            Channel: ChannelHost
            NumberOfViewers: string
        }

        type NoticeMessage = {
            Channel: string
            Message: string
            MsgId: NoticeEventType
        }

        type RoomStateMessage = {
            Channel: string
            EmoteOnly: bool option
            FollowersOnly: bool option
            R9K: bool option
            RoomId: string
            Slow: int option
            SubsOnly: bool option
        }

        type UserNoticeMessage = {
            Channel: string
            Message: string option
            BadgeInfo: string list
            Badges: string list
            DisplayName: string
            Emotes: Map<string, string>
            Id: string
            Login: string
            Moderator: bool
            MsgId: UserNoticeEventType
            RoomId: string
            Subscriber: bool
            SystemMsg: string
            UserId: string
            UserType: string
        }

        type UserStateMessage = {
            Channel: string
            BadgeInfo: string list
            Badges: string list
            Color: string
            DisplayName: string
            EmoteSets: string list
            Id: string option
            Moderator: bool
            Subscriber: bool
            UserType: string
        }

        type WhisperMessage = {
            FromUser: string
            ToUser: string
            Message: string
            Badges: string list
            Color: string
            DisplayName: string
            Emotes: Map<string, string>
            MessageId: string
            ThreadId: string
            UserId: string
            UserType: string
        }

        type CapMessage = {
            CapRequestEnabled: bool
            Capabilities: string array
        }

        type IrcMessageType =
            | PrivateMessage of PrivateMessage
            | CapMessage of CapMessage
            | ClearChatMessage of ClearChatMessage
            | ClearMsgMessage of ClearMsgMessage
            | GlobalUserStateMessage of GlobalUserStateMessage
            | HostTargetMessage of HostTargetMessage
            | JoinMessage of JoinMessage
            | NoticeMessage of NoticeMessage
            | PartMessage of PartMessage
            | PingMessage of PingMessage
            | ReconnectMessage
            | RoomStateMessage of RoomStateMessage
            | UserNoticeMessage of UserNoticeMessage
            | UserStateMessage of UserStateMessage
            | WhisperMessage of WhisperMessage

    module MessageMapping =

        open System.Text.RegularExpressions

        let private parseBadges (badges: string) =
            match badges with
            | "" -> []
            | _ -> badges.Split(",") |> Array.map (fun b -> b.Split("/") |> Array.head) |> List.ofArray

        let private parseEmoteSets (emoteSets: string) =
            match emoteSets with
            | "" -> []
            | _ -> emoteSets.Split(",") |> List.ofArray

        let private parseChannelHost =
            function
            | "-" -> ChannelHost.StoppedHosting
            | channel -> ChannelHost.Channel channel

        let private emotePositionRegex = new Regex("([\w]+):([\d]+)-([\d]+)", RegexOptions.Compiled)

        let private parseEmotes (message: string) (emotes: string) =
            let parseEmotes' (emotePositions: string) =
                let matches = emotePositionRegex.Match(emotePositions)

                match matches.Groups |> Seq.toList with
                | _ :: emoteId :: startPos :: endPos :: _ ->
                        let start = startPos.ValueSpan
                        let startIndex = System.Int32.Parse start
                        let ``end`` = endPos.ValueSpan
                        let endIndex = System.Int32.Parse ``end``

                        Some (message[startIndex..endIndex], emoteId.Value)
                | _ -> None

            emotes.Split(",")
            |> Array.choose parseEmotes'
            |> Map.ofArray

        let (|PrivateMessageCommand|_|) (message: IrcMessage) : PrivateMessage option =
            match message.Command with
            | PrivMsg ->
                let parts = message.Parameters.Split(" ", 2)
                let msg = parts.[1].[1..]

                Some {
                    Channel = parts.[0].[1..]
                    Message = msg
                    Id = message.Tags["id"]
                    Username = message.Tags["display-name"]
                    UserId = message.Tags["user-id"]
                    RoomId = message.Tags["room-id"]
                    Mod = message.Tags["mod"] |> Boolean.parseBit
                    Emotes = parseEmotes msg message.Tags["emotes"]
                    ReplyParentMessageId = message.Tags.TryFind "reply-parent-msg-id"
                    ReplyParentUserId = message.Tags.TryFind "reply-parent-user-id"
                    ReplyParentUserLogin = message.Tags.TryFind "reply-parent-user-login"
                    ReplyParentDisplayName = message.Tags.TryFind "reply-parent-display-name"
                    ReplyParentMessageBody = message.Tags.TryFind "reply-parent-msg-body" |> Option.bind (fun s -> Some (s.Replace(@"\s", " ")))
                    ReplyThreadParentMessageId = message.Tags.TryFind "reply-thread-parent-msg-id"
                    ReplyThreadParentUserLogin = message.Tags.TryFind "reply-thread-parent-user-login"
                    Bits = message.Tags.TryFind "bits"
                }
            | _ -> None

        let (|CapCommand|_|) (message: IrcMessage) : CapMessage option =
            match message.Command with
            | Cap ->
                let parts = message.Parameters.Split(" ")

                let acknowledged, capabilities =
                    match parts |> List.ofArray with
                    | "*" :: "ACK" :: cs ->
                        System.String.Join(" ", cs)
                        |> _.Split(":")
                        |> function
                            | [| _ ; cs |] -> true, cs.Split(" ")
                            | _ -> false, [||]
                    | _ -> false, [||]

                Some {
                    CapRequestEnabled = acknowledged
                    Capabilities = capabilities
                }
            | _ -> None

        let (|ClearChatCommand|_|) (message: IrcMessage) : ClearChatMessage option =
            match message.Command with
            | ClearChat ->
                Some {
                    Channel = message.Parameters[1..]
                    RoomId = message.Tags["room-id"]
                    TmiSentTimestamp = message.Tags["tmi-sent-ts"]
                }
            | _ -> None

        let (|ClearMsgCommand|_|) (message: IrcMessage) : ClearMsgMessage option =
            match message.Command with
            | ClearChat ->
                let parts = message.Parameters.Split(" ", 2)

                Some {
                    Channel = parts[0]
                    Message = parts.[1].[1..]
                    Login = message.Tags["login"]
                    RoomId = message.Tags["room-id"]
                    TargetMsgId = message.Tags["target-msg-id"]
                    TmiSentTimestamp = message.Tags["tmi-sent-ts"]
                }
            | _ -> None

        let (|GlobalUserStateCommand|_|) (message: IrcMessage) : GlobalUserStateMessage option =
            match message.Command with
            | GlobalUserState ->
                Some {
                    BadgeInfo = parseBadges message.Tags["badge-info"]
                    Badges = parseBadges message.Tags["badges"]
                    Color = message.Tags["color"]
                    DisplayName = message.Tags["display-name"]
                    EmoteSets = parseEmoteSets message.Tags["emote-sets"]
                    UserId = message.Tags["user-id"]
                    UserType = message.Tags["user-type"]
                }
            | _ -> None

        let (|HostTargetCommand|_|) (message: IrcMessage) : option<HostTargetMessage> =
            match message.Command with
            | HostTarget ->
                let parts = message.Parameters.Split(" ", 3)

                Some {
                    HostingChannel = parts.[0].[1..]
                    Channel = parseChannelHost parts[1]
                    NumberOfViewers = parts[2]
                }
            | _ -> None

        let (|JoinCommand|_|) (message: IrcMessage) : JoinMessage option =
            match message.Command with
            | Join ->
                Some {
                    Channel = message.Parameters
                    User = message.Source.Value.Nick
                }
            | _ -> None

        let (|PartCommand|_|) (message: IrcMessage) : PartMessage option =
            match message.Command with
            | Part ->
                Some {
                    Channel = message.Parameters
                    User = message.Source.Value.Nick
                }
            | _ -> None

        let (|PingCommand|_|) (message: IrcMessage) : PingMessage option =
            match message.Command with
            | Ping -> Some({ message = message.Parameters[1..] }: PingMessage)
            | _ -> None

        let (|NoticeCommand|_|) (message: IrcMessage) : NoticeMessage option =
            match message.Command with
            | Notice ->
                let parts = message.Parameters.Split(" ", 2)

                Some {
                    Channel = parts.[0].[1..]
                    Message = parts.[1].[1..]
                    MsgId = message.Tags["msg-id"] |> NoticeEventType.parse
                }
            | _ -> None

        let (|ReconnectCommand|_|) (message: IrcMessage) : IrcMessageType option =
            match message.Command with
            | Reconnect -> Some ReconnectMessage
            | _ -> None

        let (|RoomStateCommand|_|) (message: IrcMessage) : RoomStateMessage option =
            match message.Command with
            | RoomState ->
                Some {
                    Channel = message.Parameters.[1..]
                    EmoteOnly = message.Tags.TryFind "emote-only" |> Option.bind Boolean.tryParseBit
                    FollowersOnly = message.Tags.TryFind "followers-only" |> Option.bind Boolean.tryParseBit
                    R9K = message.Tags.TryFind "r9k" |> Option.bind Boolean.tryParseBit
                    RoomId = message.Tags["room-id"]
                    Slow = message.Tags.TryFind "slow" |> Option.bind (fun s -> Int32.tryParse s)
                    SubsOnly = message.Tags.TryFind "subs-only" |> Option.bind Boolean.tryParseBit
                }
            | _ -> None

        let (|UserNoticeCommand|_|) (message: IrcMessage) : UserNoticeMessage option =
            match message.Command with
            | UserNotice ->
                let (channel, msg) =
                    message.Parameters.Split(" ")
                    |> function
                        | [| channel |] -> (channel, None)
                        | [| channel ; msg |] -> (channel, Some msg)
                        | _ -> ("", None)

                Some {
                    Channel = channel
                    Message = msg
                    BadgeInfo = parseBadges message.Tags["badge-info"]
                    Badges = parseBadges message.Tags["badges"]
                    DisplayName = message.Tags["display-name"]
                    Emotes = msg |> Option.bind (fun m -> Some (parseEmotes m message.Tags["emotes"])) |?? Map.empty
                    Id = message.Tags["id"]
                    Login = message.Tags["login"]
                    Moderator = message.Tags["mod"] |> Boolean.parseBit
                    MsgId = message.Tags["msg-id"] |> UserNoticeEventType.parse
                    RoomId = message.Tags["room-id"]
                    Subscriber = message.Tags["subscriber"] |> Boolean.parseBit
                    SystemMsg = message.Tags["system-msg"]
                    UserId = message.Tags["user-id"]
                    UserType = message.Tags["user-type"]
                }
            | _ -> None

        let (|UserStateCommand|_|) (message: IrcMessage) : UserStateMessage option =
            match message.Command with
            | UserState ->
                Some {
                    Channel = message.Parameters.[1..]
                    BadgeInfo = parseBadges message.Tags["badge-info"]
                    Badges = parseBadges message.Tags["badges"]
                    Color = message.Tags["color"]
                    DisplayName = message.Tags["display-name"]
                    EmoteSets = parseEmoteSets message.Tags["emote-sets"]
                    Id = message.Tags.TryFind "id"
                    Moderator = message.Tags["mod"] |> Boolean.parseBit
                    Subscriber = message.Tags["subscriber"] |> Boolean.parseBit
                    UserType = message.Tags["user-type"]
                }
            | _ -> None

        let (|WhisperCommand|_|) (message: IrcMessage) : WhisperMessage option =
            match message.Command with
            | Whisper ->
                let parts = message.Parameters.Split(" ", 2)
                let msg = parts.[1].[1..]

                Some {
                    FromUser = message.Tags["display-name"]
                    ToUser = parts[0]
                    Message = msg
                    Badges = parseBadges message.Tags["badges"]
                    Color = message.Tags["color"]
                    DisplayName = message.Tags["display-name"]
                    Emotes = parseEmotes msg message.Tags["emotes"]
                    MessageId = message.Tags["message-id"]
                    ThreadId = message.Tags["thread-id"]
                    UserId = message.Tags["user-id"]
                    UserType = message.Tags["user-type"]
                }
            | _ -> None

        let mapIrcMessage (ircMessage: IrcMessage) =
            match ircMessage with
            | CapCommand msg -> Some(CapMessage msg)
            | ClearChatCommand msg -> Some(ClearChatMessage msg)
            | ClearMsgCommand msg -> Some(ClearMsgMessage msg)
            | GlobalUserStateCommand msg -> Some(GlobalUserStateMessage msg)
            | HostTargetCommand msg -> Some(HostTargetMessage msg)
            | JoinCommand msg -> Some(JoinMessage msg)
            | PartCommand msg -> Some(PartMessage msg)
            | PingCommand msg -> Some(PingMessage msg)
            | NoticeCommand msg -> Some(NoticeMessage msg)
            | PrivateMessageCommand msg -> Some(PrivateMessage msg)
            | ReconnectCommand _ -> Some(ReconnectMessage)
            | RoomStateCommand msg -> Some(RoomStateMessage msg)
            | UserNoticeCommand msg -> Some(UserNoticeMessage msg)
            | UserStateCommand msg -> Some(UserStateMessage msg)
            | WhisperCommand msg -> Some(WhisperMessage msg)
            | _ -> None
