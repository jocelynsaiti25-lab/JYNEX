-- =====================================================================
--  ETBOTFARMA WEB - Script de creación de la base de datos (MySQL 8.0.16+)
--  Basado en las historias de usuario HU-01 a HU-15
--
--  Tablas:
--    1. rol                    Roles del sistema (Administrador, Empleado)
--    2. usuario                Cuentas del personal                    HU-01, 03, 04, 05
--    3. sesion                 Sesiones activas / cerradas             HU-01, 02, 05
--    4. medicamento            Catálogo de medicamentos                HU-06, 07, 08, 13, 14
--    5. lote                   Lotes con caducidad y existencias       HU-06, 09, 11, 12
--    6. movimiento_inventario  Encabezado de cada entrada/salida/baja  HU-10, 11, 12, 15
--    7. movimiento_detalle     Lote(s) afectados por cada movimiento   HU-10, 11 (FEFO), 12
--    8. auditoria              Cambios administrativos                 HU-03, 05, 08, 13
--
--  Vistas (solo lectura, para las consultas):
--    vw_inventario_lotes       Semáforo y CADUCADO por lote            HU-07, 09
--    vw_existencias_medicamento Existencias vigentes, AGOTADO, stock bajo HU-13, 14
--    vw_bitacora               Bitácora lista para mostrar/exportar   HU-10, 15
--
--  Convenciones:
--    - Nombres en snake_case y en singular.
--    - Todas las fechas/horas se guardan en UTC.
--    - Nada se borra físicamente: se usa "activo" o existencias = 0
--      para conservar el histórico (HU-10 criterio 6, HU-12 criterio 6).
-- =====================================================================

CREATE DATABASE IF NOT EXISTS etbotfarma
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_0900_ai_ci;

USE etbotfarma;

SET time_zone = '+00:00';


-- =====================================================================
-- 1. ROL
-- Catálogo de roles. Se usa tabla (y no texto libre) para que un usuario
-- solo pueda tener un rol que exista y para poder agregar permisos después.
-- =====================================================================
CREATE TABLE IF NOT EXISTS rol (
  id           TINYINT UNSIGNED NOT NULL,
  nombre       VARCHAR(30)      NOT NULL,
  descripcion  VARCHAR(150)     NULL,

  CONSTRAINT pk_rol        PRIMARY KEY (id),
  CONSTRAINT uq_rol_nombre UNIQUE (nombre)
) ENGINE = InnoDB;


-- =====================================================================
-- 2. USUARIO
-- Cuentas del personal. La contraseña NUNCA se guarda en texto plano:
-- solo su hash (HU-01 criterio 2, HU-03 criterio 5).
-- =====================================================================
CREATE TABLE IF NOT EXISTS usuario (
  id                   INT UNSIGNED     NOT NULL AUTO_INCREMENT,
  nombre_completo      VARCHAR(120)     NOT NULL,
  nombre_usuario       VARCHAR(50)      NOT NULL,
  telefono             VARCHAR(10)      NOT NULL,
  password_hash        VARCHAR(255)     NOT NULL,
  rol_id               TINYINT UNSIGNED NOT NULL,
  activo               BOOLEAN          NOT NULL DEFAULT TRUE,
  fecha_creacion       DATETIME         NOT NULL DEFAULT CURRENT_TIMESTAMP,
  fecha_actualizacion  DATETIME         NULL     ON UPDATE CURRENT_TIMESTAMP,

  CONSTRAINT pk_usuario                PRIMARY KEY (id),
  -- HU-03: el nombre de usuario es único (la intercalación _ai_ci hace que
  -- "Ana", "ana" y "ána" cuenten como el mismo usuario)
  CONSTRAINT uq_usuario_nombre_usuario UNIQUE (nombre_usuario),
  CONSTRAINT fk_usuario_rol            FOREIGN KEY (rol_id) REFERENCES rol (id)
                                       ON UPDATE RESTRICT ON DELETE RESTRICT,
  -- HU-03: formato de teléfono (10 dígitos, México) -> confirmar con el equipo
  CONSTRAINT ck_usuario_telefono       CHECK (telefono REGEXP '^[0-9]{10}$'),

  -- HU-04: búsqueda y listado por nombre
  INDEX ix_usuario_nombre_completo (nombre_completo)
) ENGINE = InnoDB;


