module Server

open Giraffe.Serialization
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging

open Saturn
open System.IO

open Castos
open Castos.Auth
open Castos.Configuration
open Castos.SmapiCompositions
open Castos.FeedCompositions
open Castos.UserCompositions
open Castos.SubscriptionCompositions

open CosmoStore

let publicPath = Path.GetFullPath "../Client/public"
let port = 80us
[<Literal>]
let DataFolder = "Castos"

let appConfig =
    let builder =
        let path = DirectoryInfo(Directory.GetCurrentDirectory()).FullName
        printfn "Searching for configuration in %s" path
        ConfigurationBuilder()
            .SetBasePath(path)
            .AddJsonFile("appsettings.json", optional = true)
            .AddEnvironmentVariables()
            .Build()

    { Auth = {
        Secret = builder.["Auth:Secret"]
        Issuer = builder.["Auth:Issuer"]}}

let eventStore = { LiteDb.Configuration.Empty with
                        StoreType = LiteDb.LocalDB
                        Folder = DataFolder }
                  |> LiteDb.EventStore.getEventStore

let db = Database.createDatabaseConnection DataFolder

let webApp appConfig = router {
    post "/token" (handlePostToken appConfig.Auth (getUserComposition eventStore))
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
    use_jwt_authentication appConfig.Auth.Secret appConfig.Auth.Issuer
    url ("http://0.0.0.0:" + port.ToString() + "/")
    use_router (webApp appConfig)
    memory_cache
    use_static publicPath
    service_config configureSerialization
    use_cors "Cors Policy" (fun builder -> builder
                                            .AllowAnyMethod()
                                            .AllowAnyHeader()
                                            .WithOrigins [|"http://localhost:8080"; "http://127.0.0.1:8080"|]
                                           |> ignore )
    use_gzip
    logging (fun logger -> logger.SetMinimumLevel LogLevel.Information |> ignore)
}

[<EntryPoint>]
let main _ =
    run app
    0
