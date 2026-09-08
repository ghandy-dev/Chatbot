namespace Chatbot.Infrastructure.Services.Pastebin.Types

type PasteExpireDate =
    | Never
    | TenMinutes
    | Hour
    | Day
    | Week
    | TwoWeeks
    | Month
    | SixMonths
    | Year
    with

        member this.toString =
            match this with
            | Never -> "N"
            | TenMinutes -> "10M"
            | Hour -> "1H"
            | Day -> "1D"
            | Week -> "1W"
            | TwoWeeks -> "2W"
            | Month -> "1M"
            | SixMonths -> "6M"
            | Year -> "1Y"

type PasteVisibility =
    | Public
    | Unlisted
    | Private
    with

        member this.toString =
            match this with
            | Public -> "0"
            | Unlisted -> "1"
            | Private -> "2"
