module Chatbot.CompositionRoot

open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging

open Configuration
open Chatbot.Commands
open Chatbot.Core
open Chatbot.Core.Caching
open Chatbot.Core.Domain.Commands
open Chatbot.Core.Services
open Chatbot.Types

DotEnv.load ()

let configuration =
    ConfigurationBuilder()
        .AddJsonFile("appsettings.json", false, true)
        .AddEnvironmentVariables()
        .Build()

let loadConfigs () : Configs =
    {
        Logging = configuration |> getSection<LoggingConfig> "Logging"
        ConnectionStrings = configuration |> getSection<ConnectionStrings> "ConnectionStrings"
        Reddit = configuration |> getSection<RedditConfig> "Reddit"
        FaceIt = configuration |> getSection<FaceItConfig> "FaceIt"
        OpenAI = configuration |> getSection<OpenAIConfig> "OpenAI"
        Nasa = configuration |> getSection<NasaConfig> "Nasa"
        Google = configuration |> getSection<GoogleConfig> "Google"
        Microsoft = configuration |> getSection<MicrosoftConfig> "Microsoft"
        Pastebin = configuration |> getSection<PastebinConfig> "Pastebin"
        RiotGames = configuration |> getSection<RiotGamesConfig> "RiotGames"
        TwitchChatConfig = configuration |> getSection<TwitchChatConfig> "TwitchChat"
        TwitchApi = configuration |> getSection<TwitchApiConfig> "TwitchApi"
        CommandPrefix = configuration |> getItem "CommandPrefix"
        PipePrefix = configuration |> getItem "PipePrefix"
        AliasPrefix = configuration |> getItem "AliasPrefix"
        PipeSeparator = configuration |> getItem "PipeSeparator"
        UserAgent = configuration |> getItem "UserAgent"
        UserId = configuration |> getItem "UserId"
        HelpUrl = configuration |> getItem "HelpUrl"
    }

let loggerFactory = LoggerFactory.Create(fun options ->
    options.AddConfiguration(configuration) |> ignore
    options.AddSimpleConsole(fun options ->
        options.ColorBehavior <- Microsoft.Extensions.Logging.Console.LoggerColorBehavior.Enabled
        options.SingleLine <- true
        options.UseUtcTimestamp <- true
        options.TimestampFormat <- "[HH:mm:ss] "
    ) |> ignore
)

let configs = loadConfigs ()

let memoryCache = MemoryCache.empty ()
let db = Chatbot.Database.Db.create configs.ConnectionStrings.Database
let logger = loggerFactory.CreateLogger("Default")
let httpHandler = new Http.LoggingHandler(loggerFactory.CreateLogger<Http.LoggingHandler>())
let httpClient = Http.create httpHandler configs.UserAgent

let env: Types.Env = {
    Cache = memoryCache
    Database = db
    HttpClient = httpClient
    Logger = logger
}

let twitchService = Twitch.TwitchService.create env { ClientId = configs.TwitchApi.ClientId ; ClientSecret = configs.TwitchApi.ClientSecret ; RefreshToken = configs.TwitchApi.RefreshToken }

let emoteService =
    Emotes.EmoteService.create [
        Emotes.TwitchEmoteService.create twitchService
        Emotes.BttvEmoteService.create env
        Emotes.FfzEmoteService.create env
        Emotes.SevenTvService.create env
    ]

