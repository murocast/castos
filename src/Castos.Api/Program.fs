open System.Net

open Newtonsoft.Json
open Newtonsoft.Json.FSharp

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
open Castos.Players

open Castos.Smapi

open Chessie.ErrorHandling

let settings = new JsonSerializerSettings()
               |> Serialisation.extend

let inline unjson<'T> json =
        let a = JsonConvert.DeserializeObject<'T>(json, settings)
        a

let inline mkjson a =
        let json = JsonConvert.SerializeObject(a, settings)
        json

let rawFormString x = System.Text.Encoding.UTF8.GetString x.request.rawForm

let processFormAsync f =
    fun context ->
        async{
            let data = rawFormString context
            let result = f data
            return! match result with
                    | Ok (a, _) -> OK (mkjson a) context
                    | Bad (_) -> BAD_REQUEST "Error" context
        }

let processAsync f =
    fun context ->
        async{
            let result = f()
            return! match result with
                    | Ok (a, _) -> OK (mkjson a) context
                    | Bad (_) -> BAD_REQUEST "Error" context
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
        | "getExtendedMetadataRequest" -> ok(GetExtendedMetadataRequest(rawFormString c))
        | "getExtendedMetadataRequestText" -> ok(GetExtendedMetadataRequestText(rawFormString c))
        | _ -> fail(sprintf "Method not implemented %s" m)

let processSmapiMethod m =
    let podcasts = GetPodcasts()
    match m with
    | GetMetadata s -> processGetMetadata podcasts (getMetadataRequest.Parse s)
    | GetLastUpdate s -> processGetLastUpdate (getLastUpdateRequest.Parse s)
    | GetMediaMetadata s -> processGetMediaMetadata (getMediaMetadataRequest.Parse s)
    | _ -> fail "blubber"

let smapiImp c =
    getSmapiMethod c
     >>= processSmapiMethod

let toLines (strings) =
    List.fold (+) "" strings

let processSmapiRequest =
    fun context ->
        async{
            let result = smapiImp context
            return! match result with
                    | Ok (content, _) -> OK content context
                    | Bad (content) -> BAD_REQUEST (toLines content) context
        }

let podcastRoutes =
    choose
        [ path "/api/podcasts" >=> choose [ GET >=> warbler (fun context -> processAsync (fun() -> ok GetPodcasts)) ]

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

let playerRoutes =
    choose
        [ path "/api/players" >=> choose [GET >=> warbler (fun c -> processFormAsync GetPlayers) ]

          pathScan "/api/players/%s"
          <| fun player -> choose [ GET >=> OK(sprintf "TODO: Show information about player %s" player) ] ]

let smapiRoutes =
    choose [ path "/smapi" >=> choose [POST >=> warbler (fun c -> processSmapiRequest)] ]


[<EntryPoint>]
let main argv =
    let logger = Targets.create Debug [||]
    let loggedWebApp context = async {
        logger.debug (
            eventX "Received request {method} {url} {form}"
                >> setField "method" context.request.``method``
                >> setField "url" context.request.url
                >> setField "form" (rawFormString context))
        let! response = (choose [ podcastRoutes; playerRoutes; smapiRoutes ]) context
        match response with
        | Some context ->
            match context.response.content with
            | Bytes c -> logger.debug (eventX "Send response {form}"
                            >> setField "form" (System.Text.Encoding.UTF8.GetString c))
            | _ -> ()
        | _ -> ()
        return response }

    let cfg =
        { defaultConfig with
            bindings = [ HttpBinding.create HTTP IPAddress.Any 80us ]
            logger = logger
        }
    startWebServer cfg loggedWebApp
    0
