module Murocast.Client.Pages.Subscriptions.View

open System

open Feliz
open Feliz.Bulma
open Feliz.UseElmish

open Murocast.Client.Router
open Murocast.Client.Forms
open Domain
open Murocast.Client.SharedView

let inTemplate (content:ReactElement list) =
    Bulma.hero [
        Bulma.heroBody [
            Bulma.columns [
                Bulma.column [
                    column.is4
                    column.isOffset4
                    text.hasTextCentered
                    prop.children content
                ]
            ]
        ]
    ]

let rows subsriptions =
    subsriptions
    |> List.map (fun (s:string) ->
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
                    prop.text s
                ]
                Bulma.mediaRight []
            ]
        ]
    )

let view = React.functionComponent(fun () ->
    let model, dispatch = React.useElmish(State.init, State.update, [| |])
    //Bulma.content "Bla"
    (rows model.Subsriptions)
    |> inTemplate
)