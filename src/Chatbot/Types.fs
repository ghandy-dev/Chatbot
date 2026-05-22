namespace Chatbot.Types

open Chatbot.Core.Domain.Commands

type PrefixConfig = {
    CommandPrefix: string
    AliasPrefix: string
}

type BotConfig = {
    Commands: Map<string, Command>
    Prefixes: PrefixConfig
    PipeSeparator: string
}

module PrefixConfig =

    let create commandPrefix aliasPrefix = {
        CommandPrefix = commandPrefix
        AliasPrefix = aliasPrefix
    }

module BotConfig =

    let create commands prefixes pipeSepartor = {
        Commands = commands
        Prefixes = prefixes
        PipeSeparator = pipeSepartor
    }