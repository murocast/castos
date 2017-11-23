open System.Net
open System.Threading

open Suave
open Suave.Filters
open Suave.Logging
open Suave.Logging.Message
open Suave.Operators
open Suave.RequestErrors
open Suave.Successful
open Suave.WebPart
open Suave.SuaveConfig

open Castos
open Castos.Smapi
open Castos.SubscriptionCompositions

open Argu

open Topshelf
open Time

let rawFormString x = System.Text.Encoding.UTF8.GetString x.request.rawForm

let eventStore = createGetEventStoreEventStore<CastosEventData, Error>(VersionConflict "Version conflict")

let getSmapiMethod c =
        let m = match "SOAPAction" |> c.request.header with
                | Choice1Of2 m -> extractSmapiMethod m
                | Choice2Of2 _ -> failwith "No method in request"

        match m with
        | "getMetadata" -> ok (GetMetadata (rawFormString c))
        | "getMediaMetadata" -> ok (GetMediaMetadata (rawFormString c))
        | "getMediaURI" -> ok(GetMediaURI (rawFormString c))
        | "getLastUpdate" -> ok(GetLastUpdate(rawFormString c))
        | "getExtendedMetadataRequest" -> ok(GetExtendedMetadata(rawFormString c))
        | "getExtendedMetadataRequestText" -> ok(GetExtendedMetadataText(rawFormString c))
        | "reportPlaySeconds" -> ok(ReportPlaySeconds(rawFormString c))
        | "reportPlayStatus" -> ok(ReportPlayStatus(rawFormString c))
        | "setPlayedSeconds" -> ok(SetPlayedSeconds(rawFormString c))
        | _ -> fail(sprintf "Method not implemented %s" m)

let processSmapiMethod m =
    match m with
    | GetMetadata s -> processGetMetadata eventStore (GetMetadataRequest.Parse s)
    | GetMediaMetadata s -> processGetMediaMetadata eventStore (GetMediaMetadataRequest.Parse s)
    | GetLastUpdate s -> processGetLastUpdate (GetLastUpdateRequest.Parse s)
    | GetMediaURI s -> processGetMediaURI eventStore s
    | ReportPlaySeconds _ ->
        processReportPlaySecondsRequest eventStore
        |> ignore
        ok("")
    | ReportPlayStatus _ -> ok("")
    | SetPlayedSeconds _ -> ok("")
    | _ -> fail "blubber"

let smapiImp c =
    getSmapiMethod c
     >>= (processSmapiMethod)

let toLines (strings) =
    List.fold (+) "" strings

let processSmapiRequest() =
    fun context ->
        async{
            let result = smapiImp context
            return! match result with
                    | Success (content) -> OK content context
                    | Failure (content) -> BAD_REQUEST (content) context
        }

let smapiRoutes =
    choose [ path "/smapi" >=> choose [POST >=> warbler (fun _ -> processSmapiRequest()) ] ]


[<EntryPoint>]
let main _ =
    let cts = new CancellationTokenSource()

    let start _ =
        let logger = Targets.create Debug [||]
        let loggedWebApp context = async {
            logger.debug (
                eventX "Received request {method} {url} {form}"
                    >> setField "method" context.request.``method``
                    >> setField "url" context.request.url
                    >> setField "form" (rawFormString context))
            let! response = (choose [ smapiRoutes; subscriptionRoutes eventStore ]) context
            match response with
            | Some context ->
                match context.response.content with
                | Bytes c -> logger.debug (eventX "Send response {form}"
                                >> setField "form" (System.Text.Encoding.UTF8.GetString c))
                | _ -> ()
            | _ -> ()
            return response }
        let mimeTypes =
            Writers.defaultMimeTypesMap
                @@ (function | ".mp3" -> Writers.createMimeType "audio/mp3" false | _ -> None)
        let cfg =
            { defaultConfig with
                bindings = [ HttpBinding.create HTTP IPAddress.Any 80us ]
                logger = logger
                mimeTypesMap = mimeTypes
                cancellationToken = cts.Token
            }

        let _, server = startWebServerAsync cfg loggedWebApp
        Async.Start(server, cts.Token)
        true

    let stop _ =
        cts.Cancel()
        true

    Service.Default
    |> service_name "Castos"
    |> with_start start
    |> with_recovery (ServiceRecovery.Default |> restart (min 10))
    |> with_stop stop
    |> run
