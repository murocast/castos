module Murocast.Shared.Auth.Communication

open System

[<RequireQualifiedAccess>]
module Request =
    type Login = {
        Email : string
        Password : string
    }

    module Login =
        let init = { Email = ""; Password = "" }

    type TokenResult =
        {
            Token : string
        }

    type Register = {
        FirstName: string
        LastName: string
        Email: string
        Password: string
        SecondPassword: string
        AgreeButtonChecked : bool
        NewslettersButtonChecked : bool
    }

    module Register =
        let init = {
            FirstName = ""
            LastName = ""
            Email = ""
            Password = ""
            SecondPassword = ""
            AgreeButtonChecked = false
            NewslettersButtonChecked = false
        }

    type ForgottenPassword = {
        Email : string
    }

    module ForgottenPassword =
        let init = { Email = "" }

    type ResetPassword = {
        PasswordResetKey : Guid
        Password: string
        SecondPassword: string
    }

    module ResetPassword =
        let init = { Password = ""; SecondPassword = ""; PasswordResetKey = Guid.Empty }
