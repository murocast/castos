module Murocast.Client.Pages.Subscriptions.View

open System

open Feliz
open Feliz.Bulma
open Feliz.UseElmish

open Murocast.Client.Router
open Murocast.Client.Forms
open Domain
open Murocast.Client.SharedView

let inTemplate (content:ReactElement) =
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

let view = React.functionComponent(fun () ->
    let model, dispatch = React.useElmish(State.init, State.update, [| |])
    Bulma.content "Bla"
    |> inTemplate
)