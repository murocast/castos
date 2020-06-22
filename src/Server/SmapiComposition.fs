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
                       | "getMetadata" -> ok (GetMetadata (formString, userId))
                       | "getMediaMetadata" -> ok (GetMediaMetadata (formString, userId))
                       | "getMediaURI" -> ok(GetMediaURI (formString, userId))
                       | "getLastUpdate" -> ok(GetLastUpdate(formString, userId))
                       | "getExtendedMetadataRequest" -> ok(GetExtendedMetadata(formString, userId))
                       | "getExtendedMetadataRequestText" -> ok(GetExtendedMetadataText(formString, userId))
                       | "reportPlaySeconds" -> ok(ReportPlaySeconds(formString, userId))
                       | "reportPlayStatus" -> ok(ReportPlayStatus(formString, userId))
                       | "setPlayedSeconds" -> ok(SetPlayedSeconds(formString, userId))
                       | "getAppLink" -> ok(GetAppLink(formString, userId))
                       | "getDeviceAuthToken" -> ok(GetDeviceAuthToken(formString, userId))
                       | _ -> fail(sprintf "Method not implemented %s" m)
            }

    let internal processSmapiMethod appConfig eventStore db m =
        match m with
        | GetMetadata (s, Some u) -> processGetMetadata eventStore u (GetMetadataRequest.Parse s)
        | GetMetadata (_, None) -> fail "User not found"
        | GetMediaMetadata (s,u) -> processGetMediaMetadata eventStore (GetMediaMetadataRequest.Parse s)
        | GetLastUpdate (s,u) -> processGetLastUpdate (GetLastUpdateRequest.Parse s)
        | GetMediaURI (s, Some u) -> processGetMediaURI eventStore s u
        | GetMediaURI (_, None) -> fail "user not found"
        | ReportPlaySeconds (s, Some u) ->
             processReportPlaySecondsRequest eventStore s u
             ok("")
        | ReportPlaySeconds (_, None) -> fail "user not found"
        | ReportPlayStatus _ -> ok("")
        | SetPlayedSeconds _ -> ok("")
        | GetAppLink (s,u) -> processGetAppLink appConfig.ClientBaseUrl db s
        | GetDeviceAuthToken (s,u) -> processGetDeviceAuthTokenRequest db s
        | GetExtendedMetadata _ -> fail "not implemented"
        | GetExtendedMetadataText _ -> fail "not implemented"

    let internal smapiImp appConfig eventStore db (c:HttpContext) =
        task {
            let! result = getSmapiMethod db c
            let log (ctx:HttpContext) (m:SmapiMethod) =
                let j = mkjson m
                let logger = ctx.GetLogger()
                logger.LogDebug j
                ok m

            return result
                    >>= log c
                    >>= (processSmapiMethod appConfig eventStore db)
        }

    let internal processSmapiRequest appConfig eventStore db =
        fun next ctx ->
            task {
                let! result = smapiImp appConfig eventStore db ctx
                return! match result with
                        | Success (content) -> text content next ctx
                        | Failure (error) ->
                            let logger = ctx.GetLogger()
                            logger.LogError (sprintf "Error Handling Request: %s" error)
                            RequestErrors.BAD_REQUEST error next ctx
            }

    let smapiRouter appConfig eventStore db = router {
        post "" (processSmapiRequest appConfig eventStore db)
    }