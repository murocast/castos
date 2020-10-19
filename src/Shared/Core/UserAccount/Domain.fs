module Murocast.Shared.Core.UserAccount.Domain

open System

module Queries =
    type AuthenticatedUser = {
        Id : Guid
        Email: string
        Roles: string list
    }
    and UserId = System.Guid