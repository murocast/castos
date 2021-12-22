module Murocast.Client.Pages.AddSubscription.View

open System

open Feliz
open Feliz.Bulma
open Feliz.UseElmish

open Murocast.Shared.Core.Subscriptions.Communication.Queries

open Murocast.Client.Router
open Murocast.Client.Forms
open Domain
open Murocast.Client.SharedView
open Murocast.Client.Template


let rows (feeds:FoundFeedRendition list) =
    feeds
    |> List.map (fun (f:FoundFeedRendition) ->
        Bulma.media [
            prop.children [
                Bulma.mediaLeft [
                    Html.p [
                        prop.className "image is-64x64"
                        prop.children [
                            Html.img [
                                prop.src "https://bulma.io/images/placeholders/128x128.png"
                            ]
                        ]
                    ]
                ]
                Bulma.mediaContent [
                    prop.text (f.Name)
                ]
                Bulma.mediaRight []
            ]
        ]
    )

let searchFormView model dispatch =
    Bulma.columns [
        Bulma.column ([
            Bulma.field.div[
                Bulma.field.hasAddons
                prop.children [
                    Bulma.control.div [
                        Bulma.input.text [
                            prop.placeholder "Find podcasts"
                            prop.onTextChange (fun text -> dispatch (FormChanged {model.FindFeedsForm with Searchstring = text}))
                        ]
                    ]
                    Bulma.control.div [
                        Bulma.button.a [
                            Bulma.color.isPrimary
                            prop.text "Search"
                            prop.onClick (fun _ -> dispatch (FindFeeds model.FindFeedsForm.Searchstring))
                        ]
                    ]
                ]
            ]
        ] @ (rows model.Feeds))
    ]

[<ReactComponent>]
let AddSubscriptionView () =
    let model, dispatch = React.useElmish(State.init, State.update, [| |])

    searchFormView model dispatch
    |> inTemplate
