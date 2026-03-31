# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/ ./

RUN dotnet restore 05_Weblog.Core.Api/Weblog.Core.Api.csproj
RUN dotnet publish 05_Weblog.Core.Api/Weblog.Core.Api.csproj -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5000;https://+:5001
EXPOSE 5000 5001
ENTRYPOINT ["dotnet", "Weblog.Core.Api.dll"]
