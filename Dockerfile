# === Этап 1: Сборка ===
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем проект и восстанавливаем пакеты
COPY ["ChineseAcademicPortal.csproj", "./"]
RUN dotnet restore "ChineseAcademicPortal.csproj"

# Копируем исходники и публикуем
COPY . .
RUN dotnet publish "ChineseAcademicPortal.csproj" -c Release -o /app/publish

# === Этап 2: Рантайм ===
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# Переменные окружения
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Порт для хоста
EXPOSE 8080

# Запуск
ENTRYPOINT ["dotnet", "ChineseAcademicPortal.dll"]