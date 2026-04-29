namespace Chatbot.Core

module Caching =

    open Microsoft.Extensions.Caching.Memory

    type MemoryCache = Microsoft.Extensions.Caching.Memory.MemoryCache

    module MemoryCache =

        let empty () =
            new MemoryCache(
                let options = new MemoryCacheOptions()
                options
            )

        let tryGetValue<'T> key (cache: MemoryCache) =
            match cache.TryGetValue<'T> key with
            | false, _ -> None
            | true, value -> Some value

        let set key value (cache: MemoryCache) = cache.Set(key, value)
        let createEntry key value (cache: MemoryCache) = cache.CreateEntry (key, value)
