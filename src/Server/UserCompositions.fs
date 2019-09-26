module Castos.UserCompositions

open Castos.Users
open System
open Giraffe
open Saturn
open FSharp.Control.Tasks.V2

open Castos
open Castos.Http
open Microsoft.AspNetCore.Http
open Microsoft.IdentityModel.Tokens

open Microsoft.AspNetCore.Authentication.JwtBearer
open System.IdentityModel.Tokens.Jwt
open System.Security.Claims

open FSharp.Data

let secret = "spadR2dre#u-ruBrE@TepA&*Uf@U" //TODO: not hardcoded
let issuer = "saturnframework.io"

[<CLIMutable>]
type LoginViewModel =
    {
        Email : string
        Password : string
    }

[<CLIMutable>]
type TokenResult =
    {
        Token : string
    }

let generateToken email =
    let claims = [|
        Claim(JwtRegisteredClaimNames.Sub, email)
        Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        Claim(ClaimTypes.Role, "Admin") |]
    claims
    |> Auth.generateJWT (secret, SecurityAlgorithms.HmacSha256) issuer (DateTime.UtcNow.AddHours(1.0))

let handlePostToken (getUser:string -> Result<User option, Error>) =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let! model = ctx.BindJsonAsync<LoginViewModel>()

            let user = getUser model.Email
            let result = match user with
                             | Success (Some u) when u.Password = model.Password //TODO: Hash with salt
                                 -> json { Token = generateToken model.Email} next ctx
                             | _ ->
                                    ctx.Response.StatusCode <- HttpStatusCodes.Unauthorized
                                    json "" next ctx
            return! result
}

let private storeUsersEvent eventStore (version, event) =
    let streamId event = StreamId (sprintf "users")
    eventStore.SaveEvents (streamId event) version [event]

let getUsersComposition eventStore =
    let result = eventStore.GetEvents (StreamId("users"))
    match result with
    | Success (_, events) -> ok (getUsers events)
    | Failure m -> fail m

let getUserComposition eventStore email =
    match getUsersComposition eventStore with
    | Success users -> Success (getUser users email)
    | Failure m -> fail m

let addUserComposition eventStore rendition =
    let result = (StreamVersion 0, UserAdded {
                    Id = Guid.NewGuid() |> UserId
                    Email = rendition.Email
                    Password = rendition.Password
                    //TODO: Hash and Salt
                 }) |> storeUsersEvent eventStore
    match result with
    | Success _ -> ok ("added user")
    | Failure m -> fail m

let authorize =
    requiresAuthentication (challenge JwtBearerDefaults.AuthenticationScheme)

let adminOnly =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let hasAdminClaim = ctx.User.Claims
                                 |> Seq.tryFindIndex
                                        (fun c -> c.Type = ClaimTypes.Role && c.Value = "Admin") //TODO: use literal
            match hasAdminClaim with
            | Some _ -> return! next ctx
            | _ -> return! (clearResponse >=> setStatusCode HttpStatusCodes.Forbidden >=> text "Forbidden") next ctx
        }


let usersRouter eventStore = router {
    //pipe_through authorize  //<-- for all methods of router
    get "" (authorize >=> adminOnly >=>  (processAsync getUsersComposition eventStore))
    post "" (processDataAsync addUserComposition eventStore)
}

