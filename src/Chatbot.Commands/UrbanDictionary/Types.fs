namespace Commands.UrbanDictionary

module Types =

    open System
    open System.Text.Json.Serialization

    type Terms =
        { list: Term list }

    and Term =
        { Definition: string
          Permalink: string
          [<JsonPropertyName("thumbs_up")>]
          ThumbsUp: int
          Author: string
          Word: string
          DefId: int
          [<JsonPropertyName("current_vote")>]
          CurrentVote: string
          [<JsonPropertyName("written_on")>]
          WrittenOn: DateTime
          Example: string
          [<JsonPropertyName("thumbs_down")>]
          ThumbsDown: int
         }
