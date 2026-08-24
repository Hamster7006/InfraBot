-- InfraBot — статические тестовые данные
-- Запуск: pgAdmin (Query Tool) или psql -U postgres -d infrabot -f SQL/InfraBotSeedData.sql
--
-- Параметры — одна строка в INSERT INTO seed_params (секция ниже).

BEGIN;

-- ========== Параметры seed (меняйте значения в INSERT) ==========
DROP TABLE IF EXISTS seed_params;
CREATE TEMP TABLE seed_params (
    admin_users           INT NOT NULL,
    operator_users        INT NOT NULL,
    guest_users           INT NOT NULL,
    server_count          INT NOT NULL,
    common_script_count   INT NOT NULL,
    personal_script_count INT NOT NULL,
    svc_account_count     INT NOT NULL,
    jobs_per_server_user  INT NOT NULL
);

INSERT INTO seed_params VALUES (
    1,  -- admin_users
    3,  -- operator_users
    1,  -- guest_users
    15, -- server_count
    1,  -- common_script_count
    15, -- personal_script_count
    2,  -- svc_account_count
    2   -- jobs_per_server_user
);
-- ================================================================

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
SELECT
    ('a0000001-0000-4000-8000-' || lpad(n::text, 12, '0'))::uuid,
    1000000 + n,
    CASE
        WHEN n <= p.admin_users THEN
            'admin_' || lpad(n::text, length(p.admin_users::text), '0')
        WHEN n <= p.admin_users + p.operator_users THEN
            'operator_' || lpad((n - p.admin_users)::text, length(p.operator_users::text), '0')
        ELSE
            'guest_' || lpad((n - p.admin_users - p.operator_users)::text, length(p.guest_users::text), '0')
    END,
    CASE
        WHEN n <= p.admin_users THEN 0
        WHEN n <= p.admin_users + p.operator_users THEN 2
        ELSE 1
    END,
    0,
    '2026-01-01 00:00:00+00'::timestamptz + (n - 1) * interval '1 minute'
FROM seed_params p
CROSS JOIN generate_series(1, p.admin_users + p.operator_users + p.guest_users) AS n;

-- WinRM-УЗ
INSERT INTO svc_sam_accounts (id, sam_account_name, password)
SELECT
    ('b0000001-0000-4000-8000-' || lpad(n::text, 12, '0'))::uuid,
    'CONTOSO\svc_infrabot_' || lpad(n::text, length(p.svc_account_count::text), '0'),
    'DemoPassword' || lpad(n::text, length(p.svc_account_count::text), '0') || '!'
FROM seed_params p
CROSS JOIN generate_series(1, p.svc_account_count) AS n;

-- Общие скрипты (для всех серверов)
INSERT INTO scripts (id, name, description, content, return_data, timeout_seconds, created_at, created_by_id)
SELECT
    ('c0000001-0000-4000-8000-' || lpad(n::text, 12, '0'))::uuid,
    CASE WHEN p.common_script_count = 1 THEN 'common-healthcheck'
         ELSE 'common-' || lpad(n::text, length(p.common_script_count::text), '0')
    END,
    CASE WHEN p.common_script_count = 1 THEN 'Общий healthcheck для всех серверов'
         ELSE 'Общий скрипт #' || n::text
    END,
    format(
        'Write-Output ''{"success":true,"script":"common-%s"}''',
        lpad(n::text, length(p.common_script_count::text), '0')),
    TRUE,
    120,
    '2026-01-01 01:00:00+00'::timestamptz + (n - 1) * interval '1 minute',
    'a0000001-0000-4000-8000-000000000001'::uuid
FROM seed_params p
CROSS JOIN generate_series(1, p.common_script_count) AS n;

-- Личные скрипты (srv-N-check); ширина номера = длина personal_script_count (без обрезки 100→10)
INSERT INTO scripts (id, name, description, content, return_data, timeout_seconds, created_at, created_by_id)
SELECT
    ('c0000001-0000-4000-8000-' || lpad((p.common_script_count + n)::text, 12, '0'))::uuid,
    'srv-' || lpad(n::text, length(p.personal_script_count::text), '0') || '-check',
    'Личный скрипт для srv-' || lpad(n::text, length(p.personal_script_count::text), '0'),
    format(
        'Write-Output ''{"success":true,"script":"srv-%s-check"}''',
        lpad(n::text, length(p.personal_script_count::text), '0')),
    TRUE,
    120,
    '2026-01-01 01:00:00+00'::timestamptz + (p.common_script_count + n) * interval '1 minute',
    'a0000001-0000-4000-8000-000000000001'::uuid
FROM seed_params p
CROSS JOIN generate_series(1, p.personal_script_count) AS n;

