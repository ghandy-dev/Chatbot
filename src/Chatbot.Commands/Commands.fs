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
            Command.createCommand ("accountage", [ "accage" ], HelpInfo.AccountAge, AAC accountAge, 10000, false)
            Command.createCommand ("addbetween", [ "ab" ], HelpInfo.AddBetween, SA addBetween, 10000, false)
            Command.createCommand ("alias", [ $"{appConfig.Bot.CommandPrefix}" ], HelpInfo.Alias, AACM alias, 5000, false)
            Command.createCommand ("apod", [], HelpInfo.AstronomyPictureOfTheDay, AA apod, 20000, false)
            Command.createCommand ("braille", [ "ascii" ], HelpInfo.Braille, AAC braille, 20000, false)
            Command.createCommand ("calculator", [ "calc" ], HelpInfo.Calculator, SA calculate, 5000, false)
            Command.createCommand ("catfact", [], HelpInfo.CatFact, A catFact, 20000, false)
            Command.createCommand ("chance", [ "%" ], HelpInfo.Chance, S chance, 10000, false)
            Command.createCommand ("channel", [], HelpInfo.Channel, AA channel, 20000, false)
            Command.createCommand ("coinflip", [ "cf" ], HelpInfo.CoinFlip, S coinFlip, 10000, false)
            // Command.createCommand ("dungeon", [], HelpInfo.Dungeon, AAC dungeon, 10000, false)
            Command.createCommand ("eightball", ["8ball"], HelpInfo.Eightball, S eightball, 10000, false)
            Command.createCommand ("echo", [], HelpInfo.Echo, SA echo, 5000, true)
            Command.createCommand ("encode", [], HelpInfo.Encode, SA encode, 5000, false)
            Command.createCommand ("faceit", [], HelpInfo.FaceIt, AA faceit, 20000, false)
            Command.createCommand ("fill", [], HelpInfo.Fill, SA fill, 10000, false)
            Command.createCommand ("followage", [ "fa" ], HelpInfo.FollowAge, AAC followAge, 20000, false)
            Command.createCommand ("gpt", [], HelpInfo.Gpt, AAC gpt, 15000, false)
            Command.createCommand ("help", [],  HelpInfo.Help, S help, 10000, false)
            Command.createCommand ("joinchannel", [], HelpInfo.JoinChannel, AA joinChannel, 5000, true)
            Command.createCommand ("leavechannel", [], HelpInfo.LeaveChannel, AA leaveChannel, 5000, true)
            Command.createCommand ("leagueoflegends", [ "lol" ; "league" ], HelpInfo.LeagueOfLegends, AA league, 15000, false)
            Command.createCommand ("namecolor", [ "color" ], HelpInfo.NameColor, AAC namecolor, 20000, false)
            Command.createCommand ("news", [], HelpInfo.News, AA news, 15000, false)
            Command.createCommand ("pick", [], HelpInfo.Pick, SA pick, 10000, false)
            Command.createCommand ("pipe", [], HelpInfo.Pipe, SA pipe, 10000, false)
            Command.createCommand ("ping", [], HelpInfo.Ping, S ping, 5000, false)
            Command.createCommand ("randomclip", [ "rc" ], HelpInfo.RandomClip, AAC randomClip, 20000, false)
            Command.createCommand ("randomemote", [], HelpInfo.RandomEmote, SAC randomEmote, 5000, false)
            Command.createCommand ("randomline", [ "rl" ], HelpInfo.RandomLine, AAC randomLine, 10000, false)
            Command.createCommand ("randomquote", [ "rq" ], HelpInfo.RandomQuote, AAC randomQuote, 10000, false)
            Command.createCommand ("reddit", [], HelpInfo.Reddit, AA reddit, 15000, false)
            Command.createCommand ("refreshchannelemotes", [ "rce" ], HelpInfo.RefreshChannelEmotes, SAC refreshChannelEmotes, 5000, true)
            Command.createCommand ("refreshglobalemotes", [ "rge" ], HelpInfo.RefreshGlobalEmotes, SA refreshGlobalEmotes, 5000, true)
            Command.createCommand ("remind", [ "notify" ], HelpInfo.Remind, AAC remind, 5000, false)
            Command.createCommand ("rockpaperscissors", [ "rps" ], HelpInfo.RockPaperScissors, AAC rps, 10000, false)
            Command.createCommand ("roll", [], HelpInfo.Roll, SA roll, 10000, false)
            Command.createCommand ("stream", [], HelpInfo.Stream, AA stream, 20000, false)
            Command.createCommand ("subage", [ "sa" ], HelpInfo.SubAge, AAC subAge, 10000, false)
            Command.createCommand ("time", [], HelpInfo.Time, AA time, 5000, false)
            Command.createCommand ("texttoascii", [ "tta" ], HelpInfo.TextToAscii, SA textToAscii, 15000, false)
            Command.createCommand ("texttransform", [ "tt" ], HelpInfo.TextTransform, SA texttransform, 5000, false)
            Command.createCommand ("topstreams", [ "ts" ], HelpInfo.TopStreams, A topStreams, 20000, false)
            Command.createCommand ("trivia", [], HelpInfo.Trivia, AAC trivia, 20000, false)
            Command.createCommand ("urban", [ "ud" ], HelpInfo.UrbanDictionary, AA urban, 20000, false)
            Command.createCommand ("userid", [ "uid" ], HelpInfo.UserId, AAC userId, 20000, false)
            Command.createCommand ("vod", [], HelpInfo.Vod, AA vod, 20000, false)
            Command.createCommand ("whatemoteisit", [], HelpInfo.WhatEmoteIsIt, AAC whatemoteisit, 10000, false)
            Command.createCommand ("weather", [], HelpInfo.Weather, AA weather, 20000, false)
            Command.createCommand ("wiki", [], HelpInfo.Wiki, AA wiki, 20000, false)
            Command.createCommand ("xd", [], HelpInfo.xd, S xd, 60000, false)
        ]

    let commands =
        commandsList
        |> List.map toKeyValuePair
        |> List.collect id
        |> Map.ofList