let catFactService = CatFact.CatFactService.create env
let faceItService = FaceIt.FaceItService.create env { ApiKey = configs.FaceIt.ApiKey }
let geolocationService = Geolocation.GeolocationService.create env { MapsApiKey = configs.Microsoft.Maps.ApiKey } { GeocodingApiKey = configs.Google.Geocoding.ApiKey ; TimezoneApiKey = configs.Google.Timezone.ApiKey}
let imageUploadService = ImageUpload.ImageUploadService.create env
let ivrService = Ivr.IvrService.create env
let nasaService = Nasa.NasaService.create env { ApiKey = configs.Nasa.ApiKey }
let newsService = News.NewsService.create env
let genAIService = OpenAI.OpenAIService.create env { ApiKey = configs.OpenAI.ApiKey ; DefaultChatModel = configs.OpenAI.DefaultChatModel ; DefaultImageModel = configs.OpenAI.DefaultImageModel}
let pastebinService = Pastebin.PastebinService.create env { ApiKey = configs.Pastebin.ApiKey }
let redditService = Reddit.RedditService.create env { ClientId = configs.Reddit.ClientId ; ClientSecret = configs.Reddit.ClientSecret}
let riotGamesService = RiotGames.RiotGamesService.create env { ApiKey = configs.RiotGames.ApiKey }
let triviaService = Trivia.TriviaService.create env
let urbanDictionaryService = UrbanDictionary.UrbanDictionaryService.create env
let weatherService = Weather.WeatherService.create env { MapsApiKey = configs.Microsoft.Maps.ApiKey }
let wikipediaService = Wikipedia.WikipediaService.create env

