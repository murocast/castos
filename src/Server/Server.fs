module Server

open FSharp.Control.Tasks.V2.ContextInsensitive
open Giraffe
open Giraffe.Serialization
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.IdentityModel.Tokens
open Saturn
open System
open System.IdentityModel.Tokens.Jwt
open System.IO
open System.Security.Claims

open Castos.EventStore
open Castos
open Castos.SmapiCompositions
open Castos.SubscriptionCompositions
open Castos.Http

let secret = "spadR2dre#u-ruBrE@TepA&*Uf@U"
let issuer = "saturnframework.io"

let publicPath = Path.GetFullPath "../Client/public"
let port = 80us

let eventStore = createGetEventStoreEventStore<CastosEventData, Error>(VersionConflict "Version conflict")

[<CLIMutable>]
type LoginViewModel =
    {
        Email : string
        Password : string
    }

[<CLIMutable>]
type TokenResult =
    {
        Token : string
    }

type User = {
    Id : UserId
    Email: string
    Password: string
    //TODO: Hash and salt
}

type AddUserRendition =
    {
        EMail: string
        Password: string
    }

let generateToken email =
    let claims = [|
        Claim(JwtRegisteredClaimNames.Sub, email);
        Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) |]
    claims
    |> Auth.generateJWT (secret, SecurityAlgorithms.HmacSha256) issuer (DateTime.UtcNow.AddHours(1.0))

let handlePostToken =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let! model = ctx.BindJsonAsync<LoginViewModel>()

            // authenticate user

            let token = generateToken model.Email

            return! json {Token = token} next ctx
}

let private storeUsersEvent eventStore (version, event) =
    let streamId event = StreamId (sprintf "users")
    eventStore.SaveEvents (streamId event) version [event]

let apply state event =
    match event with
    | UserAdded data -> { Id = data.Id
                          Email = data.Email
                          Password = data.Password } :: state
    | _ -> failwith "unkown event"

let evolve state events =
    events
    |> List.fold apply state
let getUsersComposition eventstore =
    let result = eventStore.GetEvents (StreamId("users"))
    match result with
    | Success (_, events) -> ok (evolve [] events)
    | _ -> failwith "bla"


let addUserComposition eventStore rendition =
    let result = (StreamVersion 0, UserAdded {
                    Id = Guid.NewGuid() |> UserId
                    Email = rendition.Email
                    Password = rendition.Password
                    //TODO: Hash and Salt
                 }) |> storeUsersEvent eventStore
    match result with
    | Success _ -> ok ("added user")
    | Failure m -> fail m

let usersRouter eventStore = router {
    get "" (processAsync getUsersComposition eventStore)
    post "" (processDataAsync addUserComposition eventStore)
}

let webApp = router {
    post "/token" handlePostToken

    forward "/api/users" (usersRouter eventStore)
    forward "/api/subscriptions" (subscriptionsRouter eventStore)
    forward "/smapi" (smapiRouter eventStore)
}

let configureSerialization (services:IServiceCollection) =
    let fableJsonSettings = Newtonsoft.Json.JsonSerializerSettings()
    fableJsonSettings.Converters.Add(Fable.JsonConverter())
    services.AddSingleton<IJsonSerializer>(NewtonsoftJsonSerializer fableJsonSettings)

let app = application {
    use_jwt_authentication secret issuer
    url ("http://0.0.0.0:" + port.ToString() + "/")
    use_router webApp
    memory_cache
    use_static publicPath
    service_config configureSerialization
    use_gzip
}

[<EntryPoint>]
let main _ =
    run app
    0
