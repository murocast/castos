namespace Castos

[<RequireQualifiedAccess>]
module Database =
    open LiteDB
    open LiteDB.FSharp

    type AuthToken = { Id: System.Guid
                       HouseholdId: string
                       Token: string
                       PrivateKey: string
                       UserId: UserId
                       Created: System.DateTime }

    type AuthRequest = { Id: System.Guid
                         HouseholdId: string
                         LinkCode: System.Guid
                         UserId: UserId option
                         Created: System.DateTime
                         Used: System.DateTime option }

    type DatabaseConnection = { AddAuthRequest: AuthRequest->unit
                                GetAuthRequestByLinkToken: System.Guid -> string -> AuthRequest option
                                UpdateAuthRequest: AuthRequest -> bool
                                AddAuthToken: AuthToken -> unit
                                GetAuthToken: string -> string -> AuthToken option }

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

    let private getAuthToken (db:LiteDatabase) (token:string) (householdId:string) =
        let collection = db.GetCollection<AuthToken>(AuthTokenCollection)
        let result = collection.Find (fun authToken -> authToken.Token = token)
                     |> List.ofSeq
        match result with
        | head::tail -> Some head
        | [] -> None

    let createDatabaseConnection folderName =
        let folder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData)
        let filePath = System.IO.Path.Combine(folder, folderName, "database.db")
        printfn "Using database file: %s" filePath
        let mapper = FSharpBsonMapper()
        let db = new LiteDatabase(filePath, mapper) //TODO: DB is IDisposable

        { AddAuthRequest = addAuthRequest db
          GetAuthRequestByLinkToken = getAuthRequest db
          UpdateAuthRequest = updateAuthRequest db
          AddAuthToken = addAuthToken db
          GetAuthToken = getAuthToken db }