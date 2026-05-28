# === Build stage ===
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем csproj и восстанавливаем пакеты
COPY ["ChineseAcademicPortal.csproj", "./"]
RUN dotnet restore "ChineseAcademicPortal.csproj"

# Копируем исходники и публикуем
COPY . .
RUN dotnet publish "ChineseAcademicPortal.csproj" -c Release -o /app/publish

# === Runtime stage ===
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# Настройки для продакшена
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Папка для данных (база, логи)
RUN mkdir -p /app/data
VOLUME ["/app/data"]

EXPOSE 8080

ENTRYPOINT ["dotnet", "ChineseAcademicPortal.dll"]