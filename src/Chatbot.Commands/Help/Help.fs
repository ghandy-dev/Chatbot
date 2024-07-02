namespace Chatbot.Commands

[<AutoOpen>]
module Help =

    let help () =
        Ok <| Message "See https://ghandy-dev.github.io/Chatbot/ for a list of commands"


module HelpInfo =

    open System

    let commandPrefix = Chatbot.Configuration.Bot.config.CommandPrefix
    let example = sprintf "%s %s" commandPrefix
    let exampleArgs = sprintf "%s %s %s" commandPrefix

    let Alias =
        $"""
Runs a command using a custom alias.

Examples:
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
{exampleArgs "alias" "run <alias name>"}"""

    let Braille =
        $"""
Generate ASCII braille art of an image.

Examples:

{exampleArgs "braille" "<url>"}
{exampleArgs "braille" "https://static-cdn.jtvnw.net/emoticons/v2/25/default/dark/3.0"}

With conversion
{exampleArgs "braille" "<setting> <url>"}
{exampleArgs "braille" "lightness https://static-cdn.jtvnw.net/emoticons/v2/25/default/dark/3.0"}
Supported conversions:
    - lightness (default)
    - luminance
    - average
    - max
"""

    let Calculator =
        $"""
Calculate a mathematical expression.

Examples:

{exampleArgs "calculate" "2 + 2"}
{exampleArgs "calculate" "(1 / 2) ^ 4"}
{exampleArgs "calculate" "sqrt 25"}

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
"""

    let CatFact =
        $"""
Gets a random cat fact

Examples
{example "catfact"}
"""

    let CoinFlip =
        $"""
Flips a coin (50:50 odds)

Heads or Tails

Examples:

{example "coinflip"}
"""

    let Eightball =
        $"""
Ask a question and predict the future.

Examples:

{example "eightball"}
"""

    let Echo =
        $"""
Echo input back to the user.

Examples:

{exampleArgs "echo" "<input>"}
{exampleArgs "echo" "Hello World!"}
"""

    let FaceIt =
        $"""
Get recent match stats, or recent history of wins and losses for games played on FaceIt for CS2.

Examples:

Get last game stats:
{exampleArgs "faceit" "<player name>"}
{exampleArgs "faceit" "FrozenBag"}

Get stats for a player:
{exampleArgs "faceit" "stats <player name>"}
{exampleArgs "faceit" "stats FrozenBag"}

Get a players recent win/loss history:
{exampleArgs "faceit" "history <player name>"}
{exampleArgs "faceit" "history FrozenBag"}
"""

    let JoinChannel =
        $"""
Join a channel and add it to the join list.

Examples:

{exampleArgs "joinchannel" "<channel name>"}
{exampleArgs "joinchannel" "forsen"}
"""

    let LeaveChannel =
        $"""
Leave a channel and remove it from the join list.

Examples:

{exampleArgs "leavechannel" "<channel name>"}
{exampleArgs "leavechannel" "forsen"}
"""

    let NameColor =
        $"""
Get a user's username chat color.

Examples:

{exampleArgs "namecolor" "<username>"}
{exampleArgs "namecolor" "forsen"}
"""

    let Pick =
        $"""
Pick a single random item from a given list of items.

Examples:

{exampleArgs "pick" "<items>"}
{exampleArgs "pick" "1 2 3 4 5 6 7"}
{exampleArgs "pick" "Red Green Yellow Blue Pink"}

Custom delimiter:
{exampleArgs "pick" "delimiter:<delimiter> <input sequence>"}
{exampleArgs "pick" "delimiter:, Elden Ring, Dark Souls 1, Dark Souls 2, Dark Souls 3, Sekiro"}
"""

    let Pipe =
        $"""
Pipe together 2 or more commands, taking the result from the previous command, and sending it to the next.
Commands must be delimited by a "|" character

Examples:

{exampleArgs "pipe" "<command> | <command> | ..."}
{exampleArgs "pipe" "pick one two three | texttransform uppercase"}
"""

    let Ping =
        $"""
Ping to check bot is up and running.
Responds with some metrics (TODO).

Examples:

{example "ping"}
"""

    let Percentage =
        $"""
Returns a percentage from 0-100%%.

Examples:

{example "percentage"}

"""

    let RandomClip =
        $"""
Get a random clip of the current channel, or a specified channel.

Examples:

{example "randomclip"}
{exampleArgs "randomclip" "<channel>"}
{exampleArgs "randomclip" "forsen"}
"""

    let RandomLine =
        $"""
Gets a random line from yourself or a specified user from within the current channel.
Only channels with logs available can be used.

Examples:

{example "randomline"}
{exampleArgs "randomline" "<username>"}
{exampleArgs "randomline" "forsen"}
"""

    let RandomQuote =
        $"""
Gets a random quote from a user in the current channel.
Only channels with logs available can be used.

Examples:
{example "randomquote"}
"""

    let Reddit =
        $"""
Gets a reddit post that is currently trending (images / videos only)
Only channels with logs available can be used.

Examples:

Get a trending reddit post from a subreddit.
{exampleArgs "reddit" "<subreddit>"}
{exampleArgs "reddit" "linuxmemes"}

Get a trending reddit post based sorted by; "top" / "hot" / "best"
{exampleArgs "reddit" "<sort> <subreddit>"}
{exampleArgs "reddit" "top shitposting"}
"""

    let RockPaperScissors =
        $"""
Play Rock Paper Scissors against a CPU player. Earn points, and keep track of stats.

Examples:

{exampleArgs "rps" "<shape>"}
{exampleArgs "rps" "rock"}
"""

    let Roll =
        $"""
Roll and random number.
Default 0-10.

Examples:

Roll a random number between 0 and 10.
{example "roll"}

Roll a random number between a min and max value.
{exampleArgs "roll" "<min> <max>"}
{exampleArgs "roll" "0 100"}
"""

    let Stream =
        $"""
Gets stream info for a stream that is currently live.
Title, viewcount, current category, uptime.

Examples:

{exampleArgs "stream" "<channel>"}
{exampleArgs "stream" "forsen"}
"""

    let Time =
        $"""
Get current time (UTC).

Examples:

{example "time"}
"""

    let TextTransform =
        $"""
Transforms text from input to a different format.

Supported transforms are:
    - uppercase
    - lowercase
    - reverse
    - shuffle
    - explode

Examples:

{exampleArgs "texttransform" "<transform> <text>"}
{exampleArgs "texttransform" "uppercase hello world!"}
"""

    let TopStreams =
        $"""
Gets top 10 streams (by viewer count) on Twitch.
Broadcaster - game (viewer count)

Examples:

{example "topstreams"}
"""

    let UserId =
        $"""
Gets a user's twitch id.

Doesn't return anything if the user is currently banned.

Examples:

{exampleArgs "userid" "<username>"}
{exampleArgs "userid" "forsen"}
"""

    let Vod =
        $"""
Gets information about the most recent video-on-demand (VOD) of a channel.

Doesn't return anything if the user is currently banned.

Examples:

{exampleArgs "vod" "<channel>"}
{exampleArgs "vod" "forsen"}
"""

    let xd =
        $"""
xd.

Examples:

{example "xd"}
"""
