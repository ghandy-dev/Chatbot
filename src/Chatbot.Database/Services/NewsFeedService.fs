namespace Chatbot.Database.Services

module NewsFeedService =

    open Chatbot.Database
    open Chatbot.Database.Types

    let create db =

        {
            new INewsFeedService with
                member _.Get category = NewsFeeds.getFeeds db category
        }
