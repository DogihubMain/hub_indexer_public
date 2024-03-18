FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

RUN apt-get update && apt-get install -y redis-server
RUN mkdir /redis_data
VOLUME /redis_data

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["DogiHubIndexer/DogiHubIndexer.csproj", "DogiHubIndexer/"]
RUN dotnet restore "DogiHubIndexer/DogiHubIndexer.csproj"
COPY . .
WORKDIR "/src/DogiHubIndexer"
RUN dotnet build "DogiHubIndexer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DogiHubIndexer.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY docker-entrypoint.sh /usr/local/bin/
RUN chmod +x /usr/local/bin/docker-entrypoint.sh
RUN mkdir -p /app/logs && chmod -R 777 /app/logs

ENTRYPOINT ["docker-entrypoint.sh"]
