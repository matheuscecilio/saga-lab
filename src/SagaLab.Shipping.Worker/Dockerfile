FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["NuGet.config", "Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["src/SagaLab.Contracts/SagaLab.Contracts.csproj", "src/SagaLab.Contracts/"]
COPY ["src/SagaLab.Shipping.Worker/SagaLab.Shipping.Worker.csproj", "src/SagaLab.Shipping.Worker/"]
RUN dotnet restore "src/SagaLab.Shipping.Worker/SagaLab.Shipping.Worker.csproj"

COPY . .
RUN dotnet publish "src/SagaLab.Shipping.Worker/SagaLab.Shipping.Worker.csproj" --configuration Release --output /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

USER app
ENTRYPOINT ["dotnet", "SagaLab.Shipping.Worker.dll"]
