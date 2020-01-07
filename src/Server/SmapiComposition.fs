namespace Castos

open Giraffe
open Saturn
open FSharp.Control.Tasks.V2

open Castos
open Castos.Smapi
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging

module SmapiCompositions =
    let internal getSmapiMethod (c:HttpContext) =
            task{
                let m = match c.GetRequestHeader "SOAPAction" with
                        | Error msg -> failwith msg
                        | Result.Ok headerValue -> extractSmapiMethod headerValue

                let! formString = Http.rawFormString c
                return match m with
                       | "getMetadata" -> ok (GetMetadata (formString))
                       | "getMediaMetadata" -> ok (GetMediaMetadata (formString))
                       | "getMediaURI" -> ok(GetMediaURI (formString))
                       | "getLastUpdate" -> ok(GetLastUpdate(formString))
                       | "getExtendedMetadataRequest" -> ok(GetExtendedMetadata(formString))
                       | "getExtendedMetadataRequestText" -> ok(GetExtendedMetadataText(formString))
                       | "reportPlaySeconds" -> ok(ReportPlaySeconds(formString))
                       | "reportPlayStatus" -> ok(ReportPlayStatus(formString))
                       | "setPlayedSeconds" -> ok(SetPlayedSeconds(formString))
                       | "getAppLink" -> ok(GetAppLink(formString))
                       | "getDeviceAuthToken" -> ok(GetDeviceAuthToken(formString))
                       | _ -> fail(sprintf "Method not implemented %s" m)
            }

    let internal processSmapiMethod eventStore db m =
        match m with
        | GetMetadata s -> processGetMetadata eventStore (GetMetadataRequest.Parse s)
        | GetMediaMetadata s -> processGetMediaMetadata eventStore (GetMediaMetadataRequest.Parse s)
        | GetLastUpdate s -> processGetLastUpdate (GetLastUpdateRequest.Parse s)
        | GetMediaURI s -> processGetMediaURI eventStore s
        | ReportPlaySeconds s ->
             processReportPlaySecondsRequest eventStore s
             ok("")
        | ReportPlayStatus _ -> ok("")
        | SetPlayedSeconds _ -> ok("")
        | GetAppLink s -> processGetAppLink db s
        | GetDeviceAuthToken s -> processGetDeviceAuthTokenRequest db s
        | GetExtendedMetadata _ -> fail "not implemented"
        | GetExtendedMetadataText _ -> fail "not implemented"

    let internal smapiImp eventStore db (c:HttpContext) =
        task {
            let! result = getSmapiMethod c
            let log (ctx:HttpContext) (m:SmapiMethod) =
                let j = mkjson m
                let logger = ctx.GetLogger()
                logger.LogInformation j
                ok m

            return result
                    >>= log c
                    >>= (processSmapiMethod eventStore db)
        }

    let internal processSmapiRequest eventStore db =
        fun next ctx ->
            task {
                let! result = smapiImp eventStore db ctx
                return! match result with
                        | Success (content) -> text content next ctx
                        | Failure (error) ->
                            let logger = ctx.GetLogger()
                            logger.LogError (sprintf "Error Handling Request: %s" error)
                            RequestErrors.BAD_REQUEST error next ctx
            }

    let smapiRouter eventStore db = router {
        post "" (processSmapiRequest eventStore db)
    }