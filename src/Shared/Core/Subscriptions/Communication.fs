module Murocast.Shared.Core.Subscriptions.Communication

open System

module Queries =

    type SubscriptionRendition = {
        FeedId : Guid
        Name: string
    }

    type FoundFeedRendition = {
        FeedId : Guid
        Name : string
        Url : string
        Subscribed : bool
        LastEpisodeDate : DateTime option
    }

[<RequireQualifiedAccess>]
module Request =
    type FindFeeds = { Searchstring : string }
    module FindFeeds =
        let init = { Searchstring = "" }