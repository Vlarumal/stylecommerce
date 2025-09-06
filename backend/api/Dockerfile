FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build-env
WORKDIR /app

COPY backend/api/*.csproj ./backend/api/
WORKDIR /app/backend/api
RUN dotnet restore

WORKDIR /app
COPY backend/api ./backend/api

COPY images ./images

WORKDIR /app/backend/api
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

COPY --from=build-env /app/backend/api/out .

COPY --from=build-env /app/images ./images

EXPOSE 80
EXPOSE 8080
EXPOSE 443
EXPOSE 5209

ENTRYPOINT ["dotnet", "StyleCommerce.Api.dll"]