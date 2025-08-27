# TokenMonitorApp

WPF-приложение для мониторинга новых токенов и фильтрации по настраиваемым правилам.

## Статус секретов
- Moralis API Key больше не требуется. Клиент работает без ключа. Если ключ всё же задан через переменную окружения `MORALIS_API_KEY`, он будет использован автоматически.
- Тестовый ключ приложения для отладки: `TEST-DEBUG-2024`.
  - Введите его в стартовом окне авторизации для доступа к приложению в тестовом режиме.

## Запуск
1. Требования: .NET 9.0 (Windows, WPF)
2. Открыть решение `TokenMonitorApp.sln` в Visual Studio или запустить из консоли:
   ```powershell
   dotnet restore
   dotnet build -c Release
   dotnet run --project TokenMonitorApp.csproj
   ```

## Настройки и фильтры
- Фильтры сохраняются в `%APPDATA%/TokenMonitorApp/settings.json`.
- Ключи (если когда-либо вводились):
  - `auth.key` — зашифрованный (DPAPI, CurrentUser), хранится в `%APPDATA%/TokenMonitorApp/`.
  - `moralis.key` — больше не используется; при необходимости удалите файл.

## Переменные окружения (необязательно)
- `MORALIS_API_KEY` — если указан, будет добавлен в заголовок `X-API-Key` Moralis клиента.

## Подготовка к публикации на GitHub
- В репозитории присутствует `.gitignore` для исключения артефактов сборки и пользовательских файлов.
- Секреты в коде отсутствуют.
- Тестовый ключ указан только в этом README для целей отладки.

## Удаление локально сохранённого Moralis ключа (если он когда-то сохранялся)
Путь: `%APPDATA%/TokenMonitorApp/moralis.key`

Команда PowerShell (выполняйте при необходимости):
```powershell
$path = Join-Path $env:APPDATA 'TokenMonitorApp/moralis.key'; if (Test-Path $path) { Remove-Item $path -Force }
```


