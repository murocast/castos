module Client

open Elmish
open Elmish.React
open Elmish.UrlParser
open Elmish.Navigation
open Fable.FontAwesome
open Fable.FontAwesome.Free
open Fable.React
open Fable.React.Props
open Thoth.Fetch
open Thoth.Json
open Fulma

open System
open Browser
open Browser
open Browser
open Elmish
open Fable.Import
open Shared

[<Literal>]
let BaseUrl = "http://localhost:80"

type LinkCode =
    | LinkCode of System.Guid
    | Invalid

type HttpRequest<'a> =
    | New
    | Pending
    | Success of 'a
    | Error

type AuthQuery =
    { LinkCode: string
      HouseholdId: string }

// The model holds data that you want to keep track of while the application is running
// in this case, we are keeping track of a counter
// we mark it as optional, because initially it will not be available from the client
// the initial value will be requested from server
type Model = { LinkCode: LinkCode
               HouseholdId: string
               Username: string
               Password: string
               Authorized: bool }

// The Msg type defines what events/actions can occur while the application is running
// the state of the application changes *only* in reaction to these events
type Msg =
    | Authorize
    | EmailChanged of string
    | PasswordChanged of string
    | Authorized of string

let postAuthorization linkCode model =
    promise {
        let data = { EMail = model.Username
                     Password = model.Password
                     HouseholdId = model.HouseholdId
                     LinkCode = linkCode }

        return! Fetch.post<SmapiAuthRendition,string>(BaseUrl + "/api/users/smapiauth", data)
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

let column (model : Model) (dispatch : Msg -> unit) =
    Column.column
        [ Column.Width (Screen.All, Column.Is4)
          Column.Offset (Screen.All, Column.Is4) ]
        [ Heading.h3
            [ Heading.Modifiers [ Modifier.TextColor IsGrey ] ]
            [ str "Login" ]
          Heading.p
            [ Heading.Modifiers [ Modifier.TextColor IsGrey ] ]
            [ str "Please login to proceed." ]
          Box.box' [ ]
            [ figure [ Class "avatar" ]
                [ img [ Src "https://placehold.it/128x128" ] ]
              form [ ]
                [ Field.div [ ]
                    [ Control.div [ ]
                        [ Input.email
                            [ Input.Size IsLarge
                              Input.Placeholder "Your Email"
                              Input.Props [ AutoFocus true ]
                              Input.OnChange (fun ev -> dispatch (EmailChanged ev.Value)) ] ] ]
                  Field.div [ ]
                    [ Control.div [ ]
                        [ Input.password
                            [ Input.Size IsLarge
                              Input.Placeholder "Your Password"
                              Input.OnChange (fun ev -> dispatch (PasswordChanged ev.Value)) ] ] ]
                  Button.button
                    [ Button.Color IsInfo
                      Button.IsFullWidth
                      Button.CustomClass "is-large is-block"
                      Button.OnClick (fun ev -> ev.preventDefault()
                                                dispatch Authorize) ]
                    [ str "Login" ] ] ]
          br [ ]
        ]

let view (model : Model) (dispatch : Msg -> unit) =
    Hero.hero
        [ Hero.Color IsSuccess
          Hero.IsFullHeight ]
        [ Hero.body [ ]
            [ Container.container
                [ Container.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Centered) ] ]
                [ column model dispatch ] ] ]


#if DEBUG
open Elmish.Debug
open Elmish.HMR
#endif

Program.mkProgram init update view
#if DEBUG
|> Program.withConsoleTrace
#endif
|> Program.withReactBatched "elmish-app"
#if DEBUG
|> Program.withDebugger
#endif
|> Program.run
