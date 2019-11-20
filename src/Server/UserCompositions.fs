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

type SmapiAuthRendition = { EMail: string
                            Password: string
                            LinkCode: Guid
                            HouseholdId: string }
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

let smapiauthComposition (db:Database.DatabaseConnection) eventStore rendition =
    match getUsersComposition eventStore with
    | Failure m -> fail "User not found"
    | Success users -> match (getUser users rendition.EMail) with
                       | None -> fail "User not found"
                       | Some u -> let correctPassword = (u.Password = rendition.Password)
                                   let authReq = db.GetAuthRequestByLinkToken rendition.LinkCode rendition.HouseholdId
                                   let found = authReq.IsSome
                                   match (correctPassword, found) with
                                   | true, true ->
                                        let updatedReq = { authReq.Value with
                                                            UserId = Some (u.Id)
                                                            Used = Some (System.DateTime.Now) }
                                        db.UpdateAuthRequest updatedReq |> ignore
                                        ok "Success"
                                   | _ -> fail "Auth not successful"

let usersRouter eventStore db = router {
    //pipe_through authorize  //<-- for all methods of router
    get "" (authorize >=> adminOnly >=>  (processAsync getUsersComposition eventStore))
    post "" (processDataAsync addUserComposition eventStore)
    post "/smapiauth" (processDataAsync (smapiauthComposition db) eventStore)
}

