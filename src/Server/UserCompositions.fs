module Castos.UserCompositions

open Castos.Users
open Castos.Auth
open Castos
open Castos.Http
open System
open Giraffe
open Saturn

type AddUserRendition =
    {
        EMail: string
        Password: string
    }
let private streamId = StreamId (sprintf "users")

let private storeUsersEvent eventStore (version, event) =    
    eventStore.SaveEvents streamId version [event]

let getUsersComposition eventStore =
    let result = eventStore.GetEvents streamId
    match result with
    | Success (_, events) -> ok (getUsers events)
    | Failure m -> fail m

let getUserComposition eventStore email =
    match getUsersComposition eventStore with
    | Success users -> Success (getUser users email)
    | Failure m -> fail m

let addUserComposition eventStore (rendition:AddUserRendition) =
    let result = (StreamVersion 0, UserAdded {
                    Id = Guid.NewGuid() |> UserId
                    Email = rendition.EMail
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