-- Серверы
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
    'srv-' || lpad(n::text, length(p.server_count::text), '0'),
    '10.0.0.' || n::text,
    'Demo server #' || n::text,
    'a0000001-0000-4000-8000-000000000001'::uuid,
    5986,
    ('b0000001-0000-4000-8000-' ||
        lpad((((n - 1) % p.svc_account_count) + 1)::text, 12, '0'))::uuid
FROM seed_params p
CROSS JOIN generate_series(1, p.server_count) AS n;

-- Требования: все общие скрипты на каждый сервер
INSERT INTO server_script_requirements (server_id, script_id)
SELECT
    ('d0000001-0000-4000-8000-' || lpad(s.n::text, 12, '0'))::uuid,
    ('c0000001-0000-4000-8000-' || lpad(c.n::text, 12, '0'))::uuid
FROM seed_params p
CROSS JOIN generate_series(1, p.server_count) AS s(n)
CROSS JOIN generate_series(1, p.common_script_count) AS c(n);

-- Требования: личный скрипт сервера (srv-N → script common+N)
INSERT INTO server_script_requirements (server_id, script_id)
SELECT
    ('d0000001-0000-4000-8000-' || lpad(s.n::text, 12, '0'))::uuid,
    ('c0000001-0000-4000-8000-' ||
        lpad((p.common_script_count + s.n)::text, 12, '0'))::uuid
FROM seed_params p
CROSS JOIN generate_series(1, p.server_count) AS s(n)
WHERE s.n <= p.personal_script_count;

-- Доступ всех пользователей ко всем серверам
INSERT INTO server_granted_users (server_id, user_id)
SELECT
    ('d0000001-0000-4000-8000-' || lpad(s.n::text, 12, '0'))::uuid,
    ('a0000001-0000-4000-8000-' || lpad(u.n::text, 12, '0'))::uuid
FROM seed_params p
CROSS JOIN generate_series(1, p.server_count) AS s(n)
CROSS JOIN generate_series(1, p.admin_users + p.operator_users + p.guest_users) AS u(n);

-- Job runs: jobs_per_server_user задач на пару (сервер × пользователь)
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
        ((s.n - 1) * (p.admin_users + p.operator_users + p.guest_users) * p.jobs_per_server_user
            + (u.n - 1) * p.jobs_per_server_user
            + j.n)::text,
        12, '0'))::uuid,
    CASE j.n
        WHEN 1 THEN 2
        ELSE CASE (s.n + u.n) % 4
            WHEN 0 THEN 0
            WHEN 1 THEN 2
            WHEN 2 THEN 3
            ELSE 2
        END
    END,
    CASE
        WHEN j.n = 1 OR (s.n + u.n) % 4 IN (1, 3) THEN '{"success":true}'
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
    NOW() - interval '3 days'
        + ((s.n - 1) * (p.admin_users + p.operator_users + p.guest_users) * p.jobs_per_server_user
            + (u.n - 1) * p.jobs_per_server_user + j.n) * interval '1 minute',
    CASE
        WHEN j.n = 1 OR (s.n + u.n) % 4 <> 0 THEN
            NOW() - interval '3 days'
                + ((s.n - 1) * (p.admin_users + p.operator_users + p.guest_users) * p.jobs_per_server_user
                    + (u.n - 1) * p.jobs_per_server_user + j.n) * interval '1 minute'
                + interval '5 seconds'
        ELSE NULL
    END,
    CASE
        WHEN j.n = 1 OR (s.n + u.n) % 4 IN (1, 3) THEN
            NOW() - interval '3 days'
                + ((s.n - 1) * (p.admin_users + p.operator_users + p.guest_users) * p.jobs_per_server_user
                    + (u.n - 1) * p.jobs_per_server_user + j.n) * interval '1 minute'
                + interval '30 seconds'
        WHEN (s.n + u.n) % 4 = 2 THEN
            NOW() - interval '3 days'
                + ((s.n - 1) * (p.admin_users + p.operator_users + p.guest_users) * p.jobs_per_server_user
                    + (u.n - 1) * p.jobs_per_server_user + j.n) * interval '1 minute'
                + interval '20 seconds'
        ELSE NULL
    END,
    CASE j.n
        WHEN 1 THEN 'c0000001-0000-4000-8000-000000000001'::uuid
        ELSE ('c0000001-0000-4000-8000-' ||
            lpad((p.common_script_count + s.n)::text, 12, '0'))::uuid
    END,
    ('d0000001-0000-4000-8000-' || lpad(s.n::text, 12, '0'))::uuid,
    ('a0000001-0000-4000-8000-' || lpad(u.n::text, 12, '0'))::uuid,
    1000000 + u.n
FROM seed_params p
CROSS JOIN generate_series(1, p.server_count) AS s(n)
CROSS JOIN generate_series(1, p.admin_users + p.operator_users + p.guest_users) AS u(n)
CROSS JOIN generate_series(1, p.jobs_per_server_user) AS j(n);

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
