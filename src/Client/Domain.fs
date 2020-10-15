module Murocast.Client.Domain

open Elmish
open Elmish.React
open Elmish.UrlParser
open Elmish.Navigation
open Fable.Core
open Fable.React
open Fable.React.Props
open Thoth.Fetch
open Thoth.Json
open Fulma

open System
open Browser
open Shared




type LinkCode =
    | LinkCode of System.Guid
    | Invalid

type HttpRequest<'a> =
    | New
    | Pending
    | Success of 'a
    | Error

type AuthQuery =
    { LinkCode: string
      HouseholdId: string }

// The model holds data that you want to keep track of while the application is running
// in this case, we are keeping track of a counter
// we mark it as optional, because initially it will not be available from the client
// the initial value will be requested from server
type Model = { LinkCode: LinkCode
               HouseholdId: string
               Username: string
               Password: string
               Authorized: bool }

// The Msg type defines what events/actions can occur while the application is running
// the state of the application changes *only* in reaction to these events
type Msg =
    | Authorize
    | EmailChanged of string
    | PasswordChanged of string
    | Authorized of string