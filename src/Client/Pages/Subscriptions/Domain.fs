module Murocast.Client.Pages.Subscriptions.Domain

open Murocast.Shared.Errors
open Murocast.Shared.Auth.Communication
open Murocast.Client.Forms

type Model = {
    Subsriptions : string list
}

type Msg =
    | FormChanged of Request.Login
    | Login
    | LoggedIn of ServerResult<string>
