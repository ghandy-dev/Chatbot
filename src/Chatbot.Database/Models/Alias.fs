namespace Database.Models

type Alias = {
    Name: string
    Command: string
}

type NewAlias = {
    UserId: int
    Name: string
    Command: string
} with

    static member create userId name command = {
        UserId = userId
        Name = name
        Command = command
    }

type UpdateAlias = {
    UserId: int
    Name: string
    Command: string
} with

    static member create userId name command = {
        UserId = userId
        Name = name
        Command = command
    }

type DeleteAlias = {
    UserId: int
    Name: string
} with

    static member create userId name = {
        UserId = userId
        Name = name
    }