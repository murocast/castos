FROM mcr.microsoft.com/dotnet/core/aspnet:3.1
COPY /deploy /
WORKDIR /Server
EXPOSE 80

VOLUME /Server/Castos

ENTRYPOINT [ "dotnet", "Server.dll" ]