namespace Chatbot.Database.Services

module AliasService =

    open Chatbot.Database
    open Chatbot.Database.Types

    let create db =

        {
            new IAliasService with
                member _.Add alias = Aliases.add db alias
                member _.Delete aliasName userId = Aliases.delete db aliasName userId
                member _.Get userId aliasName = Aliases.get db userId aliasName
                member _.Update alias = Aliases.update db alias
        }
