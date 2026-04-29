module Chatbot.Configuration

open Microsoft.Extensions.Configuration

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
    IrcServer: string
}

[<CLIMutable>]
type TwitchApiConfig = {
    ClientId: string
    ClientSecret: string
    RefreshToken: string
}

[<CLIMutable>]
type TwitchChatConfig = {
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
type OpenAIConfig = {
    ApiKey: string
    DefaultChatModel: string
    DefaultImageModel: string
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

type Config = {
    ConnectionStrings: ConnectionStrings
    FaceIt: FaceItConfig
    Logging: LoggingConfig
    Google: GoogleConfig
    Microsoft: MicrosoftConfig
    Nasa: NasaConfig
    OpenAI: OpenAIConfig
    Pastebin: PastebinConfig
    Reddit: RedditConfig
    RiotGames: RiotGamesConfig
    TwitchApi: TwitchApiConfig
    TwitchChatConfig: TwitchChatConfig
    CommandPrefix: string
    PipePrefix: string
    AliasPrefix: string
    PipeSeparator: string
    UserAgent: string
    UserId: string
    HelpUrl: string
}

let getSection<'T> section (configuration: IConfiguration) = configuration.GetSection(section).Get<'T>()
let getItem key (configuration: IConfiguration) = configuration.GetValue<string>(key)
