namespace Castos

open Giraffe
open Saturn
open FSharp.Control.Tasks.V2

open Castos
open Castos.Smapi
open Microsoft.AspNetCore.Http

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
                       | "GetDeviceAuthToken" -> ok(GetDeviceAuthToken(formString))
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

    let internal smapiImp eventStore db c =
        task {
            let! result = getSmapiMethod c
            return result >>= (processSmapiMethod eventStore db)
        }

    let internal processSmapiRequest eventStore db =
        fun next ctx ->
            task {
                let! result = smapiImp eventStore db ctx
                return! match result with
                        | Success (content) -> text content next ctx
                        | Failure (_) -> RequestErrors.BAD_REQUEST "Error" next ctx
            }

    let smapiRouter eventStore db = router {
        post "" (processSmapiRequest eventStore db)
    }