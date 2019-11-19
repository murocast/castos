namespace Castos

[<RequireQualifiedAccess>]
module Database =
    open LiteDB
    open LiteDB.FSharp

    type AuthToken = { Id: System.Guid
                       HouseholdId: string
                       Token: string
                       PrivateKey: string
                       UserId: int
                       Created: System.DateTime }

    type AuthRequest = { Id: System.Guid
                         HouseholdId: string
                         LinkCode: System.Guid
                         UserId: int option
                         Created: System.DateTime
                         Used: System.DateTime option }

    type DatabaseConnection = { AddAuthRequest: AuthRequest->unit
                                GetAuthRequestByLinkToken: System.Guid -> string -> AuthRequest option
                                UpdateAuthRequest: AuthRequest -> bool

                                AddAuthToken: AuthToken -> unit }

    [<Literal>]
    let private AuthRequestCollection = "authrequests"
    [<Literal>]
    let private AuthTokenCollection = "authtoken"

    let private addAuthRequest (db:LiteDatabase) (token:AuthRequest) =
        let collection = db.GetCollection<AuthRequest>(AuthRequestCollection)
        collection.Insert(token)
        |> ignore

    let private getAuthRequest (db:LiteDatabase) linkCode householdId =
        let collection = db.GetCollection<AuthRequest>(AuthRequestCollection)
        //TODO: check timestamp
        let result = collection.Find (fun token -> token.LinkCode = linkCode && token.HouseholdId = householdId)
                     |> List.ofSeq
        match result with
        | head::tail -> Some head
        | [] -> None

    let private updateAuthRequest (db:LiteDatabase) (token:AuthRequest) =
        let collection = db.GetCollection<AuthRequest>(AuthRequestCollection)
        collection.Update token

    let private addAuthToken (db:LiteDatabase) (token:AuthToken) =
        let collection = db.GetCollection<AuthToken>(AuthTokenCollection)
        collection.Insert token
        |> ignore

    let createDatabaseConnection() =
        let mapper = FSharpBsonMapper()
        let db = new LiteDatabase("murocast.db", mapper) //TODO: DB is IDisposable

        { AddAuthRequest = addAuthRequest db
          GetAuthRequestByLinkToken = getAuthRequest db
          UpdateAuthRequest = updateAuthRequest db
          AddAuthToken = addAuthToken db }