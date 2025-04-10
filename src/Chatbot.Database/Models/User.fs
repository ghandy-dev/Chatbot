namespace Database.Models

type User = {
    UserId: int
    Username: string
    IsAdmin: bool
} with

    static member create userId username = {
        UserId = userId
        Username = username
        IsAdmin = false
    }

type NewUser = {
    UserId: int
    Username: string
    IsAdmin: bool
} with

    static member create userId username = {
        UserId = userId
        Username = username
        IsAdmin = false
    }