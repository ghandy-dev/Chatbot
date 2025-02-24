module Configuration

open Microsoft.Extensions.Configuration

DotEnv.load () |> Async.RunSynchronously

type Env =
    | Dev
    | Prod

    static member fromString s =
        match s with
        | "dev" | "Dev" -> Dev
        | _ -> Prod

let configuration =
    ConfigurationBuilder()
        .AddEnvironmentVariables()
        .AddJsonFile("appsettings.json", false, true)
        .AddEnvironmentVariables()
        .Build()

let private getSection<'T> section = configuration.GetSection(section).Get<'T>()
let private getItem key = configuration.GetValue<string>(key)

[<CLIMutable>]
type LoggingConfig = {
    LogLevel: LogLevel
}

and LogLevel = {
    Default: string
}

[<CLIMutable>]
type ConnectionStrings = {
    Database: string
    Twitch: string
}

[<CLIMutable>]
type TwitchConfig = {
    ClientId: string
    ClientSecret: string
    RefreshToken: string
}

[<CLIMutable>]
type BotConfig = {
    CommandPrefix: string
    Capabilities: string array
}

[<CLIMutable>]
type RedditConfig = {
    ClientId: string
    ClientSecret: string
}

[<CLIMutable>]
type FaceItConfig = { ApiKey: string }

[<CLIMutable>]
type OpenAiConfig = {
    ApiKey: string
    DefaultModel: string
}

[<CLIMutable>]
type NasaConfig = {
    ApiKey: string
}

[<CLIMutable>]
type GoogleConfig = {
    Geocoding: Geocoding
    Timezone: Timezone
}

and Geocoding = {
    ApiKey: string
}

and Timezone = {
    ApiKey: string
}


[<CLIMutable>]
type MicrosoftConfig = {
    Maps: Maps
}

and Maps = {
    ApiKey: string
}

[<CLIMutable>]
type PastebinConfig = {
    ApiKey: string
}

[<CLIMutable>]
type RiotGamesConfig = {
    ApiKey: string
}

[<CLIMutable>]
type AppConfig = {
    Env: string
    HelpUrl: string
}

type Config = {
    Logging: LoggingConfig
    ConnectionStrings: ConnectionStrings
    Twitch: TwitchConfig
    Bot: BotConfig
    Reddit: RedditConfig
    FaceIt: FaceItConfig
    OpenAI: OpenAiConfig
    Nasa: NasaConfig
    Google: GoogleConfig
    Microsoft: MicrosoftConfig
    Pastebin: PastebinConfig
    RiotGames: RiotGamesConfig
    Env: Env
    HelpUrl: string
    UserAgent: string
}

let private parseEnv =
    function
    | e when System.String.IsNullOrWhiteSpace e -> Prod
    | e -> Env.fromString e

let loadConfig () : Config =
    {
        Logging = getSection<LoggingConfig> "Logging"
        ConnectionStrings = getSection<ConnectionStrings> "ConnectionStrings"
        Twitch = getSection<TwitchConfig> "Twitch"
        Bot = getSection<BotConfig> "Bot"
        Reddit = getSection<RedditConfig> "Reddit"
        FaceIt = getSection<FaceItConfig> "FaceIt"
        OpenAI = getSection<OpenAiConfig> "OpenAI"
        Nasa = getSection<NasaConfig> "Nasa"
        Google = getSection<GoogleConfig> "Google"
        Microsoft = getSection<MicrosoftConfig> "Microsoft"
        Pastebin = getSection<PastebinConfig> "Pastebin"
        RiotGames = getSection<RiotGamesConfig> "RiotGames"
        Env = getItem "Env" |> parseEnv
        HelpUrl = getItem "HelpUrl"
        UserAgent = getItem "UserAgent"
    }

let appConfig = loadConfig ()