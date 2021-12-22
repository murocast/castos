module Murocast.Client.Pages.Subscriptions.Domain

open Murocast.Shared.Errors
open Murocast.Shared.Auth.Communication
open Murocast.Shared.Core.Subscriptions.Communication.Queries
open Murocast.Client.Forms

type Subscription = {
    FeedId : System.Guid
    Name : string
}

type Model = {
    Subsriptions : Subscription list
}

type Msg =
    | SubscriptionsLoaded of ServerResult<SubscriptionRendition list>
