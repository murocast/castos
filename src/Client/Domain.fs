module Murocast.Client.Domain

open System
open Router

type CurrentPage =
    | Anonymous of AnonymousPage
    | Secured of SecuredPage * int //Yobo.Shared.Core.UserAccount.Domain.Queries.UserAccount

type Model = {
    IsCheckingUser : bool
    CurrentPage : CurrentPage
    ShowTerms : bool
}

type Msg =
    // auth
    | RefreshUser
    //| UserRefreshed of ServerResult<Yobo.Shared.Core.UserAccount.Domain.Queries.UserAccount>
    | RefreshUserWithRedirect of SecuredPage
    //| UserRefreshedWithRedirect of SecuredPage * ServerResult<Yobo.Shared.Core.UserAccount.Domain.Queries.UserAccount>
    | RefreshToken of string
    //| TokenRefreshed of ServerResult<string>
    | LoggedOut
    | ResendActivation of Guid
    //| ActivationResent of ServerResult<unit>
    // navigation
    | UrlChanged of Page
    // global
    | ShowTerms of bool