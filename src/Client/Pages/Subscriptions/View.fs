module Murocast.Client.Pages.Subscriptions.View

open System

open Feliz
open Feliz.Bulma
open Feliz.UseElmish
open Fable.React.Helpers

open Murocast.Client.Router
open Murocast.Client.Forms
open Domain
open Murocast.Client.SharedView
open Murocast.Client.Template

let rows (subsriptions:Subscription list) =
    subsriptions
    |> List.map (fun (s:Subscription) ->
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
                    prop.text (s.Name)
                ]
                Bulma.mediaRight []
            ]
        ]
    )

[<ReactComponent>]
let view () = 
    let model, dispatch = React.useElmish(State.init, State.update, [| |])

    (rows model.Subsriptions)
    |> ofList
    |> inTemplate