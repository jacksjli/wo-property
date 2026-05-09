# WO Property Express Service Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN (groupadd -r appgroup && useradd -r -g appgroup --uid=1000 appuser) || true
COPY bin/Release/net10.0/publish .
RUN chown -R appuser:appgroup /app
USER appuser
EXPOSE 5016
ENV ASPNETCORE_URLS=http://+:5016
ENV PORT=5016
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 CMD curl -f http://localhost:5016/health || exit 1
ENTRYPOINT ["dotnet", "WO.Property.ExpressService.dll"]
