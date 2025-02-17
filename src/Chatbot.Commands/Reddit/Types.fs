namespace Reddit

module Types =

    open System
    open System.Text.Json.Serialization

    type T3 = {
        Title: string
        Subreddit: string
        Quarantine: bool
        Score: int
        [<JsonPropertyName("created_utc")>]
        CreatedUtc: float
        [<JsonPropertyName("over_18")>]
        Over18: bool
        Url: string
        [<JsonPropertyName("is_self")>]
        IsSelf: bool
        [<JsonPropertyName("crosspost_parent_list")>]
        CrosspostParentList: T3 list option
        [<JsonPropertyName("link_flair_text")>]
        Flair: string option
    }

    type Listing<'T> = {
        Before: string option
        After: string option
        ModHash: string
        Children: Thing<'T> list
    }

    and Thing<'T> = {
        Id: string option
        Name: string option
        Kind: string
        Data: 'T
    }

    type OAuthToken = {
        [<JsonPropertyName("access_token")>]
        AccessToken: string
        [<JsonPropertyName("token_type")>]
        TokenType: string
        [<JsonPropertyName("expires_in")>]
        ExpiresIn: int
        Scope: string
    }
