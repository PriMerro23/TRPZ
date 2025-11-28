-- Скрипт ініціалізації бази даних PostgreSQL
-- Схема бази даних додатку Archiver

-- Створити базу даних (запустити це окремо, якщо база даних не існує)
-- CREATE DATABASE archiver_db WITH ENCODING 'UTF8';

-- Підключитися до бази даних
-- \c archiver_db;

-- Створити таблицю архівів
CREATE TABLE IF NOT EXISTS archives (
    id SERIAL PRIMARY KEY,
    file_path VARCHAR(500) NOT NULL,
    archive_type VARCHAR(50) NOT NULL,
    checksum VARCHAR(64),
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    modified_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Створити індекс для file_path для швидшого пошуку
CREATE INDEX IF NOT EXISTS idx_archives_file_path ON archives(file_path);
CREATE INDEX IF NOT EXISTS idx_archives_checksum ON archives(checksum);

-- Створити таблицю записів
CREATE TABLE IF NOT EXISTS entries (
    id SERIAL PRIMARY KEY,
    archive_id INTEGER NOT NULL,
    file_name VARCHAR(500) NOT NULL,
    file_size BIGINT NOT NULL,
    modified_date TIMESTAMP NOT NULL,
    is_directory BOOLEAN NOT NULL DEFAULT FALSE,
    FOREIGN KEY (archive_id) REFERENCES archives(id) ON DELETE CASCADE
);

-- Створити індекс для archive_id для швидшого об'єднання
CREATE INDEX IF NOT EXISTS idx_entries_archive_id ON entries(archive_id);

-- Створити таблицю операцій (журнал аудиту)
CREATE TABLE IF NOT EXISTS operations (
    id SERIAL PRIMARY KEY,
    archive_id INTEGER,
    operation_type VARCHAR(50) NOT NULL,
    timestamp TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    result VARCHAR(50) NOT NULL,
    metadata TEXT,
    FOREIGN KEY (archive_id) REFERENCES archives(id) ON DELETE SET NULL
);

-- Створити індекси для таблиці операцій
CREATE INDEX IF NOT EXISTS idx_operations_archive_id ON operations(archive_id);
CREATE INDEX IF NOT EXISTS idx_operations_timestamp ON operations(timestamp DESC);
CREATE INDEX IF NOT EXISTS idx_operations_type ON operations(operation_type);

-- Надати права доступу (налаштуйте ім'я користувача за потреби)
-- GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO postgres;
-- GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO postgres;

-- Відобразити інформацію про таблиці
SELECT 
    table_name,
    (SELECT COUNT(*) FROM information_schema.columns WHERE table_name = t.table_name) as column_count
FROM information_schema.tables t
WHERE table_schema = 'public' AND table_type = 'BASE TABLE'
ORDER BY table_name;
