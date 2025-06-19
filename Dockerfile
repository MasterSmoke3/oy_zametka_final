# Используем официальный .NET SDK для сборки
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Копируем csproj и восстанавливаем зависимости
COPY *.csproj ./
RUN dotnet restore

# Копируем остальной код
COPY . ./

# Сборка проекта
RUN dotnet publish -c Release -o out

# Используем .NET Runtime для запуска
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/out ./

# Открываем нужный порт
EXPOSE 80

# Команда запуска
ENTRYPOINT ["dotnet", "ZametkiApp.dll"]
