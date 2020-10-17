module Murocast.Client.Server

open Murocast.Shared.Errors

let exnToError (e:exn) : ServerError =
   ServerError.Exception(e.Message)

module Cmd =
    open Elmish

    module OfPromise =
        let eitherAsResult f args resultMsg =
            Cmd.OfPromise.either f args (Ok >> resultMsg) (exnToError >> Error >> resultMsg)