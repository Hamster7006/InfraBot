-- InfraBot PostgreSQL schema
-- psql -U postgres -d postgres -f SQL/InfraBotDb.sql

SELECT 'CREATE DATABASE infrabot ENCODING ''UTF8'''
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'infrabot')\gexec

\connect infrabot

CREATE TABLE IF NOT EXISTS bot_users
(
    id           UUID        NOT NULL PRIMARY KEY,
    telegram_id  BIGINT      NOT NULL,
    username     VARCHAR(1024),
    status       INTEGER     NOT NULL,
    pending      INTEGER     NOT NULL DEFAULT 0,
    created_at   TIMESTAMPTZ NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_bot_users_telegram_id
    ON bot_users (telegram_id);

CREATE TABLE IF NOT EXISTS svc_sam_accounts
(
    id               UUID         NOT NULL PRIMARY KEY,
    sam_account_name VARCHAR(256) NOT NULL,
    password         TEXT         NOT NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_svc_sam_accounts_name
    ON svc_sam_accounts (LOWER(sam_account_name));

CREATE TABLE IF NOT EXISTS scripts
(
    id              UUID         NOT NULL PRIMARY KEY,
    name            VARCHAR(256) NOT NULL,
    description     TEXT,
    content         TEXT         NOT NULL,
    return_data     BOOLEAN      NOT NULL DEFAULT FALSE,
    timeout_seconds INTEGER      NOT NULL DEFAULT 120,
    created_at      TIMESTAMPTZ  NOT NULL,
    created_by_id   UUID         NOT NULL,
    CONSTRAINT fk_scripts_created_by
        FOREIGN KEY (created_by_id) REFERENCES bot_users (id)
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_scripts_name
    ON scripts (LOWER(name));

CREATE TABLE IF NOT EXISTS servers
(
    id                    UUID         NOT NULL PRIMARY KEY,
    server_name           VARCHAR(256) NOT NULL,
    ip_address            VARCHAR(45)  NOT NULL,
    description           TEXT,
    registered_by_user_id UUID         NOT NULL,
    win_rm_port           INTEGER      NOT NULL DEFAULT 5986,
    svc_sam_account_id    UUID         NOT NULL,
    CONSTRAINT fk_servers_registered_by
        FOREIGN KEY (registered_by_user_id) REFERENCES bot_users (id),
    CONSTRAINT fk_servers_svc_account
        FOREIGN KEY (svc_sam_account_id) REFERENCES svc_sam_accounts (id)
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_servers_name
    ON servers (LOWER(server_name));

CREATE INDEX IF NOT EXISTS ix_servers_registered_by_user_id
    ON servers (registered_by_user_id);

CREATE INDEX IF NOT EXISTS ix_servers_svc_sam_account_id
    ON servers (svc_sam_account_id);

CREATE TABLE IF NOT EXISTS server_script_requirements
(
    server_id UUID NOT NULL,
    script_id UUID NOT NULL,
    PRIMARY KEY (server_id, script_id),
    CONSTRAINT fk_server_script_requirements_server
        FOREIGN KEY (server_id) REFERENCES servers (id) ON DELETE CASCADE,
    CONSTRAINT fk_server_script_requirements_script
        FOREIGN KEY (script_id) REFERENCES scripts (id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS ix_server_script_requirements_script_id
    ON server_script_requirements (script_id);

CREATE TABLE IF NOT EXISTS server_granted_users
(
    server_id UUID NOT NULL,
    user_id   UUID NOT NULL,
    PRIMARY KEY (server_id, user_id),
    CONSTRAINT fk_server_granted_users_server
        FOREIGN KEY (server_id) REFERENCES servers (id) ON DELETE CASCADE,
    CONSTRAINT fk_server_granted_users_user
        FOREIGN KEY (user_id) REFERENCES bot_users (id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS ix_server_granted_users_user_id
    ON server_granted_users (user_id);

CREATE TABLE IF NOT EXISTS job_runs
(
    id              UUID        NOT NULL PRIMARY KEY,
    status          INTEGER     NOT NULL,
    result_json     TEXT,
    error_message   TEXT,
    exit_code       INTEGER,
    created_at      TIMESTAMPTZ NOT NULL,
    started_at      TIMESTAMPTZ,
    finished_at     TIMESTAMPTZ,
    script_id       UUID        NOT NULL,
    server_id       UUID        NOT NULL,
    initiated_by_id UUID        NOT NULL,
    chat_id         BIGINT      NOT NULL,
    CONSTRAINT fk_job_runs_script
        FOREIGN KEY (script_id) REFERENCES scripts (id),
    CONSTRAINT fk_job_runs_server
        FOREIGN KEY (server_id) REFERENCES servers (id),
    CONSTRAINT fk_job_runs_initiated_by
        FOREIGN KEY (initiated_by_id) REFERENCES bot_users (id)
);

CREATE INDEX IF NOT EXISTS ix_job_runs_initiated_by_id
    ON job_runs (initiated_by_id);

CREATE INDEX IF NOT EXISTS ix_job_runs_server_id
    ON job_runs (server_id);

CREATE INDEX IF NOT EXISTS ix_job_runs_script_id
    ON job_runs (script_id);

CREATE INDEX IF NOT EXISTS ix_job_runs_status_created_at
    ON job_runs (status, created_at DESC);

-- Статические тестовые данные (опционально):
-- psql -U postgres -d infrabot -f SQL/InfraBotSeedData.sql
