module Murocast.Client.View

open Murocast.Client.Router
open Murocast.Client.Domain

open Domain
open Feliz
open Feliz.Bulma
open Feliz.Router

let view (model:Model) (dispatch:Msg -> unit) =
    let render =
            match model.CurrentPage with
            | Anonymous pg ->
                match pg with
                | Login -> Pages.Login.View.view()
                | LinkSonos (linkcode,householdId) -> Pages.LinkSonos.View.view { LinkCode = linkcode; HouseholdId = householdId }
                | _ -> failwith "Unknown anonymous page"
            | Secured (pg, user) ->
                match pg with
                | Subscriptions -> Pages.Subscriptions.View.view ()
                | AddSubscription -> Pages.AddSubscription.View.AddSubscriptionView ()
                | _ -> failwith "Unknown secured page"

    React.router [
        router.pathMode
        router.onUrlChanged (Page.parseFromUrlSegments >> UrlChanged >> dispatch)
        router.children render
    ]