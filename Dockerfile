# =================================================================
# ЭТАП 1: СБОРКА (Build stage)
# =================================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем csproj и восстанавливаем пакеты (кешируем слой)
COPY ["ChineseAcademicPortal.csproj", "./"]
RUN dotnet restore "ChineseAcademicPortal.csproj"

# Копируем исходники и публикуем
COPY . .
RUN dotnet publish "ChineseAcademicPortal.csproj" -c Release -o /app/publish

# =================================================================
# ЭТАП 2: РАНТАЙМ (Runtime stage)
# =================================================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Копируем опубликованные файлы из этапа build
COPY --from=build /app/publish .

# Настройки для продакшена
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Создаём папку для данных (если понадобится для бэкапов/логов)
RUN mkdir -p /app/data

# Порт для Render
EXPOSE 8080

# Запуск приложения
ENTRYPOINT ["dotnet", "ChineseAcademicPortal.dll"]