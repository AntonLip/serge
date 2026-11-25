FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY CleanDistrict.sln ./
COPY src/CleanDistrict.Domain/CleanDistrict.Domain.csproj src/CleanDistrict.Domain/
COPY src/CleanDistrict.Application/CleanDistrict.Application.csproj src/CleanDistrict.Application/
COPY src/CleanDistrict.Infrastructure/CleanDistrict.Infrastructure.csproj src/CleanDistrict.Infrastructure/
COPY src/CleanDistrict.Web/CleanDistrict.Web.csproj src/CleanDistrict.Web/
RUN dotnet restore CleanDistrict.sln

COPY src/ src/
RUN dotnet publish src/CleanDistrict.Web/CleanDistrict.Web.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
RUN apt-get update && apt-get install -y --no-install-recommends libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .
RUN mkdir -p /app/App_Data/uploads /home/app/.aspnet/DataProtection-Keys && chown -R $APP_UID:$APP_UID /app/App_Data /home/app/.aspnet
USER $APP_UID

ENTRYPOINT ["dotnet", "CleanDistrict.Web.dll"]
