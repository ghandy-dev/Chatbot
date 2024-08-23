namespace Chatbot.Commands

[<AutoOpen>]
module Help =

    let help () = Message "See https://ghandy-dev.github.io/Chatbot/ for a list of commands"


module HelpInfo =

    let commandPrefix = Chatbot.Configuration.Bot.config.CommandPrefix
    let example = sprintf "%s %s" commandPrefix
    let exampleArgs = sprintf "%s %s %s" commandPrefix


    let AstronomyPictureOfTheDay =
        {
            Name = "Astronomy Picture Of TheDay"
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
{exampleArgs "alias" "edit <alias name> <commands>"}
{exampleArgs "alias" "update <alias name> <commands>"}
{exampleArgs "alias" "edit forsenclip randomclip forsen"}

Get an alias definition
{exampleArgs "alias" "definition <alias name>"}
{exampleArgs "alias" "definition forsenclip"}

Run an alias
{exampleArgs ">" "<alias name>"}
{exampleArgs "alias" "run <alias name>"}
"""
        }

    let Braille =
        {
            Name = "Braille"
            Description = "Generate ASCII braille art of an image."
            ExampleUsage = $"""
{exampleArgs "braille" "<url>"}
{exampleArgs "braille" "https://static-cdn.jtvnw.net/emoticons/v2/25/default/dark/3.0"}

Supported conversions:
    - lightness (default)
    - luminance
    - average
    - max

{exampleArgs "braille" "<setting> <url>"}
{exampleArgs "braille" "lightness https://static-cdn.jtvnw.net/emoticons/v2/25/default/dark/3.0"}
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
            ExampleUsage =
        $"""{example "coinflip"}"""
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
{exampleArgs "encode" "ceaser Kappa 123"}

Encode using the caeser cipher with a 13 letter shift, re-applying this again to the encoded output decodes the text.
{exampleArgs "encode" "rot13 Hello World!"}

Encode input to base64.
{exampleArgs "encode" "base64 forsen"}
"""
        }

    let EvilGpt =
        {
            Name = "Evil Gpt"
            Description = "Chat with OpenAI's ChatGPT, who is a bit of a bully this time."
            ExampleUsage = $"""{exampleArgs "evilgpt" "<prompt>"}"""
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

    let Gpt =
        {
            Name = "Gpt"
            Description = "Chat with OpenAI's ChatGPT."
            ExampleUsage = $"""
Conversations are based on the context of the channel, and Gpt maintains a history of messages sent by chatters for up to 10 minutes from the last message sent.
i.e. 10 mins after sending no messages through the gpt command will wipe your chatting history with it

{exampleArgs "gpt" "<prompt>"}
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

    let LeaveChannel =
        {
            Name = "Leave Channel"
            Description = "Leave a channel and remove it from the join list."
            ExampleUsage = $"""
{exampleArgs "leavechannel" "<channel name>"}
{exampleArgs "leavechannel" "forsen"}
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
Commands must be delimited by a "|" character

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
{exampleArgs "reddit" "<sort> <subreddit>"}
{exampleArgs "reddit" "top shitposting"}
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

    let Stream =
        {
            Name = "Stream"
            Description = "Gets stream info for a stream that is currently live."
            ExampleUsage = $"""
{exampleArgs "stream" "<channel>"}
{exampleArgs "stream" "forsen"}
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

{exampleArgs "texttransform" "<transform> <text>"}
{exampleArgs "texttransform" "uppercase hello world!"}
"""
        }

    let TopStreams =
        {
            Name = "Top Streams"
            Description = "Gets top 10 streams (by viewer count) on Twitch."
            ExampleUsage = $"""{example "topstreams"}
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

    let xd =
        {
            Name = "xd"
            Description = "xd"
            ExampleUsage = $"""{example "xd"}"""
        }