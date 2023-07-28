FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["EnergyCollectorService/EnergyCollectorService.csproj", "EnergyCollectorService/"]
COPY ["EnergyCollectorService.InfluxDb/EnergyCollectorService.InfluxDb.csproj", "EnergyCollectorService.InfluxDb/"]
COPY ["SvKEnergyCollectorService/SvKEnergyCollectorService.csproj", "SvKEnergyCollectorService/"]
COPY ["EntsoeCollectorService/EntsoeCollectorService.csproj", "EntsoeCollectorService/"]
COPY ["EnergyCollectorService.CurrencyConversion/EnergyCollectorService.CurrencyConversion.csproj", "EnergyCollectorService.CurrencyConversion/"]
COPY ["EnergyCollectorService.Utils/EnergyCollectorService.Utils.csproj", "EnergyCollectorService.Utils/"]

RUN update-ca-certificates
RUN dotnet restore "EnergyCollectorService/EnergyCollectorService.csproj"
RUN dotnet restore "EnergyCollectorService.InfluxDb/EnergyCollectorService.InfluxDb.csproj"
RUN dotnet restore "SvKEnergyCollectorService/SvKEnergyCollectorService.csproj"
RUN dotnet restore "EntsoeCollectorService/EntsoeCollectorService.csproj"
RUN dotnet restore "EnergyCollectorService.CurrencyConversion/EnergyCollectorService.CurrencyConversion.csproj"
RUN dotnet restore "EnergyCollectorService.Utils/EnergyCollectorService.Utils.csproj"
COPY . .
WORKDIR "/src/EnergyCollectorService"
RUN dotnet build "EnergyCollectorService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EnergyCollectorService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EnergyCollectorService.dll"]
