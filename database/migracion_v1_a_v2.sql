-- =====================================================================
--  ETBOTFARMA WEB - Migración de la base de datos: versión 1 → versión 2
--
--  Úsalo SOLO si ya tienes la base creada con database/etbotfarma_mysql.sql
--  (tu computadora, tu Codespace). Conserva todos los datos.
--  Si vas a crear la base desde cero, usa database/etbotfarma_mysql_v2.sql.
--
--  Ejecútalo UNA sola vez:
--      mysql -u root -p < database/migracion_v1_a_v2.sql
--  Si algo falla, MySQL se detiene en ese paso; revisa el mensaje antes de repetir.
-- =====================================================================

USE etbotfarma;

-- ---------------------------------------------------------------------
-- 1. SESION
--    a) token → token_hash (ya guardaba el hash SHA-256; ahora el nombre lo dice)
--    b) nueva columna fecha_expiracion = última actividad + 20 minutos
--    c) se elimina "cerrada": una sesión está cerrada si tiene fecha_cierre
-- ---------------------------------------------------------------------
ALTER TABLE sesion CHANGE COLUMN token token_hash CHAR(64) NOT NULL;

ALTER TABLE sesion ADD COLUMN fecha_expiracion DATETIME NULL AFTER ultima_actividad;
UPDATE sesion SET fecha_expiracion = ultima_actividad + INTERVAL 20 MINUTE;
ALTER TABLE sesion MODIFY fecha_expiracion DATETIME NOT NULL;

-- Las sesiones marcadas como cerradas sin fecha de cierre reciben la de su última actividad
UPDATE sesion SET fecha_cierre = ultima_actividad WHERE cerrada = TRUE AND fecha_cierre IS NULL;
ALTER TABLE sesion DROP COLUMN cerrada;

ALTER TABLE sesion
    ADD CONSTRAINT chk_sesion_expiracion CHECK (fecha_expiracion > fecha_inicio),
    ADD CONSTRAINT chk_sesion_cierre CHECK (fecha_cierre IS NULL OR fecha_cierre >= fecha_inicio);

-- ---------------------------------------------------------------------
-- 2. LOTE: índice FEFO y llave única para la validación del detalle
-- ---------------------------------------------------------------------
ALTER TABLE lote
    ADD UNIQUE uq_lote_medicamento (id_lote, id_medicamento),
    ADD INDEX idx_lote_fefo (id_medicamento, fecha_caducidad, fecha_entrada);

-- ---------------------------------------------------------------------
-- 3. MOVIMIENTO: fecha + hora → un solo DATETIME, tipos permitidos, índices
-- ---------------------------------------------------------------------
ALTER TABLE movimiento ADD COLUMN fecha_hora DATETIME NULL AFTER motivo;
UPDATE movimiento SET fecha_hora = TIMESTAMP(fecha, hora);
ALTER TABLE movimiento DROP INDEX idx_fecha, DROP INDEX idx_tipo;
ALTER TABLE movimiento DROP COLUMN hora, DROP COLUMN fecha;
ALTER TABLE movimiento CHANGE COLUMN fecha_hora fecha DATETIME NOT NULL;

ALTER TABLE movimiento
    ADD UNIQUE uq_movimiento_medicamento (id_movimiento, id_medicamento),
    ADD INDEX idx_movimiento_fecha (fecha),
    ADD INDEX idx_movimiento_tipo_fecha (tipo, fecha),
    ADD INDEX idx_movimiento_medicamento_fecha (id_medicamento, fecha),
    ADD INDEX idx_movimiento_usuario_fecha (id_usuario, fecha),
    ADD CONSTRAINT chk_movimiento_tipo CHECK (tipo IN ('ENTRADA', 'SALIDA', 'BAJA')),
    ADD CONSTRAINT chk_movimiento_motivo_baja CHECK (tipo <> 'BAJA' OR (motivo IS NOT NULL AND TRIM(motivo) <> ''));

-- ---------------------------------------------------------------------
-- 4. DETALLE_MOVIMIENTO: el lote debe ser del mismo medicamento del movimiento
--    Se agrega id_medicamento (copiado del movimiento) y dos llaves compuestas.
--    Si algún detalle viejo usa un lote de OTRO medicamento, el paso de las
--    llaves fallará: esos datos están mal y hay que corregirlos a mano.
-- ---------------------------------------------------------------------
ALTER TABLE detalle_movimiento ADD COLUMN id_medicamento INT NULL AFTER id_movimiento;
UPDATE detalle_movimiento d
    INNER JOIN movimiento m ON m.id_movimiento = d.id_movimiento
    SET d.id_medicamento = m.id_medicamento;
ALTER TABLE detalle_movimiento MODIFY id_medicamento INT NOT NULL;

ALTER TABLE detalle_movimiento
    ADD UNIQUE uq_detalle_movimiento_lote (id_movimiento, id_lote),
    ADD INDEX idx_detalle_lote (id_lote, id_medicamento),
    ADD CONSTRAINT fk_detalle_movimiento_medicamento FOREIGN KEY (id_movimiento, id_medicamento)
        REFERENCES movimiento(id_movimiento, id_medicamento) ON DELETE RESTRICT ON UPDATE RESTRICT,
    ADD CONSTRAINT fk_detalle_lote_medicamento FOREIGN KEY (id_lote, id_medicamento)
        REFERENCES lote(id_lote, id_medicamento) ON DELETE RESTRICT ON UPDATE RESTRICT;

-- ---------------------------------------------------------------------
-- 5. AUDITORIA: fecha + hora → un solo DATETIME, índices
-- ---------------------------------------------------------------------
ALTER TABLE auditoria ADD COLUMN fecha_hora DATETIME NULL AFTER valor_nuevo;
UPDATE auditoria SET fecha_hora = TIMESTAMP(fecha, hora);
ALTER TABLE auditoria DROP INDEX idx_fecha;
ALTER TABLE auditoria DROP COLUMN hora, DROP COLUMN fecha;
ALTER TABLE auditoria CHANGE COLUMN fecha_hora fecha DATETIME NOT NULL;

ALTER TABLE auditoria
    ADD INDEX idx_auditoria_fecha (fecha),
    ADD INDEX idx_auditoria_registro (tabla_afectada, id_registro),
    ADD INDEX idx_auditoria_usuario_fecha (id_usuario, fecha);

-- ---------------------------------------------------------------------
-- 6. USUARIO: índice del rol con nombre claro (la llave foránea ya lo tenía)
-- ---------------------------------------------------------------------
-- No se requiere cambio: MySQL ya creó un índice para la llave foránea id_rol.

SELECT 'Migración a la versión 2 terminada' AS resultado;
