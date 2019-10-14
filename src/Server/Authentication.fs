module Castos.Auth

open Castos
open System
open Giraffe
open Saturn
open FSharp.Control.Tasks.V2


open Microsoft.AspNetCore.Http
open Microsoft.IdentityModel.Tokens

open Microsoft.AspNetCore.Authentication.JwtBearer
open System.IdentityModel.Tokens.Jwt
open System.Security.Claims

open FSharp.Data

let secret = "spadR2dre#u-ruBrE@TepA&*Uf@U" //TODO: not hardcoded
let issuer = "saturnframework.io"

type User = {
    Id : UserId
    Email: string
    Password: string //TODO: Hash and salt
    Roles: string list
}

type AuthenticatedUser = {
    Id : UserId
    Email: string
    Roles: string list
}

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

let claimsToAuthUser (cp:ClaimsPrincipal):AuthenticatedUser =
    cp.Claims
    |> Seq.fold (fun state c -> match c.Type with
                                | JwtRegisteredClaimNames.Sub -> { state with Email = c.Value}
                                | ClaimTypes.Sid -> { state with Id = UserId (Guid.Parse(c.Value))}
                                | ClaimTypes.Role -> { state with Roles = (state.Roles @ [c.Value])}
                                | _ -> state)
        { Id = UserId(Guid.Empty)
          Email = ""
          Roles = [] }

let generateToken (user:User) =
    let guid (UserId id) =
        id

    let claims = [|
        Claim(JwtRegisteredClaimNames.Sub, user.Email)
        Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        Claim(ClaimTypes.Sid, (guid user.Id).ToString())
        Claim(ClaimTypes.Role, "Admin") |] //TODO: Not everone is admin; use literal

    let roles = user.Roles |> List.map (fun r -> Claim(ClaimTypes.Role, r))

    claims
    |> Array.append (Array.ofList roles)
    |> Auth.generateJWT (secret, SecurityAlgorithms.HmacSha256) issuer (DateTime.UtcNow.AddHours(1.0))

let handlePostToken (getUser:string -> Result<User option, Error>) =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let! model = ctx.BindJsonAsync<LoginViewModel>()

            let user = getUser model.Email
            let result = match user with
                             | Success (Some u) when u.Password = model.Password //TODO: Hash with salt
                                 -> json { Token = generateToken u} next ctx
                             | _ ->
                                    ctx.Response.StatusCode <- HttpStatusCodes.Unauthorized
                                    json "" next ctx
            return! result
}

let authorize:(HttpFunc-> HttpContext -> HttpFuncResult) =
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