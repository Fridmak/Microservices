FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./
COPY TaskService/TaskService.csproj TaskService/
COPY Shared/Shared.csproj Shared/
RUN dotnet restore TaskService/TaskService.csproj

COPY . .
RUN dotnet publish TaskService/TaskService.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "TaskService.dll"]
