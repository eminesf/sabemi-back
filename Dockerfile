FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY src/Sabemi.Webhooks.Model/Sabemi.Webhooks.Model.csproj src/Sabemi.Webhooks.Model/
COPY src/Sabemi.Webhooks.Service/Sabemi.Webhooks.Service.csproj src/Sabemi.Webhooks.Service/
COPY src/Sabemi.Webhooks.Repository/Sabemi.Webhooks.Repository.csproj src/Sabemi.Webhooks.Repository/
COPY src/Sabemi.Webhooks.Api/Sabemi.Webhooks.Api.csproj src/Sabemi.Webhooks.Api/
RUN dotnet restore src/Sabemi.Webhooks.Api/Sabemi.Webhooks.Api.csproj

COPY src/ src/
RUN dotnet publish src/Sabemi.Webhooks.Api/Sabemi.Webhooks.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

# Railway injeta a variável PORT em runtime (não existe em build time),
# por isso a resolução do ASPNETCORE_URLS acontece no CMD (shell form),
# não em ENV.
CMD ASPNETCORE_URLS=http://+:${PORT:-8080} dotnet Sabemi.Webhooks.Api.dll
