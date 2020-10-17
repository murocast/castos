module Murocast.Client.Pages.Login.State

open Murocast.Client.Router
open Elmish
open Domain

open Murocast.Shared.Auth.Communication
open Murocast.Shared.Auth.Validation
open Murocast.Client
open Murocast.Client.Forms
open Murocast.Client.Server
open Murocast.Client.SharedView

open Thoth.Fetch
open Thoth.Json

open Fable.Core

[<Emit("__BASE_URL__")>]
let BaseUrl : string = jsNative

let getToken (form:Request.Login) =
    promise {
        return! Fetch.post<Request.Login,string>(BaseUrl + "/token", form, caseStrategy = CamelCase)
    }

let init () =
    {
        Form = Request.Login.init |> ValidatedForm.init
    }, Cmd.none

let update (msg:Msg) (model:Model) : Model * Cmd<Msg> =
    match msg with
    | FormChanged f ->
        { model with Form = model.Form |> ValidatedForm.updateWith f |> ValidatedForm.validateWithIfSent validateLogin }, Cmd.none
    | Login ->
        let model = { model with Form = model.Form |> ValidatedForm.validateWith validateLogin |> ValidatedForm.markAsSent }
        if model.Form |> ValidatedForm.isValid then
            { model with Form = model.Form |> ValidatedForm.startLoading }, Cmd.OfPromise.eitherAsResult getToken model.Form.FormData LoggedIn
        else model, Cmd.none
    | LoggedIn res ->
        let model = { model with Form = model.Form |> ValidatedForm.stopLoading }
        match res with
        | Ok token ->
            TokenStorage.setToken token
            { model with Form = Request.Login.init |> ValidatedForm.init },
                Cmd.batch [ ServerResponseViews.showSuccessToast "Login successful!"; Router.navigatePage Page.defaultPage ]
        | Error e ->
            model, e |> ServerResponseViews.showErrorToast