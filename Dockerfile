FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder
WORKDIR /src

# Copy project files first to keep restore cache effective.
COPY Weblog.Core.sln ./
COPY src/01_Weblog.Core.Model/Weblog.Core.Model.csproj           src/01_Weblog.Core.Model/
COPY src/02_Weblog.Core.Common/Weblog.Core.Common.csproj         src/02_Weblog.Core.Common/
COPY src/03_Weblog.Core.Repository/Weblog.Core.Repository.csproj src/03_Weblog.Core.Repository/
COPY src/04_Weblog.Core.Service/Weblog.Core.Service.csproj       src/04_Weblog.Core.Service/
COPY src/05_Weblog.Core.Api/Weblog.Core.Api.csproj               src/05_Weblog.Core.Api/

RUN dotnet restore

COPY . .
RUN dotnet publish src/05_Weblog.Core.Api/Weblog.Core.Api.csproj \
    -c Release \
    -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=builder /app/publish .

# Compatibility for existing 1Panel containers that still run:
# dotnet BlueIsland.Api.dll
RUN cp Weblog.Core.Api.dll BlueIsland.Api.dll \
    && cp Weblog.Core.Api.deps.json BlueIsland.Api.deps.json \
    && cp Weblog.Core.Api.runtimeconfig.json BlueIsland.Api.runtimeconfig.json

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000
ENTRYPOINT ["dotnet", "Weblog.Core.Api.dll"]
