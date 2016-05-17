[<AutoOpen>]
module Rop
    type Result<'TSuccess, 'TFailure> =
        | Success of 'TSuccess
        | Failure of 'TFailure

    let bind f x =
        match x with
        | Success s -> f s
        | Failure f -> Failure f

    let map f x =
        match x with
        | Success s -> Success(f s)
        | Failure f -> Failure f