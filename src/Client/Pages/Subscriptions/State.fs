module Murocast.Client.Pages.Subscriptions.State

open Elmish
open Domain
open Murocast.Client.Server

open Murocast.Shared.Core.Subscriptions.Communication.Queries
open Murocast.Client.SharedView

let getSubscriptions() : Fable.Core.JS.Promise<SubscriptionRendition list> =
    promise {
        return! getJsonPromise "api/subscriptions"
    }

let init () = {
                Subsriptions = []
              }, Cmd.OfPromise.eitherAsResult getSubscriptions () SubscriptionsLoaded

let update (msg:Msg) (model:Model) : Model * Cmd<Msg> =
    match msg with
    | SubscriptionsLoaded (Ok subs) ->
        let rows = subs
                   |> List.map (fun s -> string s.FeedId)
        { model with Subsriptions = rows }, []
    | SubscriptionsLoaded (Error e) ->
        model, e |> ServerResponseViews.showErrorToast