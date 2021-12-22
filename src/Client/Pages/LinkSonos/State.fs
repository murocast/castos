module Murocast.Client.Pages.LinkSonos.State

open System
open Elmish

open Fable.Core
open Thoth.Fetch
open Thoth.Json

open Murocast.Shared

open Domain

[<Emit("__BASE_URL__")>]
let BaseUrl : string = jsNative

let postAuthorization linkCode model =
    promise {
        let data = { EMail = model.Username
                     Password = model.Password
                     HouseholdId = model.HouseholdId
                     LinkCode = linkCode }

        return! Fetch.post<SmapiAuthRendition,string>(BaseUrl + "/api/users/smapiauth", data, caseStrategy = CamelCase)
    }


let init linkCode householdId : Model * Cmd<Msg> =
    let initialModel = { LinkCode = LinkCode linkCode
                         HouseholdId = householdId
                         Username = ""
                         Password = ""
                         Authorized = false }

    initialModel, Cmd.none

let update (msg : Msg) (currentModel : Model) : Model * Cmd<Msg> =
    match msg with
    | EmailChanged s ->
        { currentModel with Username = s }, Cmd.none
    | PasswordChanged s ->
        { currentModel with Password = s }, Cmd.none
    | Authorize ->
        //create command
        match currentModel.LinkCode with
        | LinkCode g  -> currentModel, Cmd.OfPromise.perform (postAuthorization g) currentModel Authorized
        | _ -> currentModel, Cmd.none
    | Authorized _ ->
        currentModel, Cmd.none
