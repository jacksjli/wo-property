FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app

RUN (groupadd -r appgroup && useradd -r -g appgroup --uid=1000 appuser) || true

COPY bin/Release/net10.0/publish .

RUN chown -R 1000:1000 /app
USER 1000

EXPOSE 5014

ENV ASPNETCORE_URLS=http://+:5014
ENV PORT=5014

HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3     CMD curl -f http://localhost:5014/health || exit 1

ENTRYPOINT ["dotnet", "WO.Property.StatisticsService.dll"]
