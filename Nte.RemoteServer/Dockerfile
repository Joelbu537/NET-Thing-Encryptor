FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Nte.Core/Nte.Core.csproj", "Nte.Core/"]
COPY ["Nte.Storage/Nte.Storage.csproj", "Nte.Storage/"]
COPY ["Nte.RemoteServer/Nte.RemoteServer.csproj", "Nte.RemoteServer/"]
RUN dotnet restore "Nte.RemoteServer/Nte.RemoteServer.csproj"
COPY . .
RUN dotnet publish "Nte.RemoteServer/Nte.RemoteServer.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
RUN addgroup --system nte && adduser --system --ingroup nte nte \
    && mkdir /data && chown nte:nte /data
USER nte
ENV ASPNETCORE_URLS=http://0.0.0.0:5248 \
    NTE_REMOTE_DATA_DIRECTORY=/data
EXPOSE 5248
VOLUME ["/data"]
COPY --from=build --chown=nte:nte /app/publish .
ENTRYPOINT ["dotnet", "Nte.RemoteServer.dll"]
