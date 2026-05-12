namespace Chatbot.Core.Domain

open Chatbot.Database

type UserId = string
type Username = string

type ChannelId = string
type ChannelName = string

type MessageId = string

type User = {
    UserId: UserId
    Username: Username
    IsAdmin: bool
}

module User =

    let create userId username isAdmin = {
        UserId = userId
        Username = username
        IsAdmin = isAdmin
    }

    let fromDbUser (dbUser: Entities.DbUser) = {
        UserId = string dbUser.user_id
        Username = dbUser.username
        IsAdmin = dbUser.is_admin
    }