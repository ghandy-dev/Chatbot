namespace Services

module Weather =

    open Azure.Types
    open Azure.Maps.Weather

    type IWeatherApi =
        abstract member getCurrentWeather: latitude: double -> longitude: double -> Async<Result<CurrentConditions, string>>

    let api =
        { new IWeatherApi with
            member _.getCurrentWeather latitude longitude = getCurrentWeather latitude longitude
        }

module Geolocation =

    open Azure.Types
    open Azure.Maps.Geolocation
    open Google.Types
    open Google.Timezone

    type IGeolocationApi =
        abstract member getReverseAddress: latitude: double -> longitude: double -> Async<Result<ReverseSearchAddressResultItem, string>>
        abstract member getSearchAddress: address: string -> Async<Result<SearchAddressResultItem, string>>
        abstract member getTimezone: latitude: double -> longitude: double -> timestamp: int64 -> Async<Result<Timezone, string>>

    let api =
        { new IGeolocationApi with
            member _.getReverseAddress latitude longitude = getReverseAddress latitude longitude
            member _.getSearchAddress address = getSearchAddress address
            member _.getTimezone latitude longitude timestamp = getTimezone latitude longitude timestamp
        }