let buildCommands commandPrefix =
    let toKeyValuePair command =
        match command.Aliases with
        | [] -> [ command.Name, command ]
        | aliases -> command.Name :: aliases |> List.map (fun a -> a, command)

    [
        Command.create "accountage" [ "accage" ] HelpInfo.AccountAge (Async (accountAge twitchService)) 10 false true
        Command.create "addbetween" [ "ab" ] HelpInfo.AddBetween (Sync addBetween) 10 false true
        Command.create "alias" [] HelpInfo.Alias (Alias (alias db configs.PipeSeparator twitchService)) 10 false false
        Command.create "apod" [] HelpInfo.AstronomyPictureOfTheDay (Async (apod nasaService)) 20 false true
        Command.create "braille" [ "ascii" ] HelpInfo.Braille (Async braille) 20 false true
        Command.create "calculator" [ "calc" ; "math" ] HelpInfo.Calculator (Sync calculate) 5 false true
        Command.create "catfact" [] HelpInfo.CatFact (Async (catFact catFactService)) 20 false true
        Command.create "chance" [ "%" ] HelpInfo.Chance (Sync chance) 10 false true
        Command.create "chatsummary" [] HelpInfo.ChatSummary (Async (chatSummary ivrService genAIService)) 20 false true
        Command.create "channel" [] HelpInfo.Channel (Async (channel twitchService)) 20 false true
        Command.create "coinflip" [ "cf" ] HelpInfo.CoinFlip (Sync coinFlip) 10 false true
        Command.create "didyouknow" [ "dyk" ] HelpInfo.DidYouKnow (Async (didYouKnow wikipediaService)) 10 false true
        Command.create "eightball" ["8ball"] HelpInfo.Eightball (Sync eightball) 10 false true
        Command.create "echo" [] HelpInfo.Echo (Sync echo) 5 true true
        Command.create "encode" [] HelpInfo.Encode (Sync encode) 5 false true
        Command.create "faceit" [] HelpInfo.FaceIt (Async (faceit faceItService)) 20 false true
        Command.create "fill" [] HelpInfo.Fill (Sync fill) 10 false true
        Command.create "followage" [ "fa" ] HelpInfo.FollowAge (Async (followAge ivrService)) 20 false true
        Command.create "gpt" [] HelpInfo.Gpt (Async (gpt genAIService)) 15 false true
        // Command.create ("gptimage" [] HelpInfo.Gpt Async (gptImage genAIService imageUploadService) 15 false))
        Command.create "help" []  HelpInfo.Help (Help (help configs.HelpUrl)) 10 false false
        Command.create "joinchannel" [] HelpInfo.JoinChannel (Async (joinChannel db twitchService)) 5 true false
        Command.create "lastline" [ "ll" ] HelpInfo.LastLine (Async (lastLine ivrService)) 5 false true
        Command.create "leavechannel" [] HelpInfo.LeaveChannel (Async (leaveChannel db twitchService)) 5 true false
        Command.create "leagueoflegends" [ "lol" ; "league" ] HelpInfo.LeagueOfLegends (Async (league riotGamesService)) 15 false true
        Command.create "namecolor" [ "color" ] HelpInfo.NameColor (Async (namecolor twitchService)) 20 false true
        Command.create "news" [] HelpInfo.News (Async (news newsService)) 15 false true
        Command.create "onthisday" [ "otd" ] HelpInfo.Pick (Async (onThisDay wikipediaService)) 10 false true
        Command.create "pick" [] HelpInfo.Pick (Sync pick) 10 false true
        Command.create "ping" [] HelpInfo.Ping (Sync ping) 5 false true
        Command.create "profilepicture" [ "pfp" ] HelpInfo.ProfilePicture (Async (profilePicture twitchService)) 20 false true
        Command.create "randomclip" [ "rc" ] HelpInfo.RandomClip (Async (randomClip twitchService)) 20 false true
        Command.create "randomemote" [] HelpInfo.RandomEmote (Sync randomEmote) 5 false true
        Command.create "randomline" [ "rl" ] HelpInfo.RandomLine (Async (randomLine ivrService)) 10 false true
        Command.create "randomquote" [ "rq" ] HelpInfo.RandomQuote (Async (randomQuote ivrService)) 10 false true
        Command.create "reddit" [] HelpInfo.Reddit (Async (reddit redditService)) 15 false true
        Command.create "refreshchannelemotes" [ "rce" ] HelpInfo.RefreshChannelEmotes (Sync refreshChannelEmotes) 5 true false
        Command.create "refreshglobalemotes" [ "rge" ] HelpInfo.RefreshGlobalEmotes (Sync refreshGlobalEmotes) 5 true false
        Command.create "remind" [ "notify" ] HelpInfo.Remind (Async (remind db twitchService)) 5 false true
        Command.create "rockpaperscissors" [ "rps" ] HelpInfo.RockPaperScissors (Async (rps db)) 10 false true
        Command.create "roll" [] HelpInfo.Roll (Sync roll) 10 false true
        Command.create "search" [] HelpInfo.Search (Async (search ivrService)) 10 false true
        Command.create "slots" [] HelpInfo.Slots (Sync slots) 10 false true
        Command.create "stream" [] HelpInfo.Stream (Async (stream twitchService)) 20 false true
        Command.create "subage" [ "sa" ] HelpInfo.SubAge (Async (subAge ivrService)) 10 false true
        Command.create "time" [] HelpInfo.Time (Async (time geolocationService)) 5 false true
        Command.create "texttoascii" [ "tta" ] HelpInfo.TextToAscii (Sync textToAscii) 15 false true
        Command.create "texttransform" [ "tt" ] HelpInfo.TextTransform (Sync texttransform) 5 false true
        Command.create "thumbnail" [ "tn" ] HelpInfo.Thumbnail (Async (thumbnail twitchService)) 20 false true
        Command.create "topstreams" [ "ts" ] HelpInfo.TopStreams (Async (topStreams twitchService)) 20 false true
        Command.create "trivia" [] HelpInfo.Trivia (Async (trivia triviaService)) 20 false false
        Command.create "urban" [ "ud" ] HelpInfo.UrbanDictionary (Async (urban urbanDictionaryService)) 20 false true
        Command.create "userid" [ "uid" ] HelpInfo.UserId (Async (userId twitchService)) 20 false true
        Command.create "vod" [] HelpInfo.Vod (Async (vod twitchService)) 20 false true
        Command.create "whatemoteisit" [] HelpInfo.WhatEmoteIsIt (Async (whatemoteisit ivrService)) 10 false true
        Command.create "weather" [] HelpInfo.Weather (Async (weather geolocationService weatherService)) 20 false true
        Command.create "wiki" [] HelpInfo.Wiki (Async (wiki wikipediaService)) 20 false true
        Command.create "wikinews" [] HelpInfo.WikiNews (Async (wikiNews wikipediaService)) 10 false true
        Command.create "xd" [] HelpInfo.xd (Sync xd) 60 false true
    ]
    |> List.map toKeyValuePair
    |> List.collect id
    |> Map.ofList

let commands = buildCommands configs.CommandPrefix
let prefixConfig = PrefixConfig.create configs.CommandPrefix configs.PipePrefix configs.AliasPrefix
let botConfig = BotConfig.create commands prefixConfig