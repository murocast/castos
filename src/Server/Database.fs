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
                         LinkCode: string
                         Created: System.DateTime }

    type DatabaseConnection = { AddAuthRequest: AuthRequest->unit
                                GetAuthRequestByLinkToken: string->AuthRequest option }

    let private addAuthRequest (db:LiteDatabase) (token:AuthRequest) =
        let collection = db.GetCollection<AuthRequest>("authrequests")
        collection.Insert(token)
        |> ignore

    let private getAuthRequest (db:LiteDatabase) linkCode =
        let collection = db.GetCollection<AuthRequest>("authrequests")
        let result = collection.Find (fun token -> token.LinkCode = linkCode)
                     |> List.ofSeq
        match result with
        | head::tail -> Some head
        | [] -> None

    let createDatabaseConnection() =
        let mapper = FSharpBsonMapper()
        let db = new LiteDatabase("murocast.db", mapper) //TODO: DB is IDisposable

        { AddAuthRequest = addAuthRequest db
          GetAuthRequestByLinkToken = getAuthRequest db }