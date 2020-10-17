module Murocast.Client.Pages.LinkSonos.Domain

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

type Model = { LinkCode: LinkCode
               HouseholdId: string
               Username: string
               Password: string
               Authorized: bool }

type Msg =
    | Authorize
    | EmailChanged of string
    | PasswordChanged of string
    | Authorized of string