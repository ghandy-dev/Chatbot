namespace Chatbot.Commands

[<AutoOpen>]
module Chance =

    open Chatbot.Core.Domain.Commands

    let random = System.Random.Shared

    let chance _ =
        let n = random.NextDouble() * 100.0
        let message = $"""{n.ToString("n2")}%%"""

        Ok [ Message message ]