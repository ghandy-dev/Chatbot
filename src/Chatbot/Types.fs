namespace Chatbot.Types

open Chatbot.Core.Domain.Commands

type PrefixConfig = {
    CommandPrefix: string
    PipePrefix: string
    AliasPrefix: string
}

type BotConfig = {
    Commands: Map<string, Command>
    Prefixes: PrefixConfig
}

module PrefixConfig =

    let create commandPrefix pipePrefix aliasPrefix = {
        CommandPrefix = commandPrefix
        PipePrefix = pipePrefix
        AliasPrefix = aliasPrefix
    }

module BotConfig =

    let create commands prefixes = {
        Commands = commands
        Prefixes = prefixes
    }