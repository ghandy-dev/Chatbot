namespace Chatbot

module Assembly =

    type Placeholder = Placeholder

module Configuration =

    open Microsoft.Extensions.Configuration

    let configuration =
        ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddJsonFile("appsettings.json", false, true)
            .AddUserSecrets(System.Reflection.Assembly.GetExecutingAssembly(), optional = true, reloadOnChange = false)
            .AddCommandLine(System.Environment.GetCommandLineArgs())
            .Build()

    let accessToken = configuration.GetValue<string>("AccessToken")

    module Twitch =

        [<CLIMutable>]
        type TwitchConfig = {
            ClientId: string
            ClientSecret: string
        }

        let config = configuration.GetSection("TwitchConfig").Get<TwitchConfig>()

    module Bot =

        [<CLIMutable>]
        type BotConfig = {
            CommandPrefix: string
            Botname: string
            Capabilities: string array
        }

        let config = configuration.GetSection("BotConfig").Get<BotConfig>()

    module Connection =

        [<CLIMutable>]
        type ConnectionConfig = {
            Host: string
            Port: int
        }

        let config = configuration.GetSection("Connection").Get<ConnectionConfig>()

    module Reddit =

        [<CLIMutable>]
        type RedditConfig = {
            ClientId: string
            ClientSecret: string
            UserAgent: string
        }

        let config = configuration.GetSection("RedditConfig").Get<RedditConfig>()

    module FaceIt =

        [<CLIMutable>]
        type FaceItConfig = { ApiKey: string }

        let config = configuration.GetSection("FaceItConfig").Get<FaceItConfig>()

    module DallE =

        [<CLIMutable>]
        type DallEConfig = { ApiKey: string }

        let config = configuration.GetSection("DallEConfig").Get<DallEConfig>()
