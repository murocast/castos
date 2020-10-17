module Murocast.Client.Router

open System
open Browser.Types
open Feliz.Router
open Fable.Core.JsInterop

type AnonymousPage =
    | Login
    | Registration
    | AccountActivation of Guid
    | LinkSonos of linkcode:Guid * householdid:string
    | ForgottenPassword
    | ResetPassword of Guid

type SecuredPage =
    | Subscriptions
    | MyAccount

    // admin
    | Users
    | Feeds

[<RequireQualifiedAccess>]
module SecuredPage =
    let isAdminOnly = function
        | Users | Feeds -> true
        | _ -> false

type Page =
    | Anonymous of AnonymousPage
    | Secured of SecuredPage

[<RequireQualifiedAccess>]
module Page =

    let defaultPage = (Secured Subscriptions)

    module private Paths =
        let [<Literal>] Login = "login"
        let [<Literal>] Subscriptions = "subscriptions"
        let [<Literal>] MyAccount = "my-account"
        let [<Literal>] Users = "users"
        let [<Literal>] Feeds = "feeds"
        let [<Literal>] Registration = "registration"
        let [<Literal>] ForgottenPassword = "forgotten-password"
        let [<Literal>] LinkSonos = "link-sonos"

    let private basicMapping =
        [
            [ Paths.Login ], Anonymous Login
            [ Paths.Registration ], Anonymous Registration
            [ Paths.ForgottenPassword ], Anonymous ForgottenPassword
            [ Paths.Subscriptions ], Secured Subscriptions
            [ Paths.MyAccount ], Secured MyAccount
            [ Paths.Users ], Secured Users
            [ Paths.Feeds ], Secured Feeds
        ]

    let parseFromUrlSegments = function
        | [Paths.LinkSonos; Route.Query [ "linkcode", Route.Guid linkcode; "householdid", householdid ] ] -> Anonymous <| LinkSonos (linkcode, householdid) //TODO: query parameter as literals out of shared
        | path ->
            basicMapping
            |> List.tryFind (fun (p,_) -> p = path)
            |> Option.map snd
            |> Option.defaultValue (Secured Subscriptions)

    let toUrlSegments = function
        | page ->
            basicMapping
            |> List.tryFind (fun (_,p) -> p = page)
            |> Option.map fst
            |> Option.defaultValue []

module Router =
    let goToUrl (e:MouseEvent) =
        e.preventDefault()
        let href : string = !!e.currentTarget?attributes?href?value
        Cmd.navigatePath href |> List.map (fun f -> f ignore) |> ignore

    let navigatePage (p:Page) = p |> Page.toUrlSegments |> Array.ofList |> Cmd.navigatePath