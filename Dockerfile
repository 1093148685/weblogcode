# ── 阶段一：构建 .NET 后端 ──
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder
WORKDIR /src

# 先单独复制 csproj / sln，利用 Docker 层缓存加速 restore
COPY Weblog.Core.sln ./
COPY src/01_Weblog.Core.Model/Weblog.Core.Model.csproj           src/01_Weblog.Core.Model/
COPY src/02_Weblog.Core.Common/Weblog.Core.Common.csproj         src/02_Weblog.Core.Common/
COPY src/03_Weblog.Core.Repository/Weblog.Core.Repository.csproj src/03_Weblog.Core.Repository/
COPY src/04_Weblog.Core.Service/Weblog.Core.Service.csproj       src/04_Weblog.Core.Service/
COPY src/05_Weblog.Core.Api/Weblog.Core.Api.csproj               src/05_Weblog.Core.Api/

RUN dotnet restore

# 复制全部源码并发布
COPY . .
RUN dotnet publish src/05_Weblog.Core.Api/Weblog.Core.Api.csproj \
    -c Release \
    -o /app/publish

# ── 阶段二：运行时镜像 ──
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=builder /app/publish .

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000
ENTRYPOINT ["dotnet", "Weblog.Core.Api.dll"]
