namespace Castos

open Microsoft.AspNetCore.Http
open FSharp.Control.Tasks.V2
open System.IO

module Http =
    let rawFormString (x:HttpContext) =
        task {
            use reader = new StreamReader(x.Request.Body)
            return! reader.ReadToEndAsync()
        }