# === Runtime stage ===
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# Устанавливаем curl для health checks и wait-for-it
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Создаём скрипт ожидания БД
RUN echo '#!/bin/bash\n\
echo "Waiting for PostgreSQL..."\n\
while ! nc -z $DB_HOST ${DB_PORT:-5432} 2>/dev/null; do\n\
  sleep 1\n\
done\n\
echo "PostgreSQL is up!"\n\
exec dotnet ChineseAcademicPortal.dll' > /app/entrypoint.sh

RUN chmod +x /app/entrypoint.sh

EXPOSE 8080

# Запускаем через скрипт ожидания
ENTRYPOINT ["/app/entrypoint.sh"]