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

let playEpisodeEvents = createGetEventStoreEventStore<EpisodeEventData, Error>(VersionConflict "Version conflict")
let subscriptionEvents = createGetEventStoreEventStore<SubscriptionEventData, Error>(VersionConflict "Version conflict")

let storeSubscriptionEvent version event =
    let subscriptionId = function
                         | SubscriptionAdded s -> s.Id
                         | SubscriptionDeleted s -> s.Id
    let streamId event = StreamId (sprintf "subscription-%A" (subscriptionId event))
    subscriptionEvents.SaveEvents (streamId event) version [event]

let addSubscriptionComposition name url =   
    Castos.SubscriptionSource.addSubscription name url
    |> storeSubscriptionEvent (StreamVersion 0)

let processSmapiMethod podcasts m =
    match m with
    | GetMetadata s -> processGetMetadata podcasts (GetMetadataRequest.Parse s)
    | GetMediaMetadata s -> processGetMediaMetadata podcasts (GetMediaMetadataRequest.Parse s)
    | GetLastUpdate s -> processGetLastUpdate (GetLastUpdateRequest.Parse s)
    | GetMediaURI s -> processGetMediaURI playEpisodeEvents s (podcastFileBasePath())
    | ReportPlaySeconds s ->
        processReportPlaySecondsRequest playEpisodeEvents s
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

let podcastRoutes =
    choose
        [ path "/api/podcasts" >=> choose [ GET >=> warbler (fun context -> processAsync (fun() -> Castos.ErrorHandling.ok GetPodcasts)) ]

          path "/api/podcasts/categories/" >=> choose [ GET >=> OK "TODO: All categories" ]

          pathScan "/api/podcasts/categories/%s"
          <| fun category -> choose [ GET >=> OK(sprintf "TODO: Show all podcasts of category '%s'" category) ]

          pathScan "/api/podcasts/%s"
          <| fun podcast -> choose [ GET >=> OK(sprintf "TODO: Show information about podcast '%s'" podcast) ]

          pathScan "/api/podcasts/%s/episodes"
          <| fun podcast -> choose [ GET >=> OK(sprintf "TODO: Show episodes of '%s'" podcast) ]

          pathScan "/api/podcasts/%s/episodes/%s"
          <| fun (podcast, episode) ->
              choose [ GET >=> OK(sprintf "TODO: Show information about episode '%s' of podcast '%s'" podcast episode) ] ]
let playRoutes =
    choose
        [ pathScan "/play/%s"
          <| fun id -> choose [GET >=> Files.file (Podcasts.GetPathFromId id) ]]

let smapiRoutes getPodcasts =
    choose [ path "/smapi" >=> choose [POST >=> warbler (fun c -> processSmapiRequest (getPodcasts() |> Seq.ofList))] ]




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
            let! response = (choose [ podcastRoutes; playRoutes; smapiRoutes GetPodcasts ]) context
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
