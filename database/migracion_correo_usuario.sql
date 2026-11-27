-- =====================================================================
--  ETBOTFARMA WEB - Migración: agregar el CORREO ELECTRÓNICO al usuario (HU-03)
--
--  Úsalo SOLO si tu base ya está en la versión 2 y la tabla usuario
--  todavía NO tiene la columna correo (tu computadora, tu Codespace).
--  Si vas a crear la base desde cero, usa database/etbotfarma_mysql_v2.sql (ya la trae).
--
--  Ejecútalo UNA sola vez:
--      mysql -u root -p < database/migracion_correo_usuario.sql
--
--  ¿Por qué el correo?  Identifica a cada persona y evita registrarla dos veces.
--  El nombre y el teléfono SÍ se pueden repetir (dos personas pueden llamarse igual).
-- =====================================================================

USE etbotfarma;

-- 1. Se agrega la columna vacía (NULL) para no romper los usuarios que ya existen
ALTER TABLE usuario ADD COLUMN correo VARCHAR(100) NULL AFTER nombre_usuario;

-- 2. A los usuarios que ya existían se les pone un correo provisional ÚNICO
--    (nombre_usuario@pendiente.etbotfarma). Cámbialo después por su correo real.
UPDATE usuario SET correo = CONCAT(nombre_usuario, '@pendiente.etbotfarma') WHERE correo IS NULL;

-- 3. Ahora sí: obligatorio y sin repetirse
ALTER TABLE usuario MODIFY correo VARCHAR(100) NOT NULL;
ALTER TABLE usuario ADD UNIQUE uq_usuario_correo (correo);

SELECT 'Columna correo agregada a usuario' AS resultado;
