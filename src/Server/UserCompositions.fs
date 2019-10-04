module Castos.UserCompositions

open Castos.Users
open Castos.Auth
open Castos
open Castos.Http
open System
open Giraffe
open Saturn
open FSharp.Control.Tasks.V2


open Microsoft.AspNetCore.Http
open Microsoft.IdentityModel.Tokens

open Microsoft.AspNetCore.Authentication.JwtBearer
open System.IdentityModel.Tokens.Jwt
open System.Security.Claims

open FSharp.Data

let private storeUsersEvent eventStore (version, event) =
    let streamId event = StreamId (sprintf "users")
    eventStore.SaveEvents (streamId event) version [event]

let getUsersComposition eventStore =
    let result = eventStore.GetEvents (StreamId("users"))
    match result with
    | Success (_, events) -> ok (getUsers events)
    | Failure m -> fail m

let getUserComposition eventStore email =
    match getUsersComposition eventStore with
    | Success users -> Success (getUser users email)
    | Failure m -> fail m

let addUserComposition eventStore rendition =
    let result = (StreamVersion 0, UserAdded {
                    Id = Guid.NewGuid() |> UserId
                    Email = rendition.Email
                    Password = rendition.Password
                    //TODO: Hash and Salt
                 }) |> storeUsersEvent eventStore
    match result with
    | Success _ -> ok ("added user")
    | Failure m -> fail m


let usersRouter eventStore = router {
    //pipe_through authorize  //<-- for all methods of router
    get "" (authorize >=> adminOnly >=>  (processAsync getUsersComposition eventStore))
    post "" (processDataAsync addUserComposition eventStore)
}

