module Murocast.Client.Pages.Login.View

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
    Bulma.box [
        Html.img [ prop.src "https://placehold.it/128x128" ]
        Bulma.field.div [
            Bulma.fieldBody [
                Bulma.input.text [
                    ValidationViews.color model.Form.ValidationErrors (nameof(model.Form.FormData.Email))
                    prop.placeholder "Email"
                    prop.onTextChange (fun x -> { model.Form.FormData with Email = x } |> FormChanged |> dispatch)
                    prop.valueOrDefault model.Form.FormData.Email
                ]
            ]
            ValidationViews.help model.Form.ValidationErrors (nameof(model.Form.FormData.Email))
        ]
        Bulma.field.div [
            Bulma.fieldBody [
                Bulma.input.password [
                    ValidationViews.color model.Form.ValidationErrors (nameof(model.Form.FormData.Password))
                    prop.placeholder "Password"
                    prop.onTextChange (fun x -> { model.Form.FormData with Password = x } |> FormChanged |> dispatch)
                    prop.valueOrDefault (model.Form.FormData.Password)
                ]
            ]
            ValidationViews.help model.Form.ValidationErrors (nameof(model.Form.FormData.Password))
        ]
        Bulma.field.div [
            Bulma.fieldBody [
                Bulma.button.button [
                    yield color.isPrimary
                    yield button.isFullWidth
                    if model.Form.IsLoading then yield! [ button.isLoading; prop.disabled true ]
                    yield prop.text "Login"
                    yield prop.onClick (fun _ -> Login |> dispatch)
                ]
            ]
        ]
        Html.div [
            Html.aRouted "Register" (Anonymous Registration)
            Html.span " · "
            Html.aRouted "Reset password" (Anonymous ForgottenPassword)
        ]
    ]
    |> inTemplate
)