module Murocast.Client.Pages.Subscriptions.State

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
        Subsriptions = []
    }, Cmd.none

let update (msg:Msg) (model:Model) : Model * Cmd<Msg> =
    model, []