-- =====================================================================
-- 3. SESION
-- Una fila por cada inicio de sesión. Permite invalidar la sesión en el
-- servidor (HU-02), expirarla por inactividad (HU-01 criterio 6) y cerrar
-- todas las sesiones de un usuario al reasignar su contraseña (HU-05).
-- Se guarda el HASH del token de la cookie, no el token.
-- =====================================================================
CREATE TABLE IF NOT EXISTS sesion (
  id                BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  usuario_id        INT UNSIGNED    NOT NULL,
  token_hash        CHAR(64)        NOT NULL,          -- SHA-256 en hexadecimal
  fecha_inicio      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
  ultima_actividad  DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
  revocada          BOOLEAN         NOT NULL DEFAULT FALSE,
  fecha_cierre      DATETIME        NULL,
  motivo_cierre     ENUM('LOGOUT', 'EXPIRADA', 'CAMBIO_PASSWORD', 'CUENTA_DESACTIVADA') NULL,
  ip                VARCHAR(45)     NULL,              -- 45 = longitud máxima de IPv6
  user_agent        VARCHAR(255)    NULL,

  CONSTRAINT pk_sesion            PRIMARY KEY (id),
  CONSTRAINT uq_sesion_token_hash UNIQUE (token_hash),
  CONSTRAINT fk_sesion_usuario    FOREIGN KEY (usuario_id) REFERENCES usuario (id)
                                  ON UPDATE RESTRICT ON DELETE RESTRICT,
  CONSTRAINT ck_sesion_cierre     CHECK (revocada = FALSE OR fecha_cierre IS NOT NULL),

  -- HU-05: buscar rápido las sesiones abiertas de un usuario para revocarlas
  INDEX ix_sesion_usuario_revocada (usuario_id, revocada)
) ENGINE = InnoDB;


-- =====================================================================
-- 4. MEDICAMENTO
-- Catálogo: datos DESCRIPTIVOS del medicamento. Las existencias NO están
-- aquí, están en cada lote (HU-08 criterio 3 y 4).
-- =====================================================================
CREATE TABLE IF NOT EXISTS medicamento (
  id                   INT UNSIGNED  NOT NULL AUTO_INCREMENT,
  nombre_comercial     VARCHAR(120)  NOT NULL,
  sustancia_activa     VARCHAR(120)  NOT NULL,
  presentacion         VARCHAR(80)   NOT NULL,        -- tabletas, jarabe, ampolleta...
  concentracion        VARCHAR(50)   NOT NULL,        -- 500 mg, 250 mg/5 ml...
  es_controlado        BOOLEAN       NOT NULL DEFAULT FALSE,
  stock_minimo         INT           NOT NULL DEFAULT 0,  -- HU-13
  activo               BOOLEAN       NOT NULL DEFAULT TRUE,
  fecha_creacion       DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
  fecha_actualizacion  DATETIME      NULL     ON UPDATE CURRENT_TIMESTAMP,

  CONSTRAINT pk_medicamento           PRIMARY KEY (id),
  -- HU-06 criterio 3: regla de NO duplicidad del catálogo -> confirmar con el equipo
  CONSTRAINT uq_medicamento_identidad UNIQUE (nombre_comercial, sustancia_activa, presentacion, concentracion),
  CONSTRAINT ck_medicamento_stock_min CHECK (stock_minimo >= 0),

  -- HU-07: búsqueda por sustancia activa (la búsqueda por nombre comercial
  -- ya usa el índice único, porque nombre_comercial es su primera columna)
  INDEX ix_medicamento_sustancia (sustancia_activa)
) ENGINE = InnoDB;


