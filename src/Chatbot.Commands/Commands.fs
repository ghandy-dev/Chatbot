namespace Chatbot.Commands

open Chatbot.Commands
open Chatbot.Commands.FaceIt
open Chatbot.Commands.Logs
open Chatbot.Commands.OpenAI
open Chatbot.Commands.Reddit

module Commands =

    let private toKeyValue c =
        match c.Aliases with
        | [] -> [ (c.Name), c ]
        | aliases -> c.Name :: aliases |> List.map (fun a -> (a), c)

    let commandsMap =
        [
            Command.createCommand ("%", [], HelpInfo.Percentage, SyncFunction percentage, 10000, false)
            Command.createCommand ("addbetween", [ "ab" ], HelpInfo.AddBetween, SyncFunctionWithArgs addBetween, 10000, false)
            Command.createCommand ("alias", [ $"{Chatbot.Configuration.Bot.config.CommandPrefix}" ], HelpInfo.Alias, AsyncFunctionWithArgsAndContext alias, 5000, false)
            Command.createCommand ("braille", [], HelpInfo.Braille, AsyncFunctionWithArgs braille, 20000, false)
            Command.createCommand ("calculator", [ "calc" ], HelpInfo.Calculator, SyncFunctionWithArgs calculate, 5000, false)
            Command.createCommand ("catfact", [], HelpInfo.CatFact, AsyncFunction catFact, 20000, false)
            Command.createCommand ("coinflip", [ "cf" ], HelpInfo.CoinFlip, SyncFunction coinFlip, 10000, false)
            Command.createCommand ("eightball", ["8ball"], HelpInfo.Eightball, SyncFunction eightball, 10000, false)
            Command.createCommand ("echo", [], HelpInfo.Echo, SyncFunctionWithArgs echo, 5000, true)
            Command.createCommand ("encode", [], HelpInfo.Encode, SyncFunctionWithArgs encode, 5000, false)
            Command.createCommand ("faceit", [], HelpInfo.FaceIt, AsyncFunctionWithArgs faceit, 20000, false)
            Command.createCommand ("gpt", [], HelpInfo.Gpt, AsyncFunctionWithArgsAndContext gpt, 15000, false)
            Command.createCommand ("help", [], "\nDisplay help info about commands.", SyncFunction help, 10000, false)
            Command.createCommand ("joinchannel", [], HelpInfo.JoinChannel, AsyncFunctionWithArgs joinChannel, 5000, true)
            Command.createCommand ("leavechannel", [], HelpInfo.LeaveChannel, AsyncFunctionWithArgs leaveChannel, 5000, true)
            Command.createCommand ("namecolor", [ "color" ], HelpInfo.NameColor, AsyncFunctionWithArgsAndContext namecolor, 20000, false)
            Command.createCommand ("pick", [], HelpInfo.Pick, SyncFunctionWithArgs pick, 10000, false)
            Command.createCommand ("pipe", [], HelpInfo.Pipe, SyncFunctionWithArgs pipe, 10000, false)
            Command.createCommand ("ping", [], HelpInfo.Ping, SyncFunction ping, 5000, false)
            Command.createCommand ("randomclip", [ "rc" ], HelpInfo.RandomClip, AsyncFunctionWithArgsAndContext randomClip, 20000, false)
            Command.createCommand ("randomline", [ "rl" ], HelpInfo.RandomLine, AsyncFunctionWithArgsAndContext randomLine, 10000, false)
            Command.createCommand ("randomquote", [ "rq" ], HelpInfo.RandomQuote, AsyncFunctionWithArgsAndContext randomQuote, 10000, false)
            Command.createCommand ("reddit", [], HelpInfo.Reddit, AsyncFunctionWithArgs reddit, 15000, false)
            Command.createCommand ("rockpaperscissors", [ "rps" ], HelpInfo.RockPaperScissors, AsyncFunctionWithArgsAndContext rps, 10000, false)
            Command.createCommand ("roll", [], HelpInfo.Roll, SyncFunctionWithArgs roll, 10000, false)
            Command.createCommand ("stream", [], HelpInfo.Stream, AsyncFunctionWithArgs stream, 20000, false)
            Command.createCommand ("time", [], HelpInfo.Time, SyncFunction time, 5000, false)
            Command.createCommand ("texttransform", [ "tt" ], HelpInfo.TextTransform, SyncFunctionWithArgs texttransform, 5000, false)
            Command.createCommand ("topstreams", [ "ts" ], HelpInfo.TopStreams, AsyncFunction topStreams, 20000, false)
            Command.createCommand ("userid", [ "uid" ], HelpInfo.UserId, AsyncFunctionWithArgsAndContext userId, 20000, false)
            Command.createCommand ("vod", [], HelpInfo.Vod, AsyncFunctionWithArgs vod, 20000, false)
            Command.createCommand ("wiki", [], HelpInfo.Wiki, AsyncFunctionWithArgs wiki, 20000, false)
            Command.createCommand ("xd", [], HelpInfo.xd, SyncFunction xd, 60000, false)
        ]

    let commands =
        commandsMap
        |> List.map toKeyValue
        |> List.collect id
        |> Map.ofList
