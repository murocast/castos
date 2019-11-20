namespace Shared
open System

type Counter = { Value : int }

type SmapiAuthRendition = { EMail: string
                            Password: string
                            LinkCode: Guid
                            HouseholdId: string }