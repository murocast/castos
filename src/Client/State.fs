module Murocast.Client.State

open System

open Elmish
open Feliz.Router

open Murocast.Client.Domain
open Murocast.Client.Router
open Murocast.Client.Server

open Thoth.Fetch
open Thoth.Json
open Murocast.Shared.Core.UserAccount.Domain.Queries

let getUserInfo() : Fable.Core.JS.Promise<AuthenticatedUser> =
    promise {
        return! Fetch.get "/api/users/userinfo"
    }

let navigateToAnonymous (p:AnonymousPage) (m:Model) =
    { m with CurrentPage = CurrentPage.Anonymous(p) }

let navigateToSecured user (p:SecuredPage) (m:Model) =
    { m with CurrentPage = CurrentPage.Secured(p, user) }

let refreshUser user (m:Model) =
    match m.CurrentPage with
    | CurrentPage.Anonymous _ -> m
    | CurrentPage.Secured(p,_) -> navigateToSecured user p m

let init () =
    let nextPage = (Router.currentPath() |> Page.parseFromUrlSegments)
    {
        CurrentPage = CurrentPage.Anonymous Login
        IsCheckingUser = false
        ShowTerms = false
    }, (nextPage |> UrlChanged |> Cmd.ofMsg)

let update (msg:Msg) (model:Model) : Model * Cmd<Msg> =
    match msg with
    | UrlChanged page ->
        match model.CurrentPage, page with
        | CurrentPage.Secured(_,user), Page.Secured targetPage ->
            if SecuredPage.isAdminOnly targetPage && false then // TODO: not user.IsAdmin then
                model, Cmd.ofMsg LoggedOut
            else
                let newModel = model |> navigateToSecured user targetPage
                newModel, Cmd.none
        | CurrentPage.Anonymous _, Page.Anonymous targetPage
        | CurrentPage.Secured _, Page.Anonymous targetPage ->
            let newModel = model |> navigateToAnonymous targetPage
            newModel, Cmd.none
        | CurrentPage.Anonymous _, Page.Secured targetpage ->
            model, Cmd.ofMsg <| RefreshUserWithRedirect(targetpage)
    | RefreshUserWithRedirect p ->
        { model with IsCheckingUser = true }, Cmd.OfPromise.perform getUserInfo () (fun u -> UserRefreshedWithRedirect(p,u))
     | UserRefreshedWithRedirect(p, u) ->
         let model = { model with IsCheckingUser = false }
         model |> navigateToSecured u p, Router.navigatePage (Page.Secured p)
    | RefreshUser ->
        model, Cmd.OfPromise.perform getUserInfo () UserRefreshed
     | UserRefreshed usr ->
        model |> refreshUser usr, Cmd.none
    | RefreshToken token -> model, [] //  Cmd.OfAsync.eitherAsResult authService.RefreshToken token TokenRefreshed
    // | TokenRefreshed res ->
    //     match res with
    //     | Ok t ->
    //         TokenStorage.setToken t
    //         model, Cmd.none
    //     | Error _ -> model, Cmd.ofMsg LoggedOut
    | LoggedOut ->
        TokenStorage.removeToken()
        model, Router.navigatePage (Page.Anonymous Login)
    // auth
    //| ResendActivation i -> model, [] // Cmd.OfAsync.eitherAsResult authService.ResendActivation i ActivationResent
    // | ActivationResent _ ->
    //     model, [
    //         SharedView.ServerResponseViews.showSuccessToast "Nyní se podívejte do vaší emailové schránky"
    //         Cmd.ofMsg LoggedOut
    //     ] |> Cmd.batch
    | ShowTerms show -> { model with ShowTerms = show }, Cmd.none

let subscribe (_:Model) =
    let sub dispatch =
        let timer = (TimeSpan.FromMinutes 1.).TotalMilliseconds |> int

        let handler _ =
            match TokenStorage.tryGetToken() with
            | Some t -> dispatch (RefreshToken t) |> ignore
            | None -> Cmd.none |> ignore
        Browser.Dom.window.setInterval(handler, timer) |> ignore
    Cmd.ofSub sub