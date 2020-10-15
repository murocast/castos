module Murocast.Client.State

open System
open Elmish
open Feliz.Router

open Elmish
open Elmish.React
open Elmish.UrlParser
open Elmish.Navigation
open Fable.Core
open Fable.React
open Fable.React.Props
open Thoth.Fetch
open Thoth.Json
open Fulma

open System
open Browser
open Shared

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


// defines the initial state and initial command (= side-effect) of the application
let init () : Model * Cmd<Msg> =

    let parser = UrlParser.top <?> stringParam "linkcode" <?> stringParam "householdid"
    //let linkCodeOption = UrlParser.parsePath (UrlParser.top <?> stringParam "linkcode" <?> stringParam "householdid") Dom.window.location
    let mapper linkcode householdid =
        match linkcode, householdid with
        | Some l, Some h -> Some ({ LinkCode = l; HouseholdId = h })
        | _ -> None
    let mapped = UrlParser.map mapper parser
    let result = UrlParser.parsePath mapped Dom.window.location
    let authQuery = Option.bind (fun (oa:option<AuthQuery>) -> match oa with
                                                               | Some s -> match Guid.TryParse s.LinkCode with
                                                                           | (true, g) -> Some (LinkCode g, s.HouseholdId)
                                                                           | (false, _) -> None
                                                               | None -> None ) result
                                |> Option.defaultValue (Invalid, "INVALID")

    let initialModel = { LinkCode = fst authQuery
                         HouseholdId = snd authQuery
                         Username = ""
                         Password = ""
                         Authorized = false }

    initialModel, Cmd.none

// The update function computes the next state of the application based on the current state and the incoming events/messages
// It can also run side-effects (encoded as commands) like calling the server via Http.
// these commands in turn, can dispatch messages to which the update function will react.
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
