namespace Chatbot.Commands.Types

module Wikipedia =

    open System.Text.Json.Serialization

    type Thumbnail = {
        Mimetype: string
        // Size: int option
        Width: int
        Height: int
        // Duration: float option
        Url: string
    }

    type Page = {
        Id: int
        Key: string
        Title: string
        Excerpt: string
        // [<JsonPropertyName("matched_title")>]
        // MatchedTitle: string option
        Description: string
        // Thumbnail: Thumbnail option
    }

    type Pages =
        { Pages: Page list }
