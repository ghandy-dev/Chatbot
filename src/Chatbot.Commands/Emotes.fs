namespace Commands

[<AutoOpen>]
module Emote =

    let randomEmote args context =
        let emote = context.Emotes.Random ()
        Message $"{emote.Name}"
