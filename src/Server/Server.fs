open System.IO

open Giraffe.Serialization
open Microsoft.Extensions.DependencyInjection
open Saturn

open Castos.EventStore
open Castos
open Castos.SmapiCompositions
open Castos.SubscriptionCompositions

let publicPath = Path.GetFullPath "../Client/public"
let port = 80us

let eventStore = createGetEventStoreEventStore<CastosEventData, Error>(VersionConflict "Version conflict")

let webApp = router {
    forward "/api" (subscriptionsRouter eventStore)
    forward "/smapi" (smapiRouter eventStore)
}

let configureSerialization (services:IServiceCollection) =
    let fableJsonSettings = Newtonsoft.Json.JsonSerializerSettings()
    fableJsonSettings.Converters.Add(Fable.JsonConverter())
    services.AddSingleton<IJsonSerializer>(NewtonsoftJsonSerializer fableJsonSettings)

let app = application {
    url ("http://0.0.0.0:" + port.ToString() + "/")
    use_router webApp
    memory_cache
    use_static publicPath
    service_config configureSerialization
    use_gzip
}

run app
