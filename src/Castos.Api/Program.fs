open Newtonsoft.Json
open Newtonsoft.Json.FSharp

open Suave
open Suave.Filters
open Suave.Operators
open Suave.RequestErrors
open Suave.Successful
open Suave.WebPart

open Castos.Podcasts
open Castos.Players

let settings = new JsonSerializerSettings()
               |> Serialisation.extend

let inline unjson<'T> json =
        let a = JsonConvert.DeserializeObject<'T>(json, settings)
        a

let inline mkjson a =
        let json = JsonConvert.SerializeObject(a, settings)
        json

let processAsync f =
    fun (x:HttpContext) ->
        async{
            let data = System.Text.Encoding.UTF8.GetString x.request.rawForm
            let result = f data
            return! match result with
                    | Success (a) -> OK (mkjson a) x
                    | Failure (_) -> BAD_REQUEST "Error" x
        }

let processSmapiMethod a =
    match a with
    | "getMetadata" -> Success(a)
    | _ -> Failure(sprintf "Method not implemented %s" a)

let extractSmapiMethod (m:string) =
    m.[34..] //cut until #: http://www.sonos.com/Services/1.1#getMetadata

let processSmapiRequest =
    fun (c:HttpContext) ->
        async{
            let result = match "SOAPAction" |> c.request.header with
                         | Choice1Of2 m -> processSmapiMethod (extractSmapiMethod m)
                         | Choice2Of2 e -> Failure (e)

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
    choose
        [ path "/smapi" >=> choose [POST >=> warbler (fun c -> processSmapiRequest)]
//          path "/getExtendedMetadataText" >=> choose [GET >=> OK "TODO: Smapi"]
//          path "/getLastUpdate" >=> choose [GET >=> OK "TODO: Smapi"]
//          path "/getMediaMetadata" >=> choose [GET >=> OK "TODO: Smapi"]
//          path "/getMediaURI" >=> choose [GET >=> OK "TODO: Smapi"]
//          path "/getMetadata" >=> choose [POST >=> OK "TODO: Smapi"]
//          path "/search" >=> choose [GET >=> OK "TODO: Smapi"]
          ]

[<EntryPoint>]
let main argv =
    startWebServer defaultConfig (choose [ podcastRoutes; playerRoutes; smapiRoutes ])
    0
