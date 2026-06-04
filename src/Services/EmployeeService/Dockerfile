FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5002

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Services/EmployeeService/EmployeeService.csproj", "Services/EmployeeService/"]
COPY ["Shared/Shared.Common/Shared.Common.csproj", "Shared/Shared.Common/"]
RUN dotnet restore "Services/EmployeeService/EmployeeService.csproj"
COPY . .
WORKDIR "/src/Services/EmployeeService"
RUN dotnet build "EmployeeService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EmployeeService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EmployeeService.dll"]
