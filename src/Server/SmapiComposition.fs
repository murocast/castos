namespace Castos

open Giraffe
open Saturn
open FSharp.Control.Tasks.V2

open Castos
open Castos.Configuration
open Castos.Smapi
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging

module SmapiCompositions =
    let internal getSmapiMethod db (c:HttpContext) =
            task{
                let m = match c.GetRequestHeader "SOAPAction" with
                        | Error msg -> failwith msg
                        | Result.Ok headerValue -> extractSmapiMethod headerValue

                let! formString = Http.rawFormString c
                let userId = getUserFromHeader db formString

                return match m with
                       | "getMetadata" -> Ok (GetMetadata (formString, userId))
                       | "getMediaMetadata" -> Ok (GetMediaMetadata (formString, userId))
                       | "getMediaURI" -> Ok(GetMediaURI (formString, userId))
                       | "getLastUpdate" -> Ok(GetLastUpdate(formString, userId))
                       | "getExtendedMetadataRequest" -> Ok(GetExtendedMetadata(formString, userId))
                       | "getExtendedMetadataRequestText" -> Ok(GetExtendedMetadataText(formString, userId))
                       | "reportPlaySeconds" -> Ok(ReportPlaySeconds(formString, userId))
                       | "reportPlayStatus" -> Ok(ReportPlayStatus(formString, userId))
                       | "setPlayedSeconds" -> Ok(SetPlayedSeconds(formString, userId))
                       | "getAppLink" -> Ok(GetAppLink(formString, userId))
                       | "getDeviceAuthToken" -> Ok(GetDeviceAuthToken(formString, userId))
                       | _ -> Error(sprintf "Method not implemented %s" m)
            }

    let internal processSmapiMethod appConfig eventStore db m =
        match m with
        | GetMetadata (s, Some u) -> processGetMetadata eventStore u (GetMetadataRequest.Parse s)
        | GetMetadata (_, None) -> Error "User not found"
        | GetMediaMetadata (s,u) -> processGetMediaMetadata eventStore (GetMediaMetadataRequest.Parse s)
        | GetLastUpdate (s,u) -> processGetLastUpdate (GetLastUpdateRequest.Parse s)
        | GetMediaURI (s, Some u) -> processGetMediaURI eventStore s u
        | GetMediaURI (_, None) -> Error "user not found"
        | ReportPlaySeconds (s, Some u) ->
             processReportPlaySecondsRequest eventStore s u
             Ok("")
        | ReportPlaySeconds (_, None) -> Error "user not found"
        | ReportPlayStatus _ -> Ok("")
        | SetPlayedSeconds _ -> Ok("")
        | GetAppLink (s,u) -> processGetAppLink appConfig.ClientBaseUrl db s
        | GetDeviceAuthToken (s,u) -> processGetDeviceAuthTokenRequest db s
        | GetExtendedMetadata _ -> Error "not implemented"
        | GetExtendedMetadataText _ -> Error "not implemented"

    let internal smapiImp appConfig eventStore db (c:HttpContext) =
        task {
            let! result = getSmapiMethod db c
            let log (ctx:HttpContext) (m:SmapiMethod) =
                let j = mkjson m
                let logger = ctx.GetLogger()
                logger.LogDebug j
                Ok m

            return result
                    >>= log c
                    >>= (processSmapiMethod appConfig eventStore db)
        }

    let internal processSmapiRequest appConfig eventStore db =
        fun next ctx ->
            task {
                let! result = smapiImp appConfig eventStore db ctx
                let logger = ctx.GetLogger()
                return! match result with
                        | Ok (content) ->
                            logger.LogDebug content
                            text content next ctx
                        | Error (error) ->
                            logger.LogError (sprintf "Error Handling Request: %s" error)
                            RequestErrors.BAD_REQUEST error next ctx
            }

    let smapiRouter appConfig eventStore db = router {
        post "" (processSmapiRequest appConfig eventStore db)
    }