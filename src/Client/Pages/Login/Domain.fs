module Murocast.Client.Pages.Login.Domain

open Murocast.Shared.Errors
open Murocast.Shared.Auth.Communication
open Murocast.Client.Forms

type Model = {
    Form : ValidatedForm<Request.Login>
}

type Msg =
    | FormChanged of Request.Login
    | Login
    | LoggedIn of ServerResult<string>
