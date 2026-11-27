# Сервис учёта подписок

Бэкенд для лабораторной работы № 3. Пользователь может зарегистрироваться, добавить свои подписки, записать платежи и посмотреть расходы за период.

Все суммы в рублях. Платежи вносятся вручную — сервис сам деньги не списывает. У каждого пользователя свой список подписок. Удаление подписки переносит её в архив и сохраняет историю платежей.

Написан на C# и ASP.NET Core (.NET 8). Для работы с базой используются EF Core и SQLite, для тестов — NUnit и Moq.

## Структура

- `src/Subscriptions.Api` — контроллеры, авторизация и Swagger.
- `src/Subscriptions.Business` — бизнес-логика, проверки данных и интерфейсы.
- `src/Subscriptions.DataAccess` — работа с БД, сущности и миграции.
- `tests` — модульные и интеграционные тесты.
- `scripts` — проверки API и покрытия тестами.
- `docs` — требования, ТЗ, промт и описание API.

Запускается один проект — `Subscriptions.Api`. Слой Business не зависит от HTTP и EF Core.

## Как запустить

Нужен .NET SDK с поддержкой .NET 8. Команды выполняются в PowerShell из папки с `Subscriptions.sln`.

```powershell
dotnet restore Subscriptions.sln
dotnet build Subscriptions.sln --no-restore
dotnet dev-certs https --trust
dotnet run --project src/Subscriptions.Api --launch-profile https
```

После запуска открыть [Swagger](https://localhost:7028/swagger).

База `src/Subscriptions.Api/subscriptions-simple.db` создаётся автоматически. Отдельно устанавливать SQLite не нужно. Данные сохраняются после перезапуска.

Если нужно применить миграции вручную:

```powershell
dotnet tool restore
$dbPath = Join-Path $PWD 'src/Subscriptions.Api/subscriptions-simple.db'
dotnet ef database update --project src/Subscriptions.DataAccess --startup-project src/Subscriptions.DataAccess --connection "Data Source=$dbPath;Foreign Keys=True"
```

## Как проверить

Запустить тесты:

```powershell
dotnet test Subscriptions.sln
```

Запустить тесты с измерением покрытия:

```powershell
& ./scripts/Coverage.ps1
```

Для этих проверок запускать API отдельно не нужно. Интеграционные тесты используют временную SQLite.

Для проверки работающего API через curl.exe открыть вторую консоль:

```powershell
& ./scripts/Smoke.ps1
```

Скрипт создаёт тестовых пользователей и проверяет регистрацию, вход, подписки, платежи и расходы. Если API запущен на другом порту:

```powershell
& ./scripts/Smoke.ps1 -BaseUrl 'https://localhost:7049'
```

Последняя проверка: **102 теста прошли** — 86 модульных и 16 интеграционных. Все **30 запросов curl** тоже прошли. Покрытие слоя Business: **99,53% строк и 100% ветвей**. Покрытие операторов отдельно не измерялось. Подробности — в [протоколе](docs/Проверка.md).

## Что учтено в API

Пароли хранятся в виде хеша с солью. После входа выдаётся токен на 30 минут; при выходе он отзывается. Пользователь может обращаться только к своим данным.

Расходы считаются по фактическим платежам за выбранный период. Цена подписки и сумма платежа могут отличаться. Следующая дата платежа автоматически не меняется.

При добавлении платежа нужен заголовок `Idempotency-Key`. Повтор с теми же данными в течение 24 часов возвращает прежний платёж. Если данные изменились, API вернёт 409.

## Документы

- [Требования](docs/Требования%20к%20сервису.md) · [Word](docs/Требования%20к%20сервису.docx)
- [ТЗ](docs/ТЗ%20на%20разработку%20сервиса.md) · [Word](docs/ТЗ%20на%20разработку%20сервиса.docx)
- [Промт для генерации](docs/Промт%20для%20генерации.txt)
- [Описание API](docs/API.md), [OpenAPI](docs/openapi.json) и [выгрузка Swagger](docs/swagger.json)

Предыдущие документы сохранены в [архиве](docs/archive/before-simplification), код до упрощения — в теге `before-simplified-code`.
