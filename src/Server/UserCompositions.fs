module Castos.UserCompositions

open Castos.Users
open Castos.Auth
open Castos
open Castos.Http
open System
open Giraffe
open Saturn
open Shared
open Castos.FeedCompositions
type AddUserRendition =
    {
        EMail: string
        Password: string
    }

let private streamId = sprintf "users"

let private storeUsersEvent eventStore event =
    storeEvent eventStore (fun _ -> streamId) event

let getUsersComposition eventStore =
    let (events, _) = getAllEventsFromStreamById eventStore streamId
    ok (getUsers events)

let getUserComposition eventStore email =
    match getUsersComposition eventStore with
    | Success users -> Success (getUser users email)
    | Failure m -> fail m

let addUserComposition eventStore (rendition:AddUserRendition) =
    UserAdded { Id = Guid.NewGuid() |> UserId
                Email = rendition.EMail
                Password = rendition.Password } //TODO: Hash and Salt
    |> storeUsersEvent eventStore

    ok ("added user")


let smapiauthComposition (db:Database.DatabaseConnection) eventStore (rendition:SmapiAuthRendition) =
    match getUsersComposition eventStore with
    | Failure m -> fail "Users not found"
    | Success users -> match (getUser users rendition.EMail) with
                       | None -> fail "User not found"
                       | Some u -> let correctPassword = (u.Password = rendition.Password)
                                   let authReq = db.GetAuthRequestByLinkToken rendition.LinkCode rendition.HouseholdId
                                   let found = authReq.IsSome
                                   match (correctPassword, found) with
                                   | true, true ->
                                        let updatedReq = { authReq.Value with
                                                            UserId = Some (u.Id) }
                                        db.UpdateAuthRequest updatedReq |> ignore
                                        ok "Success"
                                   | _ -> fail "Auth not successful"

let usersRouter eventStore db = router {
    //pipe_through authorize  //<-- for all methods of router
    get "" (authorize >=> adminOnly >=>  (processAsync getUsersComposition eventStore))
    post "" (processDataAsync addUserComposition eventStore)
    post "/smapiauth" (processDataAsync (smapiauthComposition db) eventStore)
}

