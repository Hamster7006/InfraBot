-- InfraBot — очистка всех данных (схема таблиц сохраняется)
-- Запуск: pgAdmin (Query Tool) или psql -U postgres -d infrabot -f SQL/InfraBotClearData.sql
--
-- Удаляет строки из всех таблиц приложения. Структура БД не затрагивается.

BEGIN;

TRUNCATE TABLE
    job_runs,
    server_script_requirements,
    server_granted_users,
    servers,
    scripts,
    svc_sam_accounts,
    bot_users
RESTART IDENTITY CASCADE;

COMMIT;

-- Проверка: все счётчики должны быть 0
SELECT 'bot_users' AS entity, COUNT(*) AS cnt FROM bot_users
UNION ALL SELECT 'svc_sam_accounts', COUNT(*) FROM svc_sam_accounts
UNION ALL SELECT 'scripts', COUNT(*) FROM scripts
UNION ALL SELECT 'servers', COUNT(*) FROM servers
UNION ALL SELECT 'server_script_requirements', COUNT(*) FROM server_script_requirements
UNION ALL SELECT 'server_granted_users', COUNT(*) FROM server_granted_users
UNION ALL SELECT 'job_runs', COUNT(*) FROM job_runs
ORDER BY entity;
