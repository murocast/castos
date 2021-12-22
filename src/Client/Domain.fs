module Murocast.Client.Domain

open System
open Router
open Murocast.Shared.Core.UserAccount.Domain.Queries
open Murocast.Shared.Errors

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
    | UserRefreshed of ServerResult<AuthenticatedUser>
    | RefreshUserWithRedirect of SecuredPage
    | UserRefreshedWithRedirect of SecuredPage * ServerResult<AuthenticatedUser>
    | RefreshToken of string
    | TokenRefreshed of ServerResult<string>
    | LoggedOut
    //| ResendActivation of Guid
    //| ActivationResent
    // navigation
    | UrlChanged of Page
    // global
    | ShowTerms of bool