-- =====================================================================
-- 5. LOTE
-- Cada lote de un medicamento: número, caducidad y existencias.
-- El color del semáforo NO se guarda: depende de la fecha de HOY y se
-- calcula al consultar (ver vw_inventario_lotes).
-- =====================================================================
CREATE TABLE IF NOT EXISTS lote (
  id                   INT UNSIGNED  NOT NULL AUTO_INCREMENT,
  medicamento_id       INT UNSIGNED  NOT NULL,
  numero_lote          VARCHAR(40)   NOT NULL,
  fecha_caducidad      DATE          NOT NULL,
  existencias          INT           NOT NULL DEFAULT 0,
  -- Concurrencia optimista: aumenta en cada cambio. Si dos usuarios intentan
  -- sacar/dar de baja el mismo lote al mismo tiempo, el segundo falla
  -- (HU-12 criterio 5: "evita retiros duplicados").
  version              INT UNSIGNED  NOT NULL DEFAULT 0,
  fecha_registro       DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
  fecha_actualizacion  DATETIME      NULL     ON UPDATE CURRENT_TIMESTAMP,

  CONSTRAINT pk_lote                 PRIMARY KEY (id),
  -- HU-06 criterio 3: no repetir la combinación medicamento-lote
  CONSTRAINT uq_lote_medicamento_num UNIQUE (medicamento_id, numero_lote),
  CONSTRAINT fk_lote_medicamento     FOREIGN KEY (medicamento_id) REFERENCES medicamento (id)
                                     ON UPDATE RESTRICT ON DELETE RESTRICT,
  -- HU-11 criterio 5: nunca existencias negativas
  CONSTRAINT ck_lote_existencias     CHECK (existencias >= 0),

  -- HU-11 / FEFO: lotes de un medicamento ordenados por caducidad (empate por id)
  INDEX ix_lote_fefo (medicamento_id, fecha_caducidad, id),
  -- HU-09: filtros del semáforo por rango de fechas
  INDEX ix_lote_caducidad (fecha_caducidad),
  -- HU-07: búsqueda por número de lote
  INDEX ix_lote_numero_lote (numero_lote)
) ENGINE = InnoDB;


-- =====================================================================
-- 6. MOVIMIENTO_INVENTARIO  (encabezado de la bitácora)
-- Una fila por cada operación: alta con existencias iniciales (ENTRADA),
-- salida (SALIDA) o baja de lote rojo (BAJA). Es de solo lectura una vez
-- creada (HU-10 criterio 3).
-- =====================================================================
CREATE TABLE IF NOT EXISTS movimiento_inventario (
  id              BIGINT UNSIGNED                    NOT NULL AUTO_INCREMENT,
  tipo            ENUM('ENTRADA', 'SALIDA', 'BAJA')  NOT NULL,
  medicamento_id  INT UNSIGNED                       NOT NULL,
  usuario_id      INT UNSIGNED                       NOT NULL,
  cantidad_total  INT                                NOT NULL,
  motivo          VARCHAR(255)                       NULL,
  fecha           DATETIME                           NOT NULL DEFAULT CURRENT_TIMESTAMP,

  CONSTRAINT pk_movimiento             PRIMARY KEY (id),
  CONSTRAINT fk_movimiento_medicamento FOREIGN KEY (medicamento_id) REFERENCES medicamento (id)
                                       ON UPDATE RESTRICT ON DELETE RESTRICT,
  CONSTRAINT fk_movimiento_usuario     FOREIGN KEY (usuario_id) REFERENCES usuario (id)
                                       ON UPDATE RESTRICT ON DELETE RESTRICT,
  CONSTRAINT ck_movimiento_cantidad    CHECK (cantidad_total > 0),
  -- HU-12 criterio 3: la baja exige motivo
  CONSTRAINT ck_movimiento_motivo_baja CHECK (tipo <> 'BAJA' OR (motivo IS NOT NULL AND CHAR_LENGTH(TRIM(motivo)) > 0)),

  -- HU-10 criterio 4 y 5: filtros por fecha, medicamento, usuario y tipo,
  -- siempre ordenados del más reciente al más antiguo
  INDEX ix_movimiento_fecha (fecha),
  INDEX ix_movimiento_medicamento_fecha (medicamento_id, fecha),
  INDEX ix_movimiento_usuario_fecha (usuario_id, fecha),
  INDEX ix_movimiento_tipo_fecha (tipo, fecha)
) ENGINE = InnoDB;


