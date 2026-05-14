# Build context BẮT BUỘC là thư mục gốc repo sendpay (cùng cấp với SendPay.Api/).
# Ví dụ: docker build -f SendPay.Api/Dockerfile .
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY SendPay.Api/SendPay.Api.csproj SendPay.Api/
RUN dotnet restore SendPay.Api/SendPay.Api.csproj
COPY SendPay.Api/ SendPay.Api/
RUN dotnet publish SendPay.Api/SendPay.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SendPay.Api.dll"]
