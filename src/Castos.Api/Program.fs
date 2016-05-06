open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful
open Suave.WebPart

let podcastRoutes =
    choose
        [ path "/api/podcasts" >=> choose [ GET >=> OK "TODO: All podcasts" ]

          pathScan "/api/podcasts/%s"
          <| fun podcast -> choose [ GET >=> OK(sprintf "TODO: Show information about podcast '%s'" podcast) ]

          pathScan "/api/podcasts/%s/episodes"
          <| fun podcast -> choose [ GET >=> OK(sprintf "TODO: Show episodes of '%s'" podcast) ]

          pathScan "/api/podcasts/%s/episodes/%s"
          <| fun (podcast, episode) ->
              choose [ GET >=> OK(sprintf "TODO: Show information about episode '%s' of podcast '%s'" podcast episode) ] ]

[<EntryPoint>]
let main argv =
    startWebServer defaultConfig (choose [ podcastRoutes ])
    0
