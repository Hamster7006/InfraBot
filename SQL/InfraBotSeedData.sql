-- InfraBot — статические тестовые данные
-- psql -U postgres -d infrabot -f SQL/InfraBotSeedData.sql
--
-- Состав:
--   5 пользователей (1 admin, 3 operator, 1 guest)
--   2 WinRM-УЗ (svc_sam_accounts)
--   16 скриптов (1 общий + 15 уникальных по серверам)
--   15 серверов (у каждого общий + свой скрипт)
--   150 job_runs (2 задачи на каждую пару сервер × пользователь)

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

-- Пользователи (status: 0=Admin, 1=Guest, 2=Operator; pending: 0=None)
INSERT INTO bot_users (id, telegram_id, username, status, pending, created_at)
VALUES
    ('a0000001-0000-4000-8000-000000000001', 1000001, 'admin_demo',    0, 0, '2026-01-01 00:00:00+00'),
    ('a0000001-0000-4000-8000-000000000002', 1000002, 'operator_01',   2, 0, '2026-01-01 00:01:00+00'),
    ('a0000001-0000-4000-8000-000000000003', 1000003, 'operator_02',   2, 0, '2026-01-01 00:02:00+00'),
    ('a0000001-0000-4000-8000-000000000004', 1000004, 'operator_03',   2, 0, '2026-01-01 00:03:00+00'),
    ('a0000001-0000-4000-8000-000000000005', 1000005, 'guest_demo',    1, 0, '2026-01-01 00:04:00+00');

-- WinRM-УЗ
INSERT INTO svc_sam_accounts (id, sam_account_name, password)
VALUES
    ('b0000001-0000-4000-8000-000000000001', 'CONTOSO\svc_infrabot_01', 'DemoPassword001!'),
    ('b0000001-0000-4000-8000-000000000002', 'CONTOSO\svc_infrabot_02', 'DemoPassword002!');

-- Общий скрипт для всех серверов
INSERT INTO scripts (id, name, description, content, return_data, timeout_seconds, created_at, created_by_id)
VALUES
    (
        'c0000001-0000-4000-8000-000000000001',
        'common-healthcheck',
        'Общий healthcheck для всех серверов',
        'Write-Output ''{"success":true,"script":"common-healthcheck"}''',
        TRUE,
        120,
        '2026-01-01 01:00:00+00',
        'a0000001-0000-4000-8000-000000000001'
    );

-- Уникальный скрипт для каждого сервера (15 шт.)
INSERT INTO scripts (id, name, description, content, return_data, timeout_seconds, created_at, created_by_id)
SELECT
    ('c0000001-0000-4000-8000-' || lpad(n::text, 12, '0'))::uuid,
    'srv-' || lpad(n::text, 2, '0') || '-check',
    'Уникальный скрипт для srv-' || lpad(n::text, 2, '0'),
    'Write-Output ''{"success":true,"script":"srv-' || lpad(n::text, 2, '0') || '-check"}''',
    TRUE,
    120,
    '2026-01-01 01:00:00+00'::timestamptz + (n || ' minutes')::interval,
    'a0000001-0000-4000-8000-000000000001'::uuid
FROM generate_series(2, 16) AS n;

-- Серверы (15 шт.)
INSERT INTO servers (
    id,
    server_name,
    ip_address,
    description,
    registered_by_user_id,
    win_rm_port,
    svc_sam_account_id
)
SELECT
    ('d0000001-0000-4000-8000-' || lpad(n::text, 12, '0'))::uuid,
    'srv-' || lpad(n::text, 2, '0'),
    '10.0.0.' || n::text,
    'Demo server #' || n::text,
    'a0000001-0000-4000-8000-000000000001'::uuid,
    5986,
    CASE WHEN n % 2 = 1
        THEN 'b0000001-0000-4000-8000-000000000001'::uuid
        ELSE 'b0000001-0000-4000-8000-000000000002'::uuid
    END
FROM generate_series(1, 15) AS n;

-- Требования скриптов: общий + уникальный для каждого сервера
INSERT INTO server_script_requirements (server_id, script_id)
SELECT
    ('d0000001-0000-4000-8000-' || lpad(s.n::text, 12, '0'))::uuid,
    'c0000001-0000-4000-8000-000000000001'::uuid
FROM generate_series(1, 15) AS s(n);

INSERT INTO server_script_requirements (server_id, script_id)
SELECT
    ('d0000001-0000-4000-8000-' || lpad(s.n::text, 12, '0'))::uuid,
    ('c0000001-0000-4000-8000-' || lpad((s.n + 1)::text, 12, '0'))::uuid