-- =====================================================================
-- 7. MOVIMIENTO_DETALLE
-- Qué lote(s) afectó cada movimiento y cuántas unidades de cada uno.
-- Una salida FEFO puede tomar unidades de VARIOS lotes en UNA sola
-- operación (HU-11): 1 encabezado + N detalles, sin duplicar la salida.
-- La suma de "cantidad" debe ser igual a movimiento_inventario.cantidad_total
-- (lo garantiza la aplicación dentro de la misma transacción).
-- =====================================================================
CREATE TABLE IF NOT EXISTS movimiento_detalle (
  id             BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  movimiento_id  BIGINT UNSIGNED NOT NULL,
  lote_id        INT UNSIGNED    NOT NULL,
  cantidad       INT             NOT NULL,

  CONSTRAINT pk_movimiento_detalle       PRIMARY KEY (id),
  CONSTRAINT uq_movimiento_detalle_lote  UNIQUE (movimiento_id, lote_id),
  CONSTRAINT fk_detalle_movimiento       FOREIGN KEY (movimiento_id) REFERENCES movimiento_inventario (id)
                                         ON UPDATE RESTRICT ON DELETE RESTRICT,
  CONSTRAINT fk_detalle_lote             FOREIGN KEY (lote_id) REFERENCES lote (id)
                                         ON UPDATE RESTRICT ON DELETE RESTRICT,
  CONSTRAINT ck_detalle_cantidad         CHECK (cantidad > 0),

  -- Historial de un lote (bitácora por lote)
  INDEX ix_detalle_lote (lote_id)
) ENGINE = InnoDB;


-- =====================================================================
-- 8. AUDITORIA
-- Cambios ADMINISTRATIVOS que no son movimientos de inventario
-- ("Bitácora vs auditoría" en las notas de alcance):
--   CREAR_USUARIO        HU-03
--   REASIGNAR_PASSWORD   HU-05  (NUNCA guardar contraseñas ni hashes aquí)
--   EDITAR_MEDICAMENTO   HU-08  (valores antes / después)
--   CAMBIAR_STOCK_MINIMO HU-13
-- =====================================================================
CREATE TABLE IF NOT EXISTS auditoria (
  id                  BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  usuario_id          INT UNSIGNED    NOT NULL,     -- quién hizo el cambio
  accion              VARCHAR(50)     NOT NULL,
  entidad             VARCHAR(50)     NOT NULL,     -- 'usuario', 'medicamento'
  entidad_id          BIGINT UNSIGNED NOT NULL,     -- id del registro afectado
  valores_anteriores  JSON            NULL,
  valores_nuevos      JSON            NULL,
  fecha               DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,

  CONSTRAINT pk_auditoria         PRIMARY KEY (id),
  CONSTRAINT fk_auditoria_usuario FOREIGN KEY (usuario_id) REFERENCES usuario (id)
                                  ON UPDATE RESTRICT ON DELETE RESTRICT,

  INDEX ix_auditoria_entidad (entidad, entidad_id, fecha),
  INDEX ix_auditoria_usuario_fecha (usuario_id, fecha),
  INDEX ix_auditoria_fecha (fecha)
) ENGINE = InnoDB;


-- =====================================================================
-- VISTAS (consultas de solo lectura - lado "Query" de CQRS)
-- Nota: la regla del semáforo también vive en el Domain (C#). Si cambia,
-- hay que actualizar los dos lugares.
-- =====================================================================

-- HU-07 y HU-09: inventario por lote con semáforo y etiqueta CADUCADO
--   VERDE    caducidad a más de 12 meses
--   AMARILLO más de 3 meses y hasta 12 meses
--   ROJO     3 meses o menos, incluidos los ya vencidos (meses calendario)
CREATE OR REPLACE VIEW vw_inventario_lotes AS
SELECT
  l.id                AS lote_id,
  m.id                AS medicamento_id,
  m.nombre_comercial,
  m.sustancia_activa,
  m.presentacion,
  m.concentracion,
  m.es_controlado,
  l.numero_lote,
  l.fecha_caducidad,
  l.existencias,
  CASE
    WHEN l.fecha_caducidad <= DATE_ADD(CURDATE(), INTERVAL 3 MONTH)  THEN 'ROJO'
    WHEN l.fecha_caducidad <= DATE_ADD(CURDATE(), INTERVAL 12 MONTH) THEN 'AMARILLO'
    ELSE 'VERDE'
  END                 AS semaforo,
  (l.fecha_caducidad < CURDATE()) AS caducado
FROM lote l
JOIN medicamento m ON m.id = l.medicamento_id
WHERE m.activo = TRUE;

-- HU-13 y HU-14: existencias vigentes por medicamento, AGOTADO y stock bajo
-- (vigente = lote NO vencido; los lotes caducados no cuentan)
CREATE OR REPLACE VIEW vw_existencias_medicamento AS
SELECT
  t.medicamento_id,
  t.nombre_comercial,
  t.sustancia_activa,
  t.presentacion,
  t.concentracion,
  t.stock_minimo,
  t.existencias_vigentes,
  CASE
    WHEN t.existencias_vigentes = 0                 THEN 'AGOTADO'
    WHEN t.existencias_vigentes <= t.stock_minimo   THEN 'STOCK_BAJO'
    ELSE 'NORMAL'
  END AS estado_stock
FROM (
  SELECT
    m.id AS medicamento_id,
    m.nombre_comercial,
    m.sustancia_activa,
    m.presentacion,
    m.concentracion,
    m.stock_minimo,
    COALESCE(SUM(CASE WHEN l.fecha_caducidad >= CURDATE() THEN l.existencias ELSE 0 END), 0)
      AS existencias_vigentes
  FROM medicamento m
  LEFT JOIN lote l ON l.medicamento_id = m.id
  WHERE m.activo = TRUE
  GROUP BY m.id, m.nombre_comercial, m.sustancia_activa, m.presentacion, m.concentracion, m.stock_minimo
) AS t;

-- HU-10 y HU-15: una fila por lote afectado, lista para mostrar o exportar a Excel
CREATE OR REPLACE VIEW vw_bitacora AS
SELECT
  d.id               AS detalle_id,
  mv.id              AS movimiento_id,
  mv.fecha,
  u.id               AS usuario_id,
  u.nombre_usuario,
  u.nombre_completo  AS usuario_nombre,
  mv.tipo,
  m.id               AS medicamento_id,
  m.nombre_comercial,
  m.presentacion,
  m.concentracion,
  l.numero_lote,
  d.cantidad,
  mv.motivo
FROM movimiento_detalle d
JOIN movimiento_inventario mv ON mv.id = d.movimiento_id
JOIN usuario u                ON u.id  = mv.usuario_id
JOIN medicamento m            ON m.id  = mv.medicamento_id
JOIN lote l                   ON l.id  = d.lote_id;


-- =====================================================================
-- DATOS INICIALES
-- El usuario administrador inicial NO se inserta aquí: su contraseña debe
-- generarse con el mismo algoritmo de hash que usa la API, así que la API
-- lo creará al arrancar por primera vez (se implementa en HU-01).
-- =====================================================================
INSERT INTO rol (id, nombre, descripcion) VALUES
  (1, 'Administrador', 'Acceso total: usuarios, catálogo e inventario'),
  (2, 'Empleado',      'Operación diaria del inventario')
ON DUPLICATE KEY UPDATE descripcion = VALUES(descripcion);
