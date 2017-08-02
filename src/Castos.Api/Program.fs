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
open Castos.Podcasts
open Castos.Events

open Castos.Smapi

open Argu

open Topshelf
open Time

type Arguments =
    | BaseUrl of string
with
    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | BaseUrl _ -> "Url smapi listens on"

let rawFormString x = System.Text.Encoding.UTF8.GetString x.request.rawForm

let mutable baseurl = "http://127.0.0.1"
let podcastFileBasePath() = baseurl + "/play/"

let processFormAsync f =
    fun context ->
        async{
            let data = rawFormString context
            let result = f data
            return! match result with
                    | Success (a) -> OK (mkjson a) context
                    | Failure (_) -> BAD_REQUEST "Error" context
        }

let processAsync f =
    fun context ->
        async{
            let result = f()
            return! match result with
                    | Success (a) -> OK (mkjson a) context
                    | Failure (_) -> BAD_REQUEST "Error" context
        }

let getSmapiMethod c =
        let m = match "SOAPAction" |> c.request.header with
                | Choice1Of2 m -> extractSmapiMethod m
                | Choice2Of2 e -> failwith "No method in request"

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

let eventStore = createGetEventStoreEventStore<CastosEventData, Error>(VersionConflict "Version conflict")

let storeSubscriptionEvent version event =
    let streamId event = StreamId (sprintf "subscription-%A" (Castos.SubscriptionSource.subscriptionId event))
    eventStore.SaveEvents (streamId event) version [event]

let addSubscriptionComposition name url =
    Castos.SubscriptionSource.addSubscription name url
    |> storeSubscriptionEvent (StreamVersion 0)

let getSubscriptionsComposition() =
    let result = eventStore.GetEvents (StreamId("$ce-subscription"))
    match result with
    | Success (streamVersion, events) -> ok (Castos.SubscriptionSource.getSubscriptions events)
    | _ -> failwith "bla"

let processSmapiMethod podcasts m =
    match m with
    | GetMetadata s -> processGetMetadata podcasts (GetMetadataRequest.Parse s)
    | GetMediaMetadata s -> processGetMediaMetadata podcasts (GetMediaMetadataRequest.Parse s)
    | GetLastUpdate s -> processGetLastUpdate (GetLastUpdateRequest.Parse s)
    | GetMediaURI s -> processGetMediaURI eventStore s (podcastFileBasePath())
    | ReportPlaySeconds s ->
        processReportPlaySecondsRequest eventStore
        |> ignore
        ok("")
    | ReportPlayStatus s -> ok("")
    | SetPlayedSeconds s -> ok("")
    | _ -> fail "blubber"

let smapiImp c podcasts =
    getSmapiMethod c
     >>= (processSmapiMethod podcasts)

let toLines (strings) =
    List.fold (+) "" strings

let processSmapiRequest podcasts=
    fun context ->
        async{
            let result = smapiImp context podcasts
            return! match result with
                    | Success (content) -> OK content context
                    | Failure (content) -> BAD_REQUEST (content) context
        }

let playRoutes =
    choose
        [ pathScan "/play/%s"
          <| fun id -> choose [GET >=> Files.file (Podcasts.GetPathFromId id) ]]

let smapiRoutes getPodcasts =
    choose [ path "/smapi" >=> choose [POST >=> warbler (fun c -> processSmapiRequest (getPodcasts() |> Seq.ofList))] ]

let subscriptionRoutes =
    choose [ path "/api/subscriptions"
                    >=> choose [ GET >=> warbler ( fun context -> processAsync (fun() -> getSubscriptionsComposition()))
                                 POST >=> OK "OK" ] ]


[<EntryPoint>]
let main argv =
    let parser = ArgumentParser.Create<Arguments>(programName = "Castos.Api.exe")
    let results = parser.ParseConfiguration (ConfigurationReader.DefaultReader())
    baseurl <- results.GetResult  (<@ BaseUrl @>, defaultValue = baseurl)

    let cts = new CancellationTokenSource()

    let start hc =
        let logger = Targets.create Debug [||]
        let loggedWebApp context = async {
            logger.debug (
                eventX "Received request {method} {url} {form}"
                    >> setField "method" context.request.``method``
                    >> setField "url" context.request.url
                    >> setField "form" (rawFormString context))
            let! response = (choose [ playRoutes; smapiRoutes GetPodcasts; subscriptionRoutes ]) context
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

        let listening, server = startWebServerAsync cfg loggedWebApp
        Async.Start(server, cts.Token)
        true

    let stop hc =
        cts.Cancel()
        true

    //PreLoad Podcasts in Memory
    GetPodcasts() |> ignore

    Service.Default
    |> service_name "Castos"
    |> with_start start
    |> with_recovery (ServiceRecovery.Default |> restart (min 10))
    |> with_stop stop
    |> run
