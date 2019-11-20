module Server

open Giraffe.Serialization
open Microsoft.Extensions.DependencyInjection

open Saturn
open System.IO

open Castos.EventStore
open Castos
open Castos.Auth
open Castos.SmapiCompositions
open Castos.FeedCompositions
open Castos.UserCompositions
open Castos.SubscriptionCompositions

let publicPath = Path.GetFullPath "../Client/public"
let port = 80us

let eventStore = createGetEventStoreEventStore<CastosEventData, Error>(VersionConflict "Version conflict")
let db = Database.createDatabaseConnection()

let webApp = router {

    post "/token" (handlePostToken (getUserComposition eventStore))

    forward "/api/users" (usersRouter eventStore db)
    forward "/api/feeds" (feedsRouter eventStore)
    forward "/api/subscriptions" (subscriptionsRouter eventStore)
    forward "/smapi" (smapiRouter eventStore db)
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
    use_cors "Cors Policy" (fun builder -> builder
                                            .AllowAnyMethod()
                                            .AllowAnyHeader()
                                            .WithOrigins [|"http://localhost:8080"; "http://127.0.0.1:8080"|]
                                           |> ignore )
    use_gzip
}

[<EntryPoint>]
let main _ =
    run app
    0
