module Castos.Auth

open Castos
open Castos.Configuration
open Murocast.Shared.Core.UserAccount.Domain.Queries
open System
open System.Security.Cryptography
open Microsoft.AspNetCore.Cryptography.KeyDerivation
open Giraffe
open Saturn


open Microsoft.AspNetCore.Http
open Microsoft.IdentityModel.Tokens

open Microsoft.AspNetCore.Authentication.JwtBearer
open System.IdentityModel.Tokens.Jwt
open System.Security.Claims

open FSharp.Data

type User = {
    Id : UserId
    Email: string
    PasswordHash: string
    Salt: byte array
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

let generateSalt() =
    use rng = RandomNumberGenerator.Create()
    let mutable (salt:byte array) = Array.zeroCreate (128 / 8) //128bit salt
    rng.GetBytes(salt)
    salt

let calculateHash password salt =
    Convert.ToBase64String(KeyDerivation.Pbkdf2(password, salt, KeyDerivationPrf.HMACSHA256, 10000, 256 / 8)) //256bit hash with SHA256

let claimsToAuthUser (cp:ClaimsPrincipal):AuthenticatedUser =
    cp.Claims
    |> Seq.fold (fun state c -> match c.Type with
                                | JwtRegisteredClaimNames.Sub -> { state with Email = c.Value}
                                | ClaimTypes.Sid -> { state with Id = Guid.Parse(c.Value)}
                                | ClaimTypes.Role -> { state with Roles = (state.Roles @ [c.Value])}
                                | _ -> state)
        { Id = Guid.Empty
          Email = ""
          Roles = [] }

let generateToken secret issuer userId email roles =
    let claims = [|
        Claim(JwtRegisteredClaimNames.Sub, email)
        Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        Claim(ClaimTypes.Sid, (userId).ToString())
        Claim(ClaimTypes.Role, "Admin") |] //TODO: Not everone is admin; use literal

    let roles = roles |> List.map (fun r -> Claim(ClaimTypes.Role, r))

    claims
    |> Array.append (Array.ofList roles)
    |> Auth.generateJWT (secret, SecurityAlgorithms.HmacSha256) issuer (DateTime.UtcNow.AddHours(1.0))

let authorize:(HttpFunc-> HttpContext -> HttpFuncResult) =
    requiresAuthentication (challenge JwtBearerDefaults.AuthenticationScheme)

let handlePostToken authConfig (getUser:string -> Result<User option, Error>) =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let! model = ctx.BindJsonAsync<LoginViewModel>()

            let user = getUser model.Email
            let result = match user with
                             | Ok (Some u) when u.PasswordHash = calculateHash model.Password u.Salt
                                 -> json { Token = generateToken authConfig.Secret authConfig.Issuer u.Id u.Email u.Roles} next ctx
                             | _ ->
                                    ctx.Response.StatusCode <- HttpStatusCodes.Unauthorized
                                    json "" next ctx
            return! result
}

let handleRefreshToken authConfig =
    fun (next : HttpFunc) (ctx : HttpContext) ->
            task {
                let u = claimsToAuthUser ctx.User
                return! json ({ Token = generateToken authConfig.Secret authConfig.Issuer u.Id u.Email u.Roles}:TokenResult) next ctx
            }

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