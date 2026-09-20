FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["WarcraftNameTools.csproj", "."]
RUN dotnet restore "WarcraftNameTools.csproj"

COPY . .
RUN dotnet publish "WarcraftNameTools.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "WarcraftNameTools.dll"]