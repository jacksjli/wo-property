# === 构建阶段 ===
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 复制项目文件和Shared依赖
COPY WO.Property.Shared/WO.Property.Shared.csproj ./WO.Property.Shared/
COPY WO.Property.PersonService/WO.Property.PersonService.csproj ./WO.Property.PersonService/
RUN dotnet restore WO.Property.PersonService/WO.Property.PersonService.csproj

# 复制源代码并构建
COPY WO.Property.Shared/ ./WO.Property.Shared/
COPY WO.Property.PersonService/ ./WO.Property.PersonService/
RUN dotnet publish WO.Property.PersonService/WO.Property.PersonService.csproj \
    -c Release -o /app/publish --no-restore

# === 运行阶段 ===
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# 安装 curl（健康检查用）
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

# 创建非root用户
RUN groupadd --system --gid 1000 appgroup && \
    useradd --system --uid 1000 --ingroup appgroup appuser

# 复制发布文件
COPY --from=build /app/publish .

# 权限
RUN chown -R appuser:appgroup /app

USER appuser

# 端口
EXPOSE 5018

# 环境变量
ENV ASPNETCORE_URLS=http://+:5018
ENV PORT=5018

# 健康检查
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:5018/health || exit 1

ENTRYPOINT ["dotnet", "WO.Property.PersonService.dll"]
