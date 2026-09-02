FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props PersonalFinanceManager.slnx ./
COPY src/PersonalFinance.Domain/PersonalFinance.Domain.csproj src/PersonalFinance.Domain/
COPY src/PersonalFinance.Application/PersonalFinance.Application.csproj src/PersonalFinance.Application/
COPY src/PersonalFinance.Infrastructure/PersonalFinance.Infrastructure.csproj src/PersonalFinance.Infrastructure/
COPY src/PersonalFinance.Api/PersonalFinance.Api.csproj src/PersonalFinance.Api/

RUN dotnet restore src/PersonalFinance.Api/PersonalFinance.Api.csproj

COPY src/ src/
RUN dotnet publish src/PersonalFinance.Api/PersonalFinance.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "PersonalFinance.Api.dll"]
