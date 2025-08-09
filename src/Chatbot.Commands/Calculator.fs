namespace Commands

// https://github.com/stephan-tolksdorf/fparsec/blob/fdd990ad5abe32fd65d926005b4c7bd71dd2384f/Samples/Calculator/calculator.fs
[<AutoOpen>]
module Calculator =

    open FParsec

    let private ws = spaces
    let private str_ws s = pstring s .>> ws

    let private ob = "("
    let private cb = ")"

    let private ob_ws = str_ws ob
    let private cb_ws = str_ws cb

    let private number = pfloat .>> ws

    let private opp = new OperatorPrecedenceParser<float, unit, unit>()

    opp.AddOperator(InfixOperator("+", ws, 1, Associativity.Left, (+)))
    opp.AddOperator(InfixOperator("-", ws, 1, Associativity.Left, (-)))
    opp.AddOperator(InfixOperator("*", ws, 2, Associativity.Left, (*)))
    opp.AddOperator(InfixOperator("/", ws, 2, Associativity.Left, (/)))
    opp.AddOperator(InfixOperator("%", ws, 2, Associativity.Left, (%)))
    opp.AddOperator(InfixOperator("^", ws, 3, Associativity.Right, (fun x y -> x ** y)))
    opp.AddOperator(PrefixOperator("-", ws, 4, true, (fun x -> -x)))

    let private ws1 = nextCharSatisfiesNot isLetter >>. ws

    opp.AddOperator(PrefixOperator("sqrt", ws1, 3, true, sqrt))
    opp.AddOperator(PrefixOperator("log", ws1, 4, true, log))
    opp.AddOperator(PrefixOperator("exp", ws1, 4, true, exp))

    let private expr = opp.ExpressionParser

    let private parseExpr = number <|> between ob_ws cb_ws expr

    opp.TermParser <- parseExpr

    let private completeExpression = ws >>. expr .>> eof

    let private innerCalculate s = run completeExpression s

    let calculate (input: string list) =
        let input = String.concat " " input
        let result = innerCalculate input

        match result with
        | Success(r, _, _) -> Result.Ok <| CommandOk.Message $"{r}"
        | Failure(msg, _, _) -> Result.Ok <| CommandOk.Message msg
