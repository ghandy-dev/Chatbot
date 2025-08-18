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
            Command.create ("accountage", [ "accage" ], HelpInfo.AccountAge, AAC accountAge, 10000, false)
            Command.create ("addbetween", [ "ab" ], HelpInfo.AddBetween, SA addBetween, 10000, false)
            Command.create ("alias", [ $"{appConfig.Bot.CommandPrefix}" ], HelpInfo.Alias, AACM alias, 5000, false)
            Command.create ("apod", [], HelpInfo.AstronomyPictureOfTheDay, AA apod, 20000, false)
            Command.create ("braille", [ "ascii" ], HelpInfo.Braille, AAC braille, 20000, false)
            Command.create ("calculator", [ "calc" ; "math" ], HelpInfo.Calculator, SA calculate, 5000, false)
            Command.create ("catfact", [], HelpInfo.CatFact, A catFact, 20000, false)
            Command.create ("chance", [ "%" ], HelpInfo.Chance, S chance, 10000, false)
            Command.create ("channel", [], HelpInfo.Channel, AA channel, 20000, false)
            Command.create ("coinflip", [ "cf" ], HelpInfo.CoinFlip, S coinFlip, 10000, false)
            Command.create ("didyouknow", [ "dyk" ], HelpInfo.CoinFlip, AA didYouKnow, 10000, false)
            Command.create ("eightball", ["8ball"], HelpInfo.Eightball, S eightball, 10000, false)
            Command.create ("echo", [], HelpInfo.Echo, SA echo, 5000, true)
            Command.create ("encode", [], HelpInfo.Encode, SA encode, 5000, false)
            Command.create ("faceit", [], HelpInfo.FaceIt, AA faceit, 20000, false)
            Command.create ("fill", [], HelpInfo.Fill, SA fill, 10000, false)
            Command.create ("followage", [ "fa" ], HelpInfo.FollowAge, AAC followAge, 20000, false)
            Command.create ("gpt", [], HelpInfo.Gpt, AAC gpt, 15000, false)
            Command.create ("help", [],  HelpInfo.Help, S help, 10000, false)
            Command.create ("joinchannel", [], HelpInfo.JoinChannel, AA joinChannel, 5000, true)
            Command.create ("lastline", [ "ll" ], HelpInfo.LastLine, AAC lastLine, 5000, true)
            Command.create ("leavechannel", [], HelpInfo.LeaveChannel, AA leaveChannel, 5000, true)
            Command.create ("leagueoflegends", [ "lol" ; "league" ], HelpInfo.LeagueOfLegends, AA league, 15000, false)
            Command.create ("namecolor", [ "color" ], HelpInfo.NameColor, AAC namecolor, 20000, false)
            Command.create ("news", [], HelpInfo.News, AA news, 15000, false)
            Command.create ("onthisday", [ "otd" ], HelpInfo.Pick, AA onThisDay, 10000, false)
            Command.create ("pick", [], HelpInfo.Pick, SA pick, 10000, false)
            Command.create ("pipe", [], HelpInfo.Pipe, SA pipe, 10000, false)
            Command.create ("ping", [], HelpInfo.Ping, S ping, 5000, false)
            Command.create ("randomclip", [ "rc" ], HelpInfo.RandomClip, AAC randomClip, 20000, false)
            Command.create ("randomemote", [], HelpInfo.RandomEmote, SAC randomEmote, 5000, false)
            Command.create ("randomline", [ "rl" ], HelpInfo.RandomLine, AAC randomLine, 10000, false)
            Command.create ("randomquote", [ "rq" ], HelpInfo.RandomQuote, AAC randomQuote, 10000, false)
            Command.create ("reddit", [], HelpInfo.Reddit, AA reddit, 15000, false)
            Command.create ("refreshchannelemotes", [ "rce" ], HelpInfo.RefreshChannelEmotes, SAC refreshChannelEmotes, 5000, true)
            Command.create ("refreshglobalemotes", [ "rge" ], HelpInfo.RefreshGlobalEmotes, SA refreshGlobalEmotes, 5000, true)
            Command.create ("remind", [ "notify" ], HelpInfo.Remind, AAC remind, 5000, false)
            Command.create ("rockpaperscissors", [ "rps" ], HelpInfo.RockPaperScissors, AAC rps, 10000, false)
            Command.create ("roll", [], HelpInfo.Roll, SA roll, 10000, false)
            Command.create ("search", [], HelpInfo.Search, AAC search, 10000, false)
            Command.create ("slots", [], HelpInfo.Slots, SAC slots, 10000, false)
            Command.create ("stream", [], HelpInfo.Stream, AA stream, 20000, false)
            Command.create ("subage", [ "sa" ], HelpInfo.SubAge, AAC subAge, 10000, false)
            Command.create ("time", [], HelpInfo.Time, AA time, 5000, false)
            Command.create ("texttoascii", [ "tta" ], HelpInfo.TextToAscii, SA textToAscii, 15000, false)
            Command.create ("texttransform", [ "tt" ], HelpInfo.TextTransform, SA texttransform, 5000, false)
            Command.create ("topstreams", [ "ts" ], HelpInfo.TopStreams, A topStreams, 20000, false)
            Command.create ("trivia", [], HelpInfo.Trivia, AAC trivia, 20000, false)
            Command.create ("urban", [ "ud" ], HelpInfo.UrbanDictionary, AA urban, 20000, false)
            Command.create ("userid", [ "uid" ], HelpInfo.UserId, AAC userId, 20000, false)
            Command.create ("vod", [], HelpInfo.Vod, AA vod, 20000, false)
            Command.create ("whatemoteisit", [], HelpInfo.WhatEmoteIsIt, AAC whatemoteisit, 10000, false)
            Command.create ("weather", [], HelpInfo.Weather, AA weather, 20000, false)
            Command.create ("wiki", [], HelpInfo.Wiki, AA wiki, 20000, false)
            Command.create ("wikinews", [], HelpInfo.WikiNews, AA wikiNews, 10000, false)
            Command.create ("xd", [], HelpInfo.xd, S xd, 60000, false)
        ]

    let commands =
        commandsList
        |> List.map toKeyValuePair
        |> List.collect id
        |> Map.ofList
