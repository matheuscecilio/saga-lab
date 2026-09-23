FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["NuGet.config", "Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["src/SagaLab.Contracts/SagaLab.Contracts.csproj", "src/SagaLab.Contracts/"]
COPY ["src/SagaLab.Order.Api/SagaLab.Order.Api.csproj", "src/SagaLab.Order.Api/"]
RUN dotnet restore "src/SagaLab.Order.Api/SagaLab.Order.Api.csproj"

COPY . .
RUN dotnet publish "src/SagaLab.Order.Api/SagaLab.Order.Api.csproj" --configuration Release --output /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "SagaLab.Order.Api.dll"]
