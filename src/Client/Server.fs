module Murocast.Client.Server

open Murocast.Shared.Errors

open Fable.Core
open Fetch.Types
open Thoth.Fetch
open Thoth.Json


[<Emit("__BASE_URL__")>]
let BaseUrl : string = jsNative

let exnToError (e:exn) : ServerError =
   ServerError.Exception(e.Message)

let inline getJsonPromise url =
    promise {
        let headers = TokenStorage.tryGetToken()
                    |> Option.map (sprintf "Bearer %s" >> HttpRequestHeaders.Authorization)
                    |> Option.toList
                    |> List.append [ HttpRequestHeaders.ContentType "application/json" ]
        return! Fetch.get (BaseUrl + url, headers = headers, caseStrategy = CaseStrategy.CamelCase)
    }

let inline postJsonPromise url data =
    promise {
        let headers = TokenStorage.tryGetToken()
                                |> Option.map (sprintf "Bearer %s" >> HttpRequestHeaders.Authorization)
                                |> Option.toList
                                |> List.append [ HttpRequestHeaders.ContentType "application/json" ]

        return! Fetch.post (url = BaseUrl +  url,
                            data = data,
                            headers = headers,
                            caseStrategy = CaseStrategy.CamelCase)
    }


module Cmd =
    open Elmish

    module OfPromise =
        let eitherAsResult f args resultMsg =
            Cmd.OfPromise.either f args (Ok >> resultMsg) (exnToError >> Error >> resultMsg)