# InfraBot

Telegram-бот для управления инфраструктурой Windows-серверов: учёт серверов, PowerShell-скриптов, WinRM-учётных записей и запуск задач удалённо через очередь.

## Тема проекта

**Автоматизация администрирования серверов через Telegram.**

InfraBot — это точка входа для инженеров и операторов: вместо прямого подключения к каждому серверу пользователь работает с единым каталогом серверов и скриптов, запускает PowerShell на удалённых машинах по WinRM и получает результат в чат. Доступ разграничен по ролям; все данные хранятся в PostgreSQL.

## Задачи, которые требовалось реализовать

### Пользователи и роли
- Регистрация через `/start` (новый пользователь — **Guest**)
- Роли: **Guest**, **Operator**, **Admin**
- Запрос повышения Guest → Operator (`/pending`) и обработка заявок администратором
- Управление пользователями и смена ролей (`/usercontrol`)

### Серверы
- CRUD серверов: имя, IP, описание, порт WinRM, привязка WinRM-УЗ
- Привязка скриптов к серверу (требования для запуска)
- Выдача и отзыв доступа операторам к конкретным серверам
- Просмотр списка доступных серверов (`/listservers`)

### Скрипты
- CRUD PowerShell-скриптов: имя, описание, текст, флаг возврата JSON, таймаут
- Просмотр, изменение и удаление из карточки скрипта
- Список скриптов для администратора (`/scripts`)

### WinRM-учётные записи
- CRUD учётных записей домена (SAM + пароль) для подключения к серверам
- Смена пароля и удаление из карточки УЗ

### Запуск задач (Job Run)
- Выбор скрипта на сервере и постановка задачи в очередь
- Фоновый исполнитель WinRM (одна задача за раз)
- Статусы: Queued, Running, Success, Failed, Cancelled
- Уведомление пользователя в Telegram по завершении

### Отчёты
- Отчёт по своим запускам за 7 дней (`/report`)
- Отчёт по всем запускам за 7 дней для Admin (`/reportall`)

### Хранение данных
- PostgreSQL — единственное хранилище (JSON/file-storage удалён)
- Доступ через LinqToDB (репозитории `Sql*Repository`)
- Конфигурация подключения в `config.json` (токен бота + connection string)
- SQL-скрипты в каталоге `SQL/` (см. раздел «База данных» в быстром старте)

### UI/UX Telegram
- Reply-клавиатура по роли, inline-кнопки для списков и карточек
- Пошаговые сценарии (добавление/изменение/удаление сущностей)
- Пагинация списков, модуль администрирования (`/admincontrol`)

## Стек

| Компонент | Технология |
|-----------|------------|
| Runtime | .NET 8 |
| Telegram API | Telegram.Bot 22.x |
| БД | PostgreSQL |
| ORM | LinqToDB + Npgsql |
| Удалённое выполнение | WinRM через PowerShell (`JobRunExe`) |

## Структура проекта

```
InfraBot/
├── Core/                  # Сущности, интерфейсы, enum-ы, LinqToDB-модели
├── Infrastructure/        # DataAccess, SQL-репозитории, сервисы, WinRM-исполнитель
├── Scenarios/             # Пошаговые сценарии (add/update/delete/run job)
├── TelegramBot/           # UpdateHandler, инициализация бота
├── HelpData/              # Команды, клавиатуры, тексты
├── Helpers/               # Форматирование, утилиты
├── SQL/                   # Схема, seed и очистка данных
├── Program.cs
└── config.json            # Локальный конфиг (не в git)
```

## Быстрый старт

### 1. База данных

Создание БД и таблиц (один раз):

```powershell
psql -U postgres -d postgres -f SQL/InfraBotDb.sql
```

| Файл | Назначение |
|------|------------|
| `SQL/InfraBotDb.sql` | Создание БД `infrabot` и схемы таблиц |
| `SQL/InfraBotSeedData.sql` | Тестовые данные (перед загрузкой делает `TRUNCATE`) |
| `SQL/InfraBotClearData.sql` | Очистка всех таблиц без удаления схемы |

Скрипты seed и очистки можно выполнять в **pgAdmin → Query Tool** на базе `infrabot` (F5) или через `psql`:

```powershell
# только очистка
psql -U postgres -d infrabot -f SQL/InfraBotClearData.sql

# тестовые данные (очистка + загрузка)
psql -U postgres -d infrabot -f SQL/InfraBotSeedData.sql
```

**Параметры seed** — блок `INSERT INTO seed_params` в начале `SQL/InfraBotSeedData.sql`:

| Параметр | По умолчанию | Описание |
|----------|--------------|----------|
| `admin_users` | 1 | Пользователи с ролью Admin |
| `operator_users` | 3 | Operator |
| `guest_users` | 1 | Guest |
| `server_count` | 15 | Серверы |
| `common_script_count` | 1 | Общие скрипты |
| `personal_script_count` | 15 | Личные скрипты (`srv-N-check`) |
| `svc_account_count` | 2 | WinRM-УЗ |
| `jobs_per_server_user` | 2 | Задач на пару (сервер × пользователь) |

> Admin в seed: `telegram_id = 1000001`.  
> Для своего аккаунта: `UPDATE bot_users SET status = 0 WHERE telegram_id = <ваш_id>;`  
> Job runs в seed создаются с датой `NOW() - 3 days`, чтобы попадать в отчёты за 7 дней.

### 2. Конфигурация

При первом запуске создаётся `config.json`. Можно заполнить заранее:

```json
{
  "token": "YOUR_TELEGRAM_BOT_TOKEN",
  "connectionString": "Host=localhost;Port=5432;Database=infrabot;Username=postgres;Password=..."
}
```

## Роли и команды

| Роль | Основные возможности |
|------|----------------------|
| **Guest** | `/start`, `/pending`, `/help`, `/info` |
| **Operator** | + список серверов, запуск скриптов, `/report` |
| **Admin** | + CRUD серверов/скриптов/WinRM-УЗ, пользователи, `/reportall`, `/admincontrol` |

Полный список команд — `/help` (зависит от роли).

## Лицензия

См. [LICENSE.txt](LICENSE.txt).
