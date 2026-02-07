namespace Commands

open Configuration
open Commands

module Commands =

    let private toKeyValuePair c =
        match appConfig.Env with
        | Dev ->
            match c.Aliases with
            | [] -> [ $"dev_{c.Name}", c ]
            | aliases -> c.Name :: aliases |> List.map (fun a -> $"dev_{a}", c)
        | Prod ->
            match c.Aliases with
            | [] -> [ c.Name, c ]
            | aliases -> c.Name :: aliases |> List.map (fun a -> a, c)

    let commandsList =
        [
            Command.create ("accountage", [ "accage" ], HelpInfo.AccountAge, Async accountAge, 10000, false)
            Command.create ("addbetween", [ "ab" ], HelpInfo.AddBetween, Sync addBetween, 10000, false)
            Command.create ("alias", [ $"{appConfig.Bot.CommandPrefix}" ], HelpInfo.Alias, Alias alias, 5000, false)
            Command.create ("apod", [], HelpInfo.AstronomyPictureOfTheDay, Async apod, 20000, false)
            Command.create ("braille", [ "ascii" ], HelpInfo.Braille, Async braille, 20000, false)
            Command.create ("calculator", [ "calc" ; "math" ], HelpInfo.Calculator, Sync calculate, 5000, false)
            Command.create ("catfact", [], HelpInfo.CatFact, Async catFact, 20000, false)
            Command.create ("chance", [ "%" ], HelpInfo.Chance, Sync chance, 10000, false)
            Command.create ("chatsummary", [], HelpInfo.ChatSummary, Async chatSummary, 20000, false)
            Command.create ("channel", [], HelpInfo.Channel, Async channel, 20000, false)
            Command.create ("coinflip", [ "cf" ], HelpInfo.CoinFlip, Sync coinFlip, 10000, false)
            Command.create ("didyouknow", [ "dyk" ], HelpInfo.DidYouKnow, Async didYouKnow, 10000, false)
            Command.create ("eightball", ["8ball"], HelpInfo.Eightball, Sync eightball, 10000, false)
            Command.create ("echo", [], HelpInfo.Echo, Sync echo, 5000, true)
            Command.create ("encode", [], HelpInfo.Encode, Sync encode, 5000, false)
            Command.create ("faceit", [], HelpInfo.FaceIt, Async faceit, 20000, false)
            Command.create ("fill", [], HelpInfo.Fill, Sync fill, 10000, false)
            Command.create ("followage", [ "fa" ], HelpInfo.FollowAge, Async followAge, 20000, false)
            Command.create ("gpt", [], HelpInfo.Gpt, Async gpt, 15000, false)
            // Command.create ("gptimage", [], HelpInfo.Gpt, ASync gptImage, 15000, false)
            Command.create ("help", [],  HelpInfo.Help, Help help, 10000, false)
            Command.create ("joinchannel", [], HelpInfo.JoinChannel, Async joinChannel, 5000, true)
            Command.create ("lastline", [ "ll" ], HelpInfo.LastLine, Async lastLine, 5000, false)
            Command.create ("leavechannel", [], HelpInfo.LeaveChannel, Async leaveChannel, 5000, true)
            Command.create ("leagueoflegends", [ "lol" ; "league" ], HelpInfo.LeagueOfLegends, Async league, 15000, false)
            Command.create ("namecolor", [ "color" ], HelpInfo.NameColor, Async namecolor, 20000, false)
            Command.create ("news", [], HelpInfo.News, Async news, 15000, false)
            Command.create ("onthisday", [ "otd" ], HelpInfo.Pick, Async onThisDay, 10000, false)
            Command.create ("pick", [], HelpInfo.Pick, Sync pick, 10000, false)
            Command.create ("pipe", [], HelpInfo.Pipe, Sync pipe, 10000, false)
            Command.create ("ping", [], HelpInfo.Ping, Sync ping, 5000, false)
            Command.create ("randomclip", [ "rc" ], HelpInfo.RandomClip, Async randomClip, 20000, false)
            Command.create ("randomemote", [], HelpInfo.RandomEmote, Sync randomEmote, 5000, false)
            Command.create ("randomline", [ "rl" ], HelpInfo.RandomLine, Async randomLine, 10000, false)
            Command.create ("randomquote", [ "rq" ], HelpInfo.RandomQuote, Async randomQuote, 10000, false)
            Command.create ("reddit", [], HelpInfo.Reddit, Async reddit, 15000, false)
            Command.create ("refreshchannelemotes", [ "rce" ], HelpInfo.RefreshChannelEmotes, Sync refreshChannelEmotes, 5000, true)
            Command.create ("refreshglobalemotes", [ "rge" ], HelpInfo.RefreshGlobalEmotes, Sync refreshGlobalEmotes, 5000, true)
            Command.create ("remind", [ "notify" ], HelpInfo.Remind, Async remind, 5000, false)
            Command.create ("rockpaperscissors", [ "rps" ], HelpInfo.RockPaperScissors, Async rps, 10000, false)
            Command.create ("roll", [], HelpInfo.Roll, Sync roll, 10000, false)
            Command.create ("search", [], HelpInfo.Search, Async search, 10000, false)
            Command.create ("slots", [], HelpInfo.Slots, Sync slots, 10000, false)
            Command.create ("stream", [], HelpInfo.Stream, Async stream, 20000, false)
            Command.create ("subage", [ "sa" ], HelpInfo.SubAge, Async subAge, 10000, false)
            Command.create ("time", [], HelpInfo.Time, Async time, 5000, false)
            Command.create ("texttoascii", [ "tta" ], HelpInfo.TextToAscii, Sync textToAscii, 15000, false)
            Command.create ("texttransform", [ "tt" ], HelpInfo.TextTransform, Sync texttransform, 5000, false)
            Command.create ("topstreams", [ "ts" ], HelpInfo.TopStreams, Async topStreams, 20000, false)
            Command.create ("trivia", [], HelpInfo.Trivia, Async trivia, 20000, false)
            Command.create ("urban", [ "ud" ], HelpInfo.UrbanDictionary, Async urban, 20000, false)
            Command.create ("userid", [ "uid" ], HelpInfo.UserId, Async userId, 20000, false)
            Command.create ("vod", [], HelpInfo.Vod, Async vod, 20000, false)
            Command.create ("whatemoteisit", [], HelpInfo.WhatEmoteIsIt, Async whatemoteisit, 10000, false)
            Command.create ("weather", [], HelpInfo.Weather, Async weather, 20000, false)
            Command.create ("wiki", [], HelpInfo.Wiki, Async wiki, 20000, false)
            Command.create ("wikinews", [], HelpInfo.WikiNews, Async wikiNews, 10000, false)
            Command.create ("xd", [], HelpInfo.xd, Sync xd, 60000, false)
        ]

    let commands =
        commandsList
        |> List.map toKeyValuePair
        |> List.collect id
        |> Map.ofList
