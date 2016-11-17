open System.Net

open Newtonsoft.Json
open Newtonsoft.Json.FSharp

open Suave
open Suave.Filters
open Suave.Operators
open Suave.RequestErrors
open Suave.Successful
open Suave.WebPart
open Suave.SuaveConfig

open Castos
open Castos.Podcasts
open Castos.Players

open Castos.Smapi

let settings = new JsonSerializerSettings()
               |> Serialisation.extend

let inline unjson<'T> json =
        let a = JsonConvert.DeserializeObject<'T>(json, settings)
        a

let inline mkjson a =
        let json = JsonConvert.SerializeObject(a, settings)
        json

let rawFormString x = System.Text.Encoding.UTF8.GetString x.request.rawForm

let processAsync f =
    fun (c:HttpContext) ->
        async{
            let data = rawFormString c
            let result = f data
            return! match result with
                    | Success (a) -> OK (mkjson a) c
                    | Failure (_) -> BAD_REQUEST "Error" c
        }

let getSmapiMethod c =
        let m = match "SOAPAction" |> c.request.header with
                | Choice1Of2 m -> extractSmapiMethod m
                | Choice2Of2 e -> failwith "No method in request"

        match m with
        | "getMetadata" -> Success (GetMetadata (rawFormString c))
        | "getMediaMetadata" -> Success (GetMediaMetadata (rawFormString c))
        | "getMediaURI" -> Success(GetMediaURI (rawFormString c))
        | "getLastUpdate" -> Success(GetLastUpdate(rawFormString c))
        | "getExtendedMetadataRequest" -> Success(GetExtendedMetadataRequest(rawFormString c))
        | "getExtendedMetadataRequestText" -> Success(GetExtendedMetadataRequestText(rawFormString c))
        | _ -> Failure(sprintf "Method not implemented %s" m)

let smapiImp =
    getSmapiMethod
    >> Rop.bind processSmapiMethod

let processSmapiRequest =
    fun (c:HttpContext) ->
        async{
            let result = smapiImp c
            return! match result with
                    | Success (content) -> OK content c
                    | Failure (content) -> BAD_REQUEST content c
        }

let podcastRoutes =
    choose
        [ path "/api/podcasts" >=> choose [ GET >=> warbler (fun c -> processAsync GetPodcasts) ]

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
        [ path "/api/players" >=> choose [GET >=> warbler (fun c -> processAsync GetPlayers) ]

          pathScan "/api/players/%s"
          <| fun player -> choose [ GET >=> OK(sprintf "TODO: Show information about player %s" player) ] ]

let smapiRoutes =
    choose [ path "/smapi" >=> choose [POST >=> warbler (fun c -> processSmapiRequest)] ]

[<EntryPoint>]
let main argv =
    let cfg =
        { defaultConfig with
            bindings = [ HttpBinding.mk HTTP IPAddress.Any 80us ]
            logger = Logging.Loggers.saneDefaultsFor Logging.LogLevel.Verbose
        }
    startWebServer cfg (choose [ podcastRoutes; playerRoutes; smapiRoutes ])
    0
