module Castos.Configuration

type AuthConfig = {
    Secret: string
    Issuer: string
}

type Configuration =
    { Auth: AuthConfig }
