module Murocast.Client.SharedView
open Murocast.Shared.Errors
open Murocast.Shared.Validation
open Feliz.Bulma
open Feliz
open Router

module Html =
    module Props =
        let routed (p:Page) =
            [
                prop.href (p |> Page.toUrlSegments |> Router.formatPath)
                prop.onClick (Router.goToUrl)
            ]

    let aRouted (text:string) (p:Page) =
        Html.a [
            yield! Props.routed p
            prop.text text
        ]

    let faIcon (icon:string) = Html.i [ prop.className icon; prop.style [ style.marginRight 5 ] ]
    let faIconSingle (icon:string) = Html.i [ prop.className icon ]

module ServerResponseViews =
    open Elmish
    open Elmish.Toastr

    let showErrorToast (e:ServerError) : Cmd<_> =
        let basicToaster =
            match e with
            | Validation v ->
                v
                |> List.map (fun x -> x.Field, ValidationErrorType.explain x.Type)
                |> List.map (fun (n,e) -> sprintf "%s : %s" n e)
                |> String.concat "<br/>"
                |> Toastr.message
                |> Toastr.title "Data nejsou vyplněna správně"
                |> Toastr.timeout 30000
                |> Toastr.extendedTimout 10000
            | Authentication e ->
                e
                |> AuthenticationError.explain
                |> Toastr.message
                |> Toastr.timeout 5000
                |> Toastr.extendedTimout 2000
            | error ->
                error
                |> ServerError.explain
                |> Toastr.message
                |> Toastr.title "Došlo k chybě"

        basicToaster
        |> Toastr.position ToastPosition.TopRight
        |> Toastr.hideEasing Easing.Swing
        |> Toastr.withProgressBar
        |> Toastr.showCloseButton
        |> Toastr.error

    let showSuccessToast msg : Cmd<_> =
        Toastr.message msg
        |> Toastr.position ToastPosition.TopRight
        |> Toastr.success

module ValidationViews =

    let help errors name =
        errors
        |> List.tryFind (fun x -> x.Field = name)
        |> Option.map (fun x ->
            Bulma.help [
                color.isDanger
                prop.text (x.Type |> ValidationErrorType.explain)
            ]
        )
        |> Option.defaultValue Html.none

    let color errors name =
        errors
        |> List.tryFind (fun x -> x.Field = name)
        |> Option.map (fun _ -> color.isDanger)
        |> Option.defaultValue (Interop.mkAttr "dummy" "")