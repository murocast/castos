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

let eventStore = createGetEventStoreEventStore<CastosEventData, Error>(VersionConflict "Version conflict")

let storeSubscriptionEvent (version, event) =
    let streamId event = StreamId (sprintf "subscription-%A" (Castos.SubscriptionSource.subscriptionId event))
    eventStore.SaveEvents (streamId event) version [event]

let addSubscriptionComposition form =
    //TODO: Validation
    let (rendition:AddSubscriptionRendition) = unjson form
    Castos.SubscriptionSource.addSubscription rendition
    |> storeSubscriptionEvent

let deleteSubscriptionComposition id =
    let result = eventStore.GetEvents (StreamId(sprintf "subscription-%s" id))
                 >>= Castos.SubscriptionSource.deleteSubscription
                 >>= storeSubscriptionEvent
    match result with
    | Success _ -> ok (sprintf "Deleted %s" id)
    | Failure m -> fail m


let getSubscriptionsComposition() =
    let result = eventStore.GetEvents (StreamId("$ce-subscription"))
    match result with
    | Success (_, events) -> ok (Castos.SubscriptionSource.getSubscriptions events)
    | _ -> failwith "bla"

let getSubscriptionComposition id =
    let result = eventStore.GetEvents (StreamId(sprintf "subscription-%s" id))
    match result with
    | Success (_, events) -> ok (Castos.SubscriptionSource.getSubscription events)
    | _ -> failwith "stream not found"

let addEpisodeComposition subscriptionId form =
    let (rendition:AddEpisodeRendition) = unjson form
    let result = eventStore.GetEvents (StreamId(sprintf "subscription-%s" (subscriptionId)))
                    >>= (Castos.SubscriptionSource.addEpisode subscriptionId rendition)
                    >>= storeSubscriptionEvent
    match result with
    | Success _ -> ok ("added episode")
    | Failure m -> fail m

let getCategoriesComposition() =
    let result = eventStore.GetEvents (StreamId("$ce-subscription"))
    match result with
    | Success (_, events) -> ok (Castos.SubscriptionSource.getCategories events)
    | _ -> failwith "bla"

let processSmapiMethod podcasts m =
    match m with
    | GetMetadata s -> processGetMetadata podcasts (GetMetadataRequest.Parse s)
    | GetMediaMetadata s -> processGetMediaMetadata podcasts (GetMediaMetadataRequest.Parse s)
    | GetLastUpdate s -> processGetLastUpdate (GetLastUpdateRequest.Parse s)
    | GetMediaURI s -> processGetMediaURI eventStore s (podcastFileBasePath())
    | ReportPlaySeconds _ ->
        processReportPlaySecondsRequest eventStore
        |> ignore
        ok("")
    | ReportPlayStatus _ -> ok("")
    | SetPlayedSeconds _ -> ok("")
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
    choose [ path "/smapi" >=> choose [POST >=> warbler (fun _ -> processSmapiRequest (getPodcasts() |> Seq.ofList))] ]

let subscriptionRoutes =
    choose [ path "/api/subscriptions"
                >=> choose [ GET >=> warbler ( fun _ -> processAsync getSubscriptionsComposition)
                             POST >=> warbler( fun _ ->  processFormAsync addSubscriptionComposition) ]
             path "/api/subscriptions/categories"
                >=> GET >=> warbler (fun _ -> processAsync getCategoriesComposition)
             pathScan "/api/subscriptions/%s/episodes/%i"
                <| fun (subscriptionId, episodeId) -> choose [ GET >=> OK (sprintf "Metadata of Episode %i of subscription %A" episodeId subscriptionId)]
             pathScan "/api/subscriptions/%s/episodes"
                <| fun id -> choose [ GET >=> OK (sprintf "List Episodes of suscription %A" id)
                                      POST >=> warbler (fun _ -> processFormAsync (addEpisodeComposition id)) ]
             pathScan "/api/subscriptions/%s"
                <| fun id -> choose [ GET >=> warbler ( fun _ -> processAsync (fun () -> getSubscriptionComposition id))
                                      DELETE >=> warbler (fun _ -> processAsync (fun () -> deleteSubscriptionComposition id)) ]]


[<EntryPoint>]
let main _ =
    let parser = ArgumentParser.Create<Arguments>(programName = "Castos.Api.exe")
    let results = parser.ParseConfiguration (ConfigurationReader.DefaultReader())
    baseurl <- results.GetResult  (<@ BaseUrl @>, defaultValue = baseurl)

    let cts = new CancellationTokenSource()

    let start _ =
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

        let _, server = startWebServerAsync cfg loggedWebApp
        Async.Start(server, cts.Token)
        true

    let stop _ =
        cts.Cancel()
        true

    //PreLoad Podcasts in Memory
    //GetPodcasts() |> ignore

    Service.Default
    |> service_name "Castos"
    |> with_start start
    |> with_recovery (ServiceRecovery.Default |> restart (min 10))
    |> with_stop stop
    |> run
