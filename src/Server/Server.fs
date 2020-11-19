module Server

open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging

open Saturn
open Giraffe
open System.IO

open Castos
open Castos.Auth
open Castos.Configuration
open Castos.SmapiCompositions
open Castos.FeedCompositions
open Castos.UserCompositions
open Castos.SubscriptionCompositions

open CosmoStore


[<Literal>]
let DataFolder = "Castos"

let appConfig =
    let builder =
        let path = DirectoryInfo(Directory.GetCurrentDirectory()).FullName
        printfn "Searching for configuration in %s" path
        ConfigurationBuilder()
            .SetBasePath(path)
            .AddJsonFile("appsettings.json", optional = false)
            .AddEnvironmentVariables()
            .Build()

    let parsedLevel = builder.["LogLevel"]
                      |> Option.ofObj
                      |> Option.map (fun str ->
                                           let parsed, (value:LogLevel) = LogLevel.TryParse(str)
                                           if parsed then Some value else None)
                      |> Option.flatten
    { Auth = {
        Secret = builder.["Auth:Secret"]
        Issuer = builder.["Auth:Issuer"] }
      CorsUrls = builder.["CorsUrls"].Split([|';'|], System.StringSplitOptions.RemoveEmptyEntries)
      Url = builder.["Url"]
      ClientBaseUrl = builder.["ClientBaseUrl"]
      Port = System.UInt16.Parse builder.["Port"]
      LogLevel = Option.defaultValue LogLevel.Information parsedLevel }

let eventStore = { LiteDb.Configuration.Empty with
                        StoreType = LiteDb.LocalDB
                        Folder = DataFolder }
                  |> LiteDb.EventStore.getEventStore

let db = Database.createDatabaseConnection DataFolder

let webApp appConfig = router {
    post "/token" (handlePostToken appConfig.Auth (getUserComposition eventStore))
    post "/refreshtoken" (authorize >=> (handleRefreshToken appConfig.Auth))
    forward "/api/users" (usersRouter eventStore db)
    forward "/api/feeds" (feedsRouter eventStore)
    forward "/api/subscriptions" (subscriptionsRouter eventStore)
    forward "/smapi" (smapiRouter appConfig eventStore db)
}

let app = application {
    use_jwt_authentication appConfig.Auth.Secret appConfig.Auth.Issuer
    url  (sprintf "http://%s:%i/" appConfig.Url appConfig.Port)
    use_router (webApp appConfig)
    memory_cache
    use_static "public"
    use_json_serializer(Thoth.Json.Giraffe.ThothSerializer(caseStrategy=Thoth.Json.Net.CamelCase))
    use_cors "Cors Policy" (fun builder -> builder
                                            .AllowAnyMethod()
                                            .AllowAnyHeader()
                                            .WithOrigins appConfig.CorsUrls
                                           |> ignore )
    use_gzip
    logging (fun logger -> logger.SetMinimumLevel appConfig.LogLevel |> ignore)
}

[<EntryPoint>]
let main _ =
    run app
    0
