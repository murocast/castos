module Murocast.Client.Domain

open System
open Router
open Murocast.Shared.Core.UserAccount.Domain.Queries

type CurrentPage =
    | Anonymous of AnonymousPage
    | Secured of SecuredPage * Murocast.Shared.Core.UserAccount.Domain.Queries.AuthenticatedUser

type Model = {
    IsCheckingUser : bool
    CurrentPage : CurrentPage
    ShowTerms : bool
}

type Msg =
    // auth
    | RefreshUser
    | UserRefreshed of AuthenticatedUser
    | RefreshUserWithRedirect of SecuredPage
    | UserRefreshedWithRedirect of SecuredPage * AuthenticatedUser
    | RefreshToken of string
    //| TokenRefreshed of string
    | LoggedOut
    //| ResendActivation of Guid
    //| ActivationResent
    // navigation
    | UrlChanged of Page
    // global
    | ShowTerms of bool