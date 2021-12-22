module Murocast.Client.Pages.AddSubscription.Domain

open Murocast.Shared.Errors
open Murocast.Shared.Auth.Communication
open Murocast.Shared.Core.Subscriptions.Communication.Queries
open Murocast.Shared.Core.Subscriptions.Communication
open Murocast.Client.Forms

type Model = {
    FindFeedsForm : Request.FindFeeds
    Feeds : FoundFeedRendition list
}

type Msg =
    | FormChanged of Request.FindFeeds
    | FindFeeds of string
    | FoundFeedsLoaded of ServerResult<FoundFeedRendition list>

