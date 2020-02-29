module Castos.Users

open Castos.Events
open Castos.Auth

let private apply state event =
    match event with
    | UserAdded data -> { Id = data.Id
                          Email = data.Email
                          PasswordHash = data.PasswordHash
                          Salt = data.Salt
                          Roles = [] } :: state
    | _ -> failwith "unkown event"

let private evolve state events =
    events
    |> List.fold apply state

let getUsers events =
    evolve [] events

let getUser (users:User List) email =
    users |> List.tryFind (fun u -> u.Email = email)