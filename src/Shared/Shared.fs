namespace Shared
open System
open Thoth.Json

type Counter = { Value : int }

type SmapiAuthRendition = { EMail: string
                            Password: string
                            LinkCode: Guid
                            HouseholdId: string }