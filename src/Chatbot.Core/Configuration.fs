namespace Chatbot

module Configuration =

    open Microsoft.Extensions.Configuration

    DotEnv.load () |> Async.RunSynchronously

    let configuration =
        ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddJsonFile("appsettings.json", false, true)
            .AddEnvironmentVariables()
            .Build()

    module Logging =

        type LogLevel = { Default: string }

        [<CLIMutable>]
        type LoggingConfig = { LogLevel: LogLevel }

        let config = configuration.GetSection("Logging").Get<LoggingConfig>()


    module ConnectionStrings =

        [<CLIMutable>]
        type ConnectionStrings = {
            Database: string
            Irc: string
        }

        let config = configuration.GetSection("ConnectionStrings").Get<ConnectionStrings>()


    module Twitch =

        [<CLIMutable>]
        type TwitchConfig = {
            ClientId: string
            ClientSecret: string
            RefreshToken: string
        }

        let config = configuration.GetSection("Twitch").Get<TwitchConfig>()

    module Bot =

        [<CLIMutable>]
        type BotConfig = {
            CommandPrefix: string
            Capabilities: string array
        }

        let config = configuration.GetSection("BotConfig").Get<BotConfig>()

    module Reddit =

        [<CLIMutable>]
        type RedditConfig = {
            ClientId: string
            ClientSecret: string
        }

        let config = configuration.GetSection("Reddit").Get<RedditConfig>()

    module FaceIt =

        [<CLIMutable>]
        type FaceItConfig = { ApiKey: string }

        let config: FaceItConfig = configuration.GetSection("FaceIt").Get<FaceItConfig>()

    module OpenAI =

        [<CLIMutable>]
        type OpenAiConfig = {
            ApiKey: string
            DefaultModel: string
        }

        let config = configuration.GetSection("OpenAI").Get<OpenAiConfig>()

    module Nasa =

        [<CLIMutable>]
        type NasaConfig = {
            ApiKey: string
        }

        let config = configuration.GetSection("Nasa").Get<NasaConfig>()
