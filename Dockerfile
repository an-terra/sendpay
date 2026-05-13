FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Build Blazor WebAssembly
COPY SendPay.Web/SendPay.Web.csproj SendPay.Web/
RUN dotnet restore SendPay.Web/SendPay.Web.csproj
COPY SendPay.Web/ SendPay.Web/
# Clear local dev ApiBaseUrl so production uses HostEnvironment.BaseAddress
RUN echo '{}' > SendPay.Web/wwwroot/appsettings.json
RUN dotnet publish SendPay.Web/SendPay.Web.csproj -c Release -o /web/publish

# Build API
COPY SendPay.Api/SendPay.Api.csproj SendPay.Api/
RUN dotnet restore SendPay.Api/SendPay.Api.csproj
COPY SendPay.Api/ SendPay.Api/
RUN dotnet publish SendPay.Api/SendPay.Api.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=build /web/publish/wwwroot ./wwwroot
ENTRYPOINT ["dotnet", "SendPay.Api.dll"]
