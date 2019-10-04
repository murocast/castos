module Castos.Users

open Castos.Events
open Castos.Auth

type AddUserRendition =
    {
        EMail: string
        Password: string
    }

let private apply state event =
    match event with
    | UserAdded data -> { Id = data.Id
                          Email = data.Email
                          Password = data.Password } :: state
    | _ -> failwith "unkown event"

let private evolve state events =
    events
    |> List.fold apply state

let getUsers events =
    evolve [] events

let getUser (users:User List) email =
    users |> List.tryFind (fun u -> u.Email = email)