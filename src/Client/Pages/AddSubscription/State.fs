module Murocast.Client.Pages.AddSubscription.State

open System
open Elmish
open Domain
open Murocast.Client.Server

open Murocast.Shared.Core.Subscriptions.Communication
open Murocast.Shared.Core.Subscriptions.Communication.Queries
open Murocast.Client.SharedView

let getSubscriptions() : Fable.Core.JS.Promise<SubscriptionRendition list> =
    promise {
        return! getJsonPromise "/api/subscriptions"
    }

let getMessages() =
    Ok [{  FeedId = Guid.NewGuid()
           Name = "Logbuch Netzpolitik"
           Url = "https://logbuchnetzpolitik.fm/feed"
           Subscribed = false
           LastEpisodeDate = Some (DateTime(2021,1,12)) }]

let init () =
    { Feeds = []
      FindFeedsForm = Request.FindFeeds.init }, []

let update (msg:Msg) (model:Model) : Model * Cmd<Msg> =
    match msg with
    | FormChanged f ->
        { model with FindFeedsForm = f }, []
    | FindFeeds s ->
        model, Cmd.ofMsg (FoundFeedsLoaded (getMessages()))
    | FoundFeedsLoaded result ->
        match result with
        | Ok feeds -> { model with Feeds = feeds}, []
        | Error e -> model, ServerResponseViews.showErrorToast e