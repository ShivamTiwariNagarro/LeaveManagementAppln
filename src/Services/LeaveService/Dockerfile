FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5003

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Services/LeaveService/LeaveService.csproj", "Services/LeaveService/"]
COPY ["Shared/Shared.Common/Shared.Common.csproj", "Shared/Shared.Common/"]
RUN dotnet restore "Services/LeaveService/LeaveService.csproj"
COPY . .
WORKDIR "/src/Services/LeaveService"
RUN dotnet build "LeaveService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LeaveService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LeaveService.dll"]
