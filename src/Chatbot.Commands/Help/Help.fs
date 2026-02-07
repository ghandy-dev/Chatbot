namespace Commands

open CommandError
open Configuration

[<AutoOpen>]
module Help =

    let help context (commands: Map<string, Command>) =
        match context.Args with
        | [] -> Ok <| Message $"See %s{appConfig.HelpUrl} for a list of commands"
        | command :: _ ->
            match commands |> Map.tryFind command with
            | None -> Ok <| Message $"see %s{appConfig.HelpUrl} for a list of commands"
            | Some c ->
                let aliases = if c.Aliases.Length > 0 then c.Aliases |> strJoin ", " |> fun a -> $"({a})" else ""
                Ok <| Message $"""%s{c.Name} %s{aliases} | %s{c.Details.Description} %s{appConfig.HelpUrl}{c.Name}"""

module HelpInfo =

    let commandPrefix = appConfig.Bot.CommandPrefix
    let example = sprintf "%s %s" commandPrefix
    let exampleArgs = sprintf "%s %s %s" commandPrefix

    let AccountAge =
        {
            Name = "Account Age"
            Description = "Look up a user's account age and creation date."
            ExampleUsage = $"""
Look up your own account age
{example "accountage"}

Look up another user's account age
{exampleArgs "accountage" "<username>"}
{exampleArgs "accountage" "k4yfour"}
"""
        }

    let AstronomyPictureOfTheDay =
        {
            Name = "Astronomy Picture Of The Day"
            Description = "Get the Astronomy Picture of the Day from NASA."
            ExampleUsage = $"""
Get todays picture.
{example "apod"}


Get the picture for a given date. (date format: yyyy/mm/dd)
{exampleArgs "apod" "<date>"}
{exampleArgs "apod" "2024/07/19"}
"""
        }

    let AddBetween =
        {
            Name = "Add Between"
            Description = "Insert a word (e.g. an emote) between each word in the input text."
            ExampleUsage =
        $"""
{exampleArgs "addbetween" "addbetween <word> <text>"}
{exampleArgs "addbetween" "addbetween forsenE LET'S GO FORSEN"}
"""
        }

    let Alias =
        {
            Name = "Alias"
            Description = "Runs a command using a custom alias."
            ExampleUsage = $"""
Add a new alias
{exampleArgs "alias" "add <alias name> <commands>"}
{exampleArgs "alias" "add forsenclip randomclip forsen"}

Remove an alias
{exampleArgs "alias" "remove <alias name>"}
{exampleArgs "alias" "delete <alias name>"}
{exampleArgs "alias" "remove forsenclip"}

Update an alias
{exampleArgs "alias" "edit <alias name> <command>"}
{exampleArgs "alias" "update <alias name> <command>"}
{exampleArgs "alias" "edit forsenclip randomclip forsen"}

Get an alias definition
{exampleArgs "alias" "definition <alias name>"}
{exampleArgs "alias" "check <alias name>"}
{exampleArgs "alias" "spy <alias name>"}
{exampleArgs "alias" "definition forsenclip"}

Copy an alias
{exampleArgs "alias" "copy <username> <alias name>"}
{exampleArgs "alias" "copy forsen randomForsenClip"}

Copy and overwrite an existing alias
{exampleArgs "alias" "copyplace <username> <alias name>"}
{exampleArgs "alias" "copyplace forsen randomForsenClip"}

Run an alias
{exampleArgs "run" "<alias name>"}
{exampleArgs "alias" "run <alias name>"}
"""
        }

    let Braille =
        {
            Name = "Braille"
            Description = "Generate ASCII braille art of an image."
            ExampleUsage = $"""
Specify an image URL
{exampleArgs "braille" "<url>"}
{exampleArgs "braille" "https://static-cdn.jtvnw.net/emoticons/v2/25/default/dark/3.0"}

Specify an emote.
Can be a Twitch/BTTV/FFZ/7TV emote, but due to limitations; if a Twitch Emote is specified it must be one accessible to the bot (i.e. either it must be a global emote, or an emote from a channel the bot is subscribed to)
{exampleArgs "braille" "<emote>"}
{exampleArgs "braille" "Kappa"}

Optional arguments:

greyscale:
    - lightness (default)
    - luminance
    - average
    - max

dithering (default no dithering):
    - floydsteinberg / fs
    - bayer

invert:
    - false
    - true (default)

monospace:
    - false (default)
    - true

{exampleArgs "braille" "<optional args> ... <url>"}
{exampleArgs "braille" "greyscale:average dithering:fs invert:false monospace:true https://static-cdn.jtvnw.net/emoticons/v2/25/default/dark/3.0"}

{exampleArgs "braille" "<optional args> ... <emote>"}
{exampleArgs "braille" "greyscale:average dithering:fs invert:false monospace:true Kappa"}
"""
        }

    let Calculator =
        {
            Name = "Calculator"
            Description = "Calculate a mathematical expression."
            ExampleUsage = $"""
Supported operators:
    - Addition +
    - Subtraction -
    - Multiplication *
    - Division /
    - Modulo %%
    - Power ^
    - Negative (prefix) -
    - Square root sqrt
    - Log log
    - Exponent exp

{exampleArgs "calculate" "2 + 2"}
{exampleArgs "calculate" "(1 / 2) ^ 4"}
{exampleArgs "calculate" "sqrt 25"}
"""
        }

    let CatFact =
        {
            Name = "Cat Fact"
            Description = "Gets a random cat fact."
            ExampleUsage = $"""{example "catfact"}"""
        }

    let Chance =
        {
            Name = "Chance"
            Description = "Returns a percentage from 0-100%."
            ExampleUsage = $"""{example "percentage"}"""
        }

    let ChatSummary =
        {
            Name = "Chat Summary"
            Description = "Get a summary of the last 100 lines, or the 100 lines or fewer in the last 2 hours."
            ExampleUsage = $"""
Note:
If used multiple times before 10 minutes has passed, a cached summary is returned until it expires.
Only works in channels which have logs available.

Get a summary of the current channel's chat.
{example "chatsummary"}

Get a summary of another channel's chats.
{exampleArgs "chatsummary" "forsen"}
"""
        }

    let Channel =
        {
            Name = "Channel"
            Description = "Gets a broadcasters channel information."
            ExampleUsage =
        $"""
{exampleArgs "channel" "<channel>"}
{exampleArgs "channel" "forsen"}
"""
        }

    let CoinFlip =
        {
            Name = "Coin Flip"
            Description = "Flips a coin (50:50 odds)"
            ExampleUsage = $"""{example "coinflip"}"""
        }


    let DidYouKnow =
        {
            Name = "Did You Know"
            Description = """Get interesting facts as seen on Wikipedia's "Did you know" section"""
            ExampleUsage = $"""{example "didyouknow"}"""
        }


    let Dungeon =
        {
            Name = "Dungeon"
            Description = "Enter the Dungeon and fight for gold."
            ExampleUsage =
        $"""
Register to be able enter the dungeon:
{exampleArgs "dungeon" "register"}

Get player lifetime stats (kills, deaths, gold gained/lost):
{exampleArgs "dungeon" "stats"}

Check player status (HP, AP, AD, DEF, Gold):
{exampleArgs "dungeon" "status"}

View items available to purchase in the shop:
{exampleArgs "dungeon" "shop"}

Buy an item (using the item ID shown in the shop):
{exampleArgs "dungeon" "buy <id>"}
{exampleArgs "dungeon" "buy 1"}

Fight an opponent to earn gold:
{exampleArgs "dungeon" "fight"}
"""
        }

    let Eightball =
        {
            Name = "Eightball"
            Description = "Ask a question and predict the future."
            ExampleUsage = $"""{example "eightball"}"""
        }

    let Echo =
        {
                Name = "Echo"
                Description = "Echo input back to the user."
                ExampleUsage = $"""
{exampleArgs "echo" "<input>"}
{exampleArgs "echo" "Hello World!"}
"""
        }

    let Encode =
        {
            Name = "Encode"
            Description = "Encode text transforming it to a different a different format."
            ExampleUsage = $"""
{exampleArgs "encode" "<encoder> <input>"}

Ciphers/Encoders:
- caesar
- rot13
- base64

Encode text using the caesar cipher. Optionally specify a shift value.
{exampleArgs "encode" "ceaser Kappa 123"}
Encode text using the caesar cipher, and shift/rotate the letters by 5 places.
{exampleArgs "encode" "ceaser 5 This is a cool secret message B)"}

Encode text using the caeser cipher with a 13 letter shift, re-applying this again to the encoded text decodes it instead.
{exampleArgs "encode" "rot13 Hello World!"}

Encode text to base64.
{exampleArgs "encode" "base64 forsen"}
"""
        }

    let FaceIt =
        {
            Name = "FaceIt"
            Description = "Get recent match stats, or recent history of wins and losses for games played on FaceIt for CS2."
            ExampleUsage = $"""
Get last game stats (can't get live game info):
{exampleArgs "faceit" "<player name>"}
{exampleArgs "faceit" "FrozenBag"}

Get stats for a player:
{exampleArgs "faceit" "stats <player name>"}
{exampleArgs "faceit" "stats FrozenBag"}

Get a players recent win/loss history:
{exampleArgs "faceit" "history <player name>"}
{exampleArgs "faceit" "history FrozenBag"}
"""
        }

    let Fill =
        {
            Name = "Fill"
            Description = "Fill a message to max length with the provided word(s)."
            ExampleUsage = $"""
Optional arguments:

repeat:
    - whether to repeat the words in order or randomise the words repeated
    - true (default) / false

{exampleArgs "fill" "<words>"}
{exampleArgs "fill" "Kappa"}

Randomise the words:
{exampleArgs "fill" "repeat:false <words>"}
{exampleArgs "fill" "Kappa Keepo PogChamp"}
"""
        }

    let FollowAge =
        {
            Name = "Follow Age"
            Description = "Get how long a twitch user has been following a twitch channel for."
            ExampleUsage = $"""
Get how long you've followed the current channel for.
{example "followage"}

Get how long a user has been following the current channel for.
{exampleArgs "followage" "<channel>"}
{exampleArgs "followage" "forsen"}

Get how long a user has been following a channel for.
{exampleArgs "followage" "<user> <channel>"}
{exampleArgs "followage" "nymn forsen"}
"""
        }

    let Gpt =
        {
            Name = "Gpt"
            Description = "Chat with OpenAI's ChatGPT."
            ExampleUsage = $"""
Conversations are based on the context of the channel, and Gpt maintains a history of messages sent by chatters for up to 10 minutes from the last message sent.
i.e. 10 mins after sending no messages through the gpt command will wipe your chatting history with it

{exampleArgs "gpt" "<prompt>"}
{exampleArgs "gpt" "What are the some of the rarest deep sea creatures?"}
"""
        }

    let GptImage =
        {
            Name = "Gpt Image"
            Description = "Generate an image by providing a prompt of what you want."
            ExampleUsage = $"""
{exampleArgs "gptimage" "<prompt>"}
{exampleArgs "gptimage" "will smith eating spaghetti"}
"""
        }

    let Help =
        {
            Name = "Help"
            Description = "Get help info about commands."
            ExampleUsage = $"""{example "help"}"""
        }

    let JoinChannel =
        {
            Name = "Join Channel"
            Description = "Join a channel and add it to the join list."
            ExampleUsage = $"""
{exampleArgs "joinchannel" "<channel name>"}
{exampleArgs "joinchannel" "forsen"}
"""
        }

    let LastLine =
        {
            Name = "Last Line"
            Description = "Get the last message a user sent in the current channel."
            ExampleUsage = $"""{example "lastline"}"""
        }

    let LeaveChannel =
        {
            Name = "Leave Channel"
            Description = "Leave a channel and remove it from the join list."
            ExampleUsage = $"""
{exampleArgs "leavechannel" "<channel name>"}
{exampleArgs "leavechannel" "forsen"}
"""
        }

    let LeagueOfLegends =
        {
            Name = "League of Legends"
            Description = "Look up a player's league of legends rank and stats."
            ExampleUsage = $"""
Available regions:
EUW (Europe West)
EUNE (Europe Nordic + East)
KR (Korea)
JP (Japan)
BR (Brazil)
LA (Latin America)
OCE (Oceania)
TR (Turkey)
RU (Russia)

{exampleArgs "leagueoflegends" "<region> <username>#<tag>"}
{exampleArgs "leagueoflegends" "euw forsenxd#EUW"}
{exampleArgs "lol" "kr hide on bush#KR1"}
"""
        }

    let NameColor =
        {
            Name = "Name Color"
            Description = "Get a user's username chat color."
            ExampleUsage = $"""
{exampleArgs "namecolor" "<username>"}
{exampleArgs "namecolor" "forsen"}
"""
        }

    let News =
        {
            Name = "News"
            Description = "Get top/trending news."
            ExampleUsage = $"""
Valid categories are:
Top Stories
World
US
UK
Business
Politics
Health
Education
Science
Technology
Entertainment
Sports

Get world news:
{example "news"}

Get news based on category
{exampleArgs "news" "<category>"}
{exampleArgs "news" "science"}
"""
        }

    let OnThisDay =
        {
            Name = "On This Day"
            Description = "Get events in history that occurred on this day."
            ExampleUsage = $"""{example "onthisday"}"""
        }

    let Pick =
        {
            Name = "Pick"
            Description = "Pick a single random item from a given list of items."
            ExampleUsage = $"""
{exampleArgs "pick" "<items>"}
{exampleArgs "pick" "1 2 3 4 5 6 7"}
{exampleArgs "pick" "Red Green Yellow Blue Pink"}

Custom delimiter:
{exampleArgs "pick" "delimiter:<delimiter> <input sequence>"}
{exampleArgs "pick" "delimiter:, Elden Ring, Dark Souls 1, Dark Souls 2, Dark Souls 3, Sekiro"}
"""
        }

    let Pipe =
        {
            Name = "Pipe"
            Description = "Pipe together 2 or more commands, taking the result from the previous command, and sending it to the next."
            ExampleUsage = $"""
Commands must be delimited by a "{pipeSeperator}" character

{exampleArgs "pipe" "<command> | <command> | ..."}
{exampleArgs "pipe" "pick one two three | texttransform uppercase"}
"""
        }

    let Ping =
        {
            Name = "Ping"
            Description = "Ping to check bot is up and running."
            ExampleUsage = $"""{example "ping"}"""
        }

    let RandomClip =
        {
            Name = "Random Clip"
            Description = "Get a random clip from the current channel, or a specified channel."
            ExampleUsage = $"""
Get a random clip from the current channel:
{example "randomclip"}

Get a random clip from a channel:
{exampleArgs "randomclip" "<channel>"}
{exampleArgs "randomclip" "forsen"}

Get a random clip using in a given period:

Valid periods are:
    - day
    - week (default)
    - month
    - year
    - all

{exampleArgs "randomclip" "<channel> period:<period>"}
{exampleArgs "randomclip" "lirik period:year"}
"""
        }

    let RandomEmote =
        {
            Name = "Random Emote"
            Description = "Get a random avaiable emote (globally and/or based on the current channel)"
            ExampleUsage = $"""
Get a random emote (global or channel emote from Twitch / BTTV / FFZ / 7TV)
{example "randomemote"}

Get a random emote from an emote provider.
Valid providers:
    - twitch
    - bttv
    - ffz
    - 7tv

{exampleArgs "randomemote" "provider:<emote provider>"}
{exampleArgs "randomemote" "provider:bttv"}
"""
        }

    let RandomLine =
        {
            Name = "Random Line"
            Description = "Gets a random line from anyone or a specified user from within the current channel."
            ExampleUsage = $"""
Only channels with logs available can be used.

{example "randomline"}
{exampleArgs "randomline" "<username>"}
{exampleArgs "randomline" "forsen"}
"""
        }

    let RandomQuote =
        {
            Name = "Random Quote"
            Description = "Gets a random quote from yourself from the current channel."
            ExampleUsage = $"""
Only channels with logs available can be used.

{example "randomquote"}
"""
        }

    let Reddit =
        {
            Name = "Reddit"
            Description = "Gets a reddit post that is currently trending (images / videos only)"
            ExampleUsage = $"""
Get a trending reddit post from a subreddit.
{exampleArgs "reddit" "<subreddit>"}
{exampleArgs "reddit" "linuxmemes"}

Get a trending reddit post based sorted by; "top" / "hot" / "best"
(top gets this weeks top posts)
{exampleArgs "reddit" "sort:<sorting> <subreddit>"}
{exampleArgs "reddit" "sort:top shitposting"}
"""
        }

    let RefreshChannelEmotes =
        {
            Name = "Refresh Channel Emotes"
            Description = "Refresh the emotes cached for a channel"
            ExampleUsage = $"""
Refresh global emotes.
{exampleArgs "refreshchannelemotes" "<channel>"}
{exampleArgs "rce" "<channel>"}
{exampleArgs "refreshchannelemotes" "forsen"}
{exampleArgs "rce" "clintstevens"}
"""
        }

    let RefreshGlobalEmotes =
        {
            Name = "Refresh Global Emotes"
            Description = "Refresh the cached global emotes"
            ExampleUsage = $"""
Refresh global emotes.
{example "refreshglobalemotes"}
{example "rge"}
"""
        }

    let Remind =
        {
            Name = "Remind"
            Description = "Remind a user when they next type in chat, or set a timed reminder for a user (or yourself)"
            ExampleUsage = $"""
Remind a user when they next type in chat:
{exampleArgs "remind" "<user> <message>"}
{exampleArgs "reddit" "forsen play assassin's creed black flag"}

Set a timed reminder to remind the user after a set duration in the current channel (can't be set in whispers):
{exampleArgs "remind" "<user> in <time period> <message>"}
{exampleArgs "remind" "forsen in 20 mins go live forsenSWA"}
{exampleArgs "remind" "ClintStevens next month OOT speedrun?"}

Set a timed reminder for yourself:
{exampleArgs "remind" "me in 10 minutes pasta" }
{exampleArgs "remind" "me tomorrow update stream title" }
"""
        }

    let RockPaperScissors =
        {
            Name = "Rock, Paper, Scissors"
            Description = "Play Rock Paper Scissors against the bot. Earn points, and keep track of stats."
            ExampleUsage = $"""
{exampleArgs "rps" "<shape>"}
{exampleArgs "rps" "rock"}
"""
        }

    let Roll =
        {
            Name = "Roll"
            Description = "Roll a random number."
            ExampleUsage = $"""
Roll a random number between 0 and 10.
{example "roll"}

Roll a random number between a min and max value.
{exampleArgs "roll" "<min> <max>"}
{exampleArgs "roll" "0 100"}
"""
        }

    let Search =
        {
            Name = "Search"
            Description = "Search logs for chat messages matching/containing the query text"
            ExampleUsage = $"""
Search logs for a single matching line containing the query text.
{exampleArgs "search" "<query>"}
{exampleArgs "search" "TriHard"}

Optional arguments:

user
    - specify a user to search logs for
    - default: user executing command

channel
    - specify the channel to search logs in
    - default: current channel

reverse
    - specify the sorting of messages
    - default: false
    - values:
        - false
            - sorts by oldest message
        - true
            - sorts by most recent message

offset
    - default: 0
    - values:
        - positive number

Search for the 2nd oldest message from nymn in forsen's channel that contains "AYAYA"
{exampleArgs "search" "user:nymn channel:forsen offset:1 AYAYA"}

Search for the most recent chat message from nymn in forsen's channel that contains "AYAYA"
{exampleArgs "search" "user:nymn channel:forsen reverse:true AYAYA"}
"""
        }

    let Slots =
        {
            Name = "Slots"
            Description = "Spin slots and try get a line of matching symbols/words/emotes"
            ExampleUsage = $"""
Provide a set of words/emotes to use and spin slots to try win.

Optional arguments:

set:
    - fruit
    - twitch (global emotes)
    - bttv (channel emotes)
    - ffz (channel emotes)
    - 7tv (channel emotes)
    - numbers (1-100)

Play slots with a custom input set:
{exampleArgs "slots" "Feels Dank Man"}
{exampleArgs "slots" "Kappa PogChamp Jebaited"}

Play slots with a specified set:
{exampleArgs "slots" "set:<set name>"}

{exampleArgs "slots" "set:twitch"}
{exampleArgs "slots" "set:bttv"}
"""
        }

    let Stream =
        {
            Name = "Stream"
            Description = "Gets stream info for a stream that is currently live."
            ExampleUsage = $"""
{exampleArgs "stream" "<channel>"}
{exampleArgs "stream" "forsen"}
"""
        }

    let SubAge =
        {
            Name = "Sub Age"
            Description = "Get how many months a twitch user has been subscribed to a twitch channel for."
            ExampleUsage = $"""

Get how many months you've been subscribed to the current channel.
{example "subage"}

Get how many months a user has been subscribed to the current channel for.
{exampleArgs "subage" "<channel>"}
{exampleArgs "subage" "forsen"}

Get how many months a user has been subscribed to a channel for.
{exampleArgs "subage" "<user> <channel>"}
{exampleArgs "subage" "nymn forsen"}
"""
        }

    let Time =
        {
            Name = "Time"
            Description = "Get the current time (UTC), or the local time from a location"
            ExampleUsage = $"""
Get current time (UTC).
{example "time"}

Get current time of location.
{exampleArgs "time" "<location>"}
{exampleArgs "time" "Paraguay"}
{exampleArgs "time" "California, US"}
"""
        }

    let TextToAscii =
        {
            Name = "Text to Ascii"
            Description = "Transform text to ascii text."
            ExampleUsage = $"""
{exampleArgs "texttoascii" "<text>"}

{exampleArgs "texttoascii" "KAPPA"}

Optional arguments:

greyscale:
    - lightness (default)
    - luminance
    - average
    - max

dithering:
    - false (default)
    - true

invert:
    - false (default)
    - true

monospace:
    - false
    - true (default)

{exampleArgs "texttoascii" "<optional arg> ... <text>"}
{exampleArgs "texttoascii" "greyscale:average dithering:true invert:false monospace:true JEBAITED"}
"""
        }

    let TextTransform =
        {
            Name = "Text Transform"
            Description = "Transforms text to a different format."
            ExampleUsage = $"""
Supported transforms are:
    - uppercase
    - lowercase
    - reverse
    - shuffle
    - explode
    - alternate/alternating

{exampleArgs "texttransform" "<transform> <text>"}
{exampleArgs "texttransform" "uppercase hello world!"}
"""
        }

    let TopStreams =
        {
            Name = "Top Streams"
            Description = "Gets top 10 streams (by viewer count) on Twitch."
            ExampleUsage = $"""{example "topstreams"}"""
        }

    let Trivia =
        {
            Name = "Trivia"
            Description = "Start a trivia."
            ExampleUsage = $"""
Start trivia (1 question by default).
{example "trivia"}

Stop an on-going trivia.
{example "trivia stop"}

Optional arguments:

count:
    - 1-10 (default 1)

include:
    - comma separated list of categories to limit trivia questions to
    - see https://gazatu.xyz/trivia/categories for a list of categories

exclude:
    - comma separated list of categories to exclude from trivia questions
    - see https://gazatu.xyz/trivia/categories for a list of categories

useHints:
    - true (default)
    - false

Start trivia with 5 questions.
{exampleArgs "trivia" "count:5"}

Start trivia with the specified categories.
{exampleArgs "trivia" "include:Twitch,WorldOfWarcraft,Music"}

Start trivia but exclude the specified categories from possibly appearing.
{exampleArgs "trivia" "exclude:Anime"}

Start trivia with hints disabled.
{exampleArgs "trivia" "useHints:false"}
"""
        }

    let UrbanDictionary =
        {
            Name = "Urban Dictionary"
            Description = "Get the definition of a word from Urban Dictionary."
            ExampleUsage = $"""
Get the definition for a random word.
{example "urban"}

Search for a specific term.
{exampleArgs "urban" "<term>"}
{exampleArgs "urban" "forsen"}
"""
        }

    let UserId =
        {
            Name = "User Id"
            Description = "Get a user's Twitch id."
            ExampleUsage = $"""
Doesn't return anything if the user is currently banned.

{exampleArgs "userid" "<username>"}
{exampleArgs "userid" "forsen"}
"""
        }

    let Vod =
        {
            Name = "Vod"
            Description = "Gets information about the most recent video-on-demand (VOD) of a channel."
            ExampleUsage = $"""
Doesn't return anything if the user is currently banned.

{exampleArgs "vod" "<channel>"}
{exampleArgs "vod" "forsen"} """
        }

    let WhatEmoteIsIt =
        {
            Name = "What Emote is it"
            Description = "Look up a twitch emote by emote name."
            ExampleUsage = $"""
{exampleArgs "whatemoteisit" "<emote>"}
{exampleArgs "whatemoteisit" "Kappa"}
{exampleArgs "whatemoteisit" "elisBall"}
"""
        }

    let Weather =
        {
            Name = "Weather"
            Description = "Look up the latest weather for any location around the world."
            ExampleUsage = $"""
{exampleArgs "weather" "<location>"}
{exampleArgs "weather" "Sweden"}
{exampleArgs "weather" "Madrid, Spain"}
"""
        }

    let Wiki =
        {
            Name = "Wikipedia"
            Description = "Gets the top result wikipedia page for the specified query, along with a short summary of the page."
            ExampleUsage = $"""{exampleArgs "wiki" "<query>"}"""
        }

    let WikiNews =
        {
            Name = "Wikipedia News"
            Description = "Get news stories from Wikipedia's news feed."
            ExampleUsage = $"""{example "wikinews"}"""
        }

    let xd =
        {
            Name = "xd"
            Description = "xd"
            ExampleUsage = $"""{example "xd"}"""
        }