FROM generate_series(1, 15) AS s(n);

-- Доступ всех 5 пользователей ко всем серверам
INSERT INTO server_granted_users (server_id, user_id)
SELECT
    ('d0000001-0000-4000-8000-' || lpad(s.n::text, 12, '0'))::uuid,
    ('a0000001-0000-4000-8000-' || lpad(u.n::text, 12, '0'))::uuid
FROM generate_series(1, 15) AS s(n)
CROSS JOIN generate_series(1, 5) AS u(n);

-- Job runs: 2 задачи на каждую пару (сервер × пользователь) = 150
INSERT INTO job_runs (
    id,
    status,
    result_json,
    error_message,
    exit_code,
    created_at,
    started_at,
    finished_at,
    script_id,
    server_id,
    initiated_by_id,
    chat_id
)
SELECT
    ('e0000001-0000-4000-8000-' || lpad(
        ((s.n - 1) * 10 + (u.n - 1) * 2 + j.n)::text, 12, '0'))::uuid,
    CASE j.n
        WHEN 1 THEN 2  -- Success
        ELSE CASE (s.n + u.n) % 4
            WHEN 0 THEN 0  -- Queued
            WHEN 1 THEN 2  -- Success
            WHEN 2 THEN 3  -- Failed
            ELSE 2         -- Success
        END
    END,
    CASE
        WHEN j.n = 1 OR (s.n + u.n) % 4 IN (1, 3) THEN '{"success":true}'
        WHEN (s.n + u.n) % 4 = 2 THEN NULL
        ELSE NULL
    END,
    CASE
        WHEN j.n <> 1 AND (s.n + u.n) % 4 = 2 THEN 'Demo failure'
        ELSE NULL
    END,
    CASE
        WHEN j.n = 1 OR (s.n + u.n) % 4 IN (1, 3) THEN 0
        WHEN (s.n + u.n) % 4 = 2 THEN 1
        ELSE NULL
    END,
    '2026-01-02 00:00:00+00'::timestamptz
        + ((s.n - 1) * 10 + (u.n - 1) * 2 + j.n) * interval '1 minute',
    CASE
        WHEN j.n = 1 OR (s.n + u.n) % 4 <> 0 THEN
            '2026-01-02 00:00:00+00'::timestamptz
                + ((s.n - 1) * 10 + (u.n - 1) * 2 + j.n) * interval '1 minute'
                + interval '5 seconds'
        ELSE NULL
    END,
    CASE
        WHEN j.n = 1 OR (s.n + u.n) % 4 IN (1, 3) THEN
            '2026-01-02 00:00:00+00'::timestamptz
                + ((s.n - 1) * 10 + (u.n - 1) * 2 + j.n) * interval '1 minute'
                + interval '30 seconds'
        WHEN (s.n + u.n) % 4 = 2 THEN
            '2026-01-02 00:00:00+00'::timestamptz
                + ((s.n - 1) * 10 + (u.n - 1) * 2 + j.n) * interval '1 minute'
                + interval '20 seconds'
        ELSE NULL
    END,
    CASE j.n
        WHEN 1 THEN 'c0000001-0000-4000-8000-000000000001'::uuid
        ELSE ('c0000001-0000-4000-8000-' || lpad((s.n + 1)::text, 12, '0'))::uuid
    END,
    ('d0000001-0000-4000-8000-' || lpad(s.n::text, 12, '0'))::uuid,
    ('a0000001-0000-4000-8000-' || lpad(u.n::text, 12, '0'))::uuid,
    1000000 + u.n
FROM generate_series(1, 15) AS s(n)
CROSS JOIN generate_series(1, 5) AS u(n)
CROSS JOIN generate_series(1, 2) AS j(n);

COMMIT;

-- Проверка
SELECT 'bot_users' AS entity, COUNT(*) AS cnt FROM bot_users
UNION ALL SELECT 'svc_sam_accounts', COUNT(*) FROM svc_sam_accounts
UNION ALL SELECT 'scripts', COUNT(*) FROM scripts
UNION ALL SELECT 'servers', COUNT(*) FROM servers
UNION ALL SELECT 'server_script_requirements', COUNT(*) FROM server_script_requirements
UNION ALL SELECT 'server_granted_users', COUNT(*) FROM server_granted_users
UNION ALL SELECT 'job_runs', COUNT(*) FROM job_runs
ORDER BY entity;
