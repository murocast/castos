module Murocast.Client.Server

// let exnToError (e:exn) : ServerError =
//     match e with
//     | :? Fable.Remoting.Client.ProxyRequestException as ex ->
//         if ex.StatusCode = 401 then
//             AuthenticationError.InvalidOrExpiredToken |> ServerError.Authentication
//         else
//             try
//                 let serverError = Json.parseAs<{| error: ServerError |}>(ex.Response.ResponseBody)
//                 serverError.error
//             with _ -> (ServerError.Exception(e.Message))
//     | _ -> (ServerError.Exception(e.Message))

// module Cmd =
//     open Elmish

//     module OfAsync =
//         let eitherAsResult f args resultMsg =
//             Cmd.OfAsync.either f args (Ok >> resultMsg) (exnToError >> Error >> resultMsg)