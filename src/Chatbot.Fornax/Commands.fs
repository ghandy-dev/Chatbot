module Commands

open Chatbot.Commands

type CommandInfo = {
    Name: string
    Aliases: string list
    HelpInfo: Chatbot.Core.Domain.Commands.Details
    Cooldown: int
    AdminOnly: bool
    CanPipe: bool
} with

    static member create name aliases helpInfo cooldown adminOnly pipe = {
        Name = name
        Aliases = aliases
        HelpInfo = helpInfo
        Cooldown = cooldown
        AdminOnly = adminOnly
        CanPipe = pipe
    }

let commands =
    [
        CommandInfo.create "accountage" [ "accage" ] HelpInfo.AccountAge 10 false true
        CommandInfo.create "addbetween" [ "ab" ] HelpInfo.AddBetween 10 false true
        CommandInfo.create "alias" [] HelpInfo.Alias 10 false false
        CommandInfo.create "apod" [] HelpInfo.AstronomyPictureOfTheDay 20 false true
        CommandInfo.create "braille" [ "ascii" ] HelpInfo.Braille 20 false true
        CommandInfo.create "calculator" [ "calc" ; "math" ] HelpInfo.Calculator 5 false true
        CommandInfo.create "catfact" [] HelpInfo.CatFact 20 false true
        CommandInfo.create "chance" [ "%" ] HelpInfo.Chance 10 false true
        CommandInfo.create "chatsummary" [] HelpInfo.ChatSummary 20 false true
        CommandInfo.create "channel" [] HelpInfo.Channel 20 false true
        CommandInfo.create "coinflip" [ "cf" ] HelpInfo.CoinFlip 10 false true
        CommandInfo.create "didyouknow" [ "dyk" ] HelpInfo.DidYouKnow 10 false true
        CommandInfo.create "eightball" ["8ball"] HelpInfo.Eightball 10 false true
        CommandInfo.create "echo" [] HelpInfo.Echo 5 true true
        CommandInfo.create "encode" [] HelpInfo.Encode 5 false true
        CommandInfo.create "faceit" [] HelpInfo.FaceIt 20 false true
        CommandInfo.create "fill" [] HelpInfo.Fill 10 false true
        CommandInfo.create "followage" [ "fa" ] HelpInfo.FollowAge 20 false true
        CommandInfo.create "gpt" [] HelpInfo.Gpt 15 false true
        // CommandInfo.create ("gptimage" [] HelpInfo.Gpt15 false))
        CommandInfo.create "help" []  HelpInfo.Help 10 false false
        CommandInfo.create "joinchannel" [] HelpInfo.JoinChannel 5 true false
        CommandInfo.create "lastline" [ "ll" ] HelpInfo.LastLine 5 false true
        CommandInfo.create "leavechannel" [] HelpInfo.LeaveChannel 5 true false
        CommandInfo.create "leagueoflegends" [ "lol" ; "league" ] HelpInfo.LeagueOfLegends 15 false true
        CommandInfo.create "namecolor" [ "color" ] HelpInfo.NameColor 20 false true
        CommandInfo.create "news" [] HelpInfo.News 15 false true
        CommandInfo.create "onthisday" [ "otd" ] HelpInfo.Pick 10 false true
        CommandInfo.create "pick" [] HelpInfo.Pick 10 false true
        CommandInfo.create "ping" [] HelpInfo.Ping 5 false true
        CommandInfo.create "profilepicture" [ "pfp" ] HelpInfo.ProfilePicture 20 false true
        CommandInfo.create "randomclip" [ "rc" ] HelpInfo.RandomClip 20 false true
        CommandInfo.create "randomemote" [] HelpInfo.RandomEmote 5 false true
        CommandInfo.create "randomline" [ "rl" ] HelpInfo.RandomLine 10 false true
        CommandInfo.create "randomquote" [ "rq" ] HelpInfo.RandomQuote 10 false true
        CommandInfo.create "reddit" [] HelpInfo.Reddit 15 false true
        CommandInfo.create "refreshchannelemotes" [ "rce" ] HelpInfo.RefreshChannelEmotes 5 true false
        CommandInfo.create "refreshglobalemotes" [ "rge" ] HelpInfo.RefreshGlobalEmotes 5 true false
        CommandInfo.create "remind" [ "notify" ] HelpInfo.Remind 5 false true
        CommandInfo.create "rockpaperscissors" [ "rps" ] HelpInfo.RockPaperScissors 10 false true
        CommandInfo.create "roll" [] HelpInfo.Roll 10 false true
        CommandInfo.create "search" [] HelpInfo.Search 10 false true
        CommandInfo.create "slots" [] HelpInfo.Slots 10 false true
        CommandInfo.create "stream" [] HelpInfo.Stream 20 false true
        CommandInfo.create "subage" [ "sa" ] HelpInfo.SubAge 10 false true
        CommandInfo.create "time" [] HelpInfo.Time 5 false true
        CommandInfo.create "texttoascii" [ "tta" ] HelpInfo.TextToAscii 15 false true
        CommandInfo.create "texttransform" [ "tt" ] HelpInfo.TextTransform 5 false true
        CommandInfo.create "topstreams" [ "ts" ] HelpInfo.TopStreams 20 false true
        CommandInfo.create "trivia" [] HelpInfo.Trivia 20 false false
        CommandInfo.create "urban" [ "ud" ] HelpInfo.UrbanDictionary 20 false true
        CommandInfo.create "userid" [ "uid" ] HelpInfo.UserId 20 false true
        CommandInfo.create "vod" [] HelpInfo.Vod 20 false true
        CommandInfo.create "whatemoteisit" [] HelpInfo.WhatEmoteIsIt 10 false true
        CommandInfo.create "weather" [] HelpInfo.Weather 20 false true
        CommandInfo.create "wiki" [] HelpInfo.Wiki 20 false true
        CommandInfo.create "wikinews" [] HelpInfo.WikiNews 10 false true
        CommandInfo.create "xd" [] HelpInfo.xd 60 false true
    ]