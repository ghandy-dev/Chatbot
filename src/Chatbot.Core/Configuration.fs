namespace Chatbot

// module Assembly =
//
//     type Placeholder = Placeholder

module Configuration =

    open Microsoft.Extensions.Configuration

    DotEnv.load () |> Async.RunSynchronously

    let configuration =
        ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddJsonFile("appsettings.json", false, true)
            .AddEnvironmentVariables()
            // .AddUserSecrets(System.Reflection.Assembly.GetExecutingAssembly(), optional = true, reloadOnChange = false)
            // .AddCommandLine(System.Environment.GetCommandLineArgs())
            .Build()

    let accessToken = configuration.GetValue<string>("AccessToken")

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
        }

        let config = configuration.GetSection("Twitch").Get<TwitchConfig>()

    module Bot =

        [<CLIMutable>]
        type BotConfig = {
            CommandPrefix: string
            Botname: string
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

        let config = configuration.GetSection("FaceIt").Get<FaceItConfig>()

    module DallE =

        [<CLIMutable>]
        type DallEConfig = { ApiKey: string }

        let config = configuration.GetSection("DallE").Get<DallEConfig>()
