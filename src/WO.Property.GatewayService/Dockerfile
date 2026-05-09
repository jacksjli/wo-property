FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["WO.Property.GatewayService.csproj", "./"]
RUN dotnet restore "WO.Property.GatewayService.csproj"
COPY . .
RUN dotnet build "WO.Property.GatewayService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WO.Property.GatewayService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "WO.Property.GatewayService.dll"]
