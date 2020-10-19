namespace Castos

module Http =
    open Giraffe
    open Microsoft.AspNetCore.Http
    open FSharp.Control.Tasks.V2
    open System.IO
    open Auth

    let rawFormString (x:HttpContext) =
        task {
            use reader = new StreamReader(x.Request.Body)
            return! reader.ReadToEndAsync()
        }

    let getJson (ctx: HttpContext) =
        ctx.BindJsonAsync()

    let processAsync f eventStore =
        fun next ctx ->
            task {
                let result = f eventStore
                return! match result with
                        | Success (a) -> Successful.OK a next ctx
                        | Failure (_) -> RequestErrors.BAD_REQUEST "Error" next ctx
            }

    let processDataAsync f eventStore =
        fun next ctx ->
            task {
                let! data = getJson ctx
                let result = f eventStore data
                return! match result with
                        | Success (a) -> Successful.OK a next ctx
                        | Failure (m) -> RequestErrors.BAD_REQUEST m next ctx
            }

    let processAuthorizedAsync f eventStore =
        fun next (ctx:HttpContext) ->
            task {
                let result = f eventStore (claimsToAuthUser ctx.User)
                return! match result with
                        | Success (a) -> Successful.OK a next ctx
                        | Failure (_) -> RequestErrors.BAD_REQUEST "Error" next ctx
            }

    let processDataAuthorizedAsync f eventStore =
        fun next ctx ->
                task {
                    let! data = getJson ctx
                    let result = f eventStore data (claimsToAuthUser ctx.User)
                    return! match result with
                            | Success (a) -> Successful.OK a next ctx
                            | Failure (_) -> RequestErrors.BAD_REQUEST "Error" next ctx
                }

    let returnUser() =
        fun next (ctx:HttpContext) ->
            task {
                return! Successful.OK (claimsToAuthUser ctx.User) next ctx
            }