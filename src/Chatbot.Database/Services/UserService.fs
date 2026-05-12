namespace Chatbot.Database.Services

module UserService =

    open Chatbot.Database
    open Chatbot.Database.Types

    let create db =

        {
            new IUsersService with
                member _.Get userId = Users.get db userId
                member _.Add user = Users.add db user
        }
