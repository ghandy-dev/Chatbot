namespace Chatbot.Commands

open Chatbot.Commands

module Commands =

    let private toKeyValue c =
        match c.Aliases with
        | [] -> [ (c.Name), c ]
        | aliases -> c.Name :: aliases |> List.map (fun a -> (a), c)

    let commands =
        [
            Command.createCommand ("xd", [], "xd.", SyncFunction xd, 30000, false)
            Command.createCommand ("echo", [], "Echo input back.", SyncFunctionWithArgs echo, 5000, true)
            Command.createCommand ("ping", [], "Ping to check bot is up and running.", SyncFunction ping, 10000, false)
            Command.createCommand ("time", [], "Get current time (UTC).", SyncFunction time, 10000, false)
            Command.createCommand ("%", [], "Returns a percentage from 0-100%.", SyncFunction percentage, 10000, false)
            Command.createCommand ("roll", [], "Roll a random number between a min and max (0-10 by default).", SyncFunctionWithArgs roll, 10000, false)
            Command.createCommand ("coinflip", [ "cf" ], "Flip a coin (heads or tails).", SyncFunction coinFlip, 10000, false)
            Command.createCommand ("8ball", [], "Answer questions about the future.", SyncFunction eightball, 10000, false)
            Command.createCommand ("rockpaperscissors", [ "rps" ], "Play Rock Paper Scissors. e.g. >rps rock.", AsyncFunctionWithArgsAndContext rps, 10000, false)
            Command.createCommand ("calculator", [ "calc" ], "Calculate a mathematical expression.", SyncFunctionWithArgs calculate, 10000, false)
            Command.createCommand ("catfact", [], "Gets a random cat fact.", AsyncFunction catFact, 15000, false)
            Command.createCommand ("joinchannel", [], "Join a channel and add it to the join list.", AsyncFunctionWithArgs joinChannel, 10000, true)
            Command.createCommand ("leavechannel", [], "Leave a channel and remove it from the join list.", AsyncFunctionWithArgs leaveChannel, 10000, true)
            Command.createCommand ("userid", [ "uid" ], "Get a user's twitch id.", AsyncFunctionWithArgsAndContext userId, 10000, false)
            Command.createCommand ("randomclip", [ "rc" ], "Get a random clip from the current channel or specified channel.", AsyncFunctionWithArgsAndContext randomClip, 10000, false)
            Command.createCommand ("reddit", [], "Get a trending post from a subreddit.", AsyncFunctionWithArgs reddit, 15000, false)
            Command.createCommand ("faceit", [], "Get recent Win/Loss stats for players in FaceIt CS2.", AsyncFunctionWithArgs faceit, 20000, false)
            Command.createCommand ("texttransform", [ "tt" ], "Transform text input to a different form.", SyncFunctionWithArgs texttransform, 10000, false)
            Command.createCommand ("namecolor", [ "color" ], "Get the color of a user's name in chat.", AsyncFunctionWithArgsAndContext namecolor, 15000, false)
            Command.createCommand ("vod", [], "Get the most recent vod for a channel.", AsyncFunctionWithArgs vod, 15000, false)
            Command.createCommand ("stream", [], "Get stream information for a channel - Title, viewcount, current category, uptime.", AsyncFunctionWithArgs stream, 15000, false)
            Command.createCommand ("braille", [], "Convert an image to braille acsii art.", AsyncFunctionWithArgs braille, 15000, false)
            Command.createCommand ("alias", [ $"{Chatbot.Configuration.Bot.config.CommandPrefix}" ], "Run a custom command.", AsyncFunctionWithArgsAndContext alias, 5000, false)
            Command.createCommand ("pipe", [], "Pipe the result of a command to the next.", SyncFunctionWithArgs pipe, 10000, false)
            Command.createCommand ("randomline", [ "rl" ], "Get a random line from a user in the current channel.", AsyncFunctionWithArgsAndContext randomLine, 10000, false)
            Command.createCommand ("randomquote", [ "rq" ], "Get a random quote from a random user in the current channel.", AsyncFunctionWithArgsAndContext randomQuote, 10000, false)
        ]
        |> List.map toKeyValue
        |> List.collect id
        |> Map.ofList
