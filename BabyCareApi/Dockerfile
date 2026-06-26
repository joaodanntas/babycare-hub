# Etapa 1: compilar a aplicação
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore BabyCareApi/BabyCareApi.csproj
RUN dotnet publish BabyCareApi/BabyCareApi.csproj -c Release -o /app

# Etapa 2: imagem final, mais leve, só com o necessário para rodar
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "BabyCareApi.dll"]
