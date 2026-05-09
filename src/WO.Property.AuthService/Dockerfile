# WO Property Auth Service Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app

COPY bin/Release/net10.0/publish .

RUN chown -R 1000:1000 /app
USER 1000

EXPOSE 5006

ENV ASPNETCORE_URLS=http://+:5006
ENV PORT=5006

HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:5006/health || exit 1

ENTRYPOINT ["dotnet", "WO.Property.AuthService.dll"]