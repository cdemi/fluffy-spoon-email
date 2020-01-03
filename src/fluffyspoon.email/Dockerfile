FROM mcr.microsoft.com/dotnet/core/aspnet:3.1.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/core/sdk:3.1.100 AS publish
WORKDIR /sln
COPY NuGet.Config .
COPY packages/ ./packages
COPY ["src/fluffyspoon.email/fluffyspoon.email.csproj", "src/fluffyspoon.email/"]
RUN dotnet restore "src/fluffyspoon.email/fluffyspoon.email.csproj"
COPY . .
WORKDIR "src/fluffyspoon.email"
ARG VERSION=1
RUN dotnet publish "fluffyspoon.email.csproj" /p:Version=$VERSION -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "fluffyspoon.email.dll"]