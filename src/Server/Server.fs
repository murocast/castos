open System.IO
open System.Threading.Tasks

open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open FSharp.Control.Tasks.V2
open Giraffe
open Saturn
open Shared

open Giraffe.Serialization
open Castos.EventStore
open Castos
open Castos.Http
open Castos.SubscriptionCompositions
open Castos.Smapi
open Microsoft.AspNetCore.Http

let publicPath = Path.GetFullPath "../Client/public"
let port = 80us

let eventStore = createGetEventStoreEventStore<CastosEventData, Error>(VersionConflict "Version conflict")

let getSmapiMethod (c:HttpContext) =
        task{
            let m = match c.GetRequestHeader "SOAPAction" with
                    | Error msg -> failwith msg
                    | Result.Ok headerValue -> extractSmapiMethod headerValue

            let! formString = rawFormString c
            return match m with
                   | "getMetadata" -> ok (GetMetadata (formString))
                   | "getMediaMetadata" -> ok (GetMediaMetadata (formString))
                   | "getMediaURI" -> ok(GetMediaURI (formString))
                   | "getLastUpdate" -> ok(GetLastUpdate(formString))
                   | "getExtendedMetadataRequest" -> ok(GetExtendedMetadata(formString))
                   | "getExtendedMetadataRequestText" -> ok(GetExtendedMetadataText(formString))
                   | "reportPlaySeconds" -> ok(ReportPlaySeconds(formString))
                   | "reportPlayStatus" -> ok(ReportPlayStatus(formString))
                   | "setPlayedSeconds" -> ok(SetPlayedSeconds(formString))
                   | _ -> fail(sprintf "Method not implemented %s" m)
        }

let processSmapiMethod m =
    match m with
    | GetMetadata s -> processGetMetadata eventStore (GetMetadataRequest.Parse s)
    | GetMediaMetadata s -> processGetMediaMetadata eventStore (GetMediaMetadataRequest.Parse s)
    | GetLastUpdate s -> processGetLastUpdate (GetLastUpdateRequest.Parse s)
    | GetMediaURI s -> processGetMediaURI eventStore s
    | ReportPlaySeconds s ->
         processReportPlaySecondsRequest eventStore s
         ok("")
    | ReportPlayStatus _ -> ok("")
    | SetPlayedSeconds _ -> ok("")
    | _ -> fail "blubber"


let smapiImp c =
    task{
        let! result = getSmapiMethod c
        return result >>= (processSmapiMethod)
    }


let toLines (strings) =
    List.fold (+) "" strings

let processSmapiRequest()=
    fun next ctx ->
        task {
            let! result = smapiImp ctx
            return! match result with
                    | Success (content) -> text content next ctx
                    | Failure (_) -> RequestErrors.BAD_REQUEST "Error" next ctx
        }

let webApp = router {
    forward "/api" (subscriptionsRouter eventStore)
    post "/smapi" (processSmapiRequest())
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
