-- Folkie local Postgres bootstrap
-- Postgres docker'da ilk kez ayağa kalkarken çalışır.

-- Hangfire için ayrı bir DB
CREATE DATABASE folkie_hangfire OWNER folkie;
