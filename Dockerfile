FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Presentation/Presentation.csproj", "Presentation/"]
RUN dotnet restore "Presentation/Presentation.csproj"
COPY . .
WORKDIR "/src/Presentation"
RUN dotnet build "Presentation.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Presentation.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Presentation.dll"]
