FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiamos todo
COPY . .

# Restaurar y publicar apuntando al csproj
RUN dotnet restore "GourmetApi/GourmetApi.csproj"
RUN dotnet publish "GourmetApi/GourmetApi.csproj" -c Release -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "GourmetApi.dll"]