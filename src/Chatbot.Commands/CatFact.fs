namespace Chatbot.Commands

[<AutoOpen>]
module CatFacts =

    open FsToolkit.ErrorHandling

    open Chatbot.Core.Domain.Commands
    open Chatbot.Core.Services.CatFact

    let catFact (catFactService: ICatFactService) context =

        asyncResult {
            let! fact = catFactService.GetCatFact () |> AsyncResult.mapError (CommandHttpError.fromHttpStatusCode "Cat Fact")

            return [ Message fact.Fact ]
        }
