namespace Chatbot.Database.Services

module RpsService =

    open Chatbot.Database
    open Chatbot.Database.Types

    let create db =

        {
            new IRockPaperScissorsService with
                member _.Add rpsStats = Rps.add db rpsStats
                member _.Get userId = Rps.get db userId
                member _.Update rpsStats = Rps.update db rpsStats
        }
