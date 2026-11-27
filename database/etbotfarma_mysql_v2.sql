-- =====================================================================
--  ETBOTFARMA WEB - Base de datos (MySQL 8) - VERSIÓN 2 (corregida)
--
--  Script para crear la base desde cero. Se puede ejecutar varias veces.
--  Para actualizar una base que ya existe (versión 1) usa:
--      database/migracion_v1_a_v2.sql
--  Explicación de cada cambio: docs/base-de-datos.md
--
--  Tablas:
--    1. rol                  Tipos de usuario (Administrador, Empleado)
--    2. usuario              Personal que usa el sistema
--    3. sesion               Inicios y cierres de sesión
--    4. medicamento          Catálogo de medicamentos
--    5. lote                 Lotes de cada medicamento (caducidad y stock)
--    6. movimiento           Entradas, salidas y bajas (bitácora)
--    7. detalle_movimiento   Qué lotes se usaron en cada movimiento
--    8. auditoria            Cambios importantes hechos por los usuarios
--
--  Todas las fechas se guardan en UTC.
-- =====================================================================

CREATE DATABASE IF NOT EXISTS etbotfarma
    DEFAULT CHARACTER SET utf8mb4
    DEFAULT COLLATE utf8mb4_spanish2_ci;

USE etbotfarma;

-- =====================================================================
-- 1. ROL
-- Tipos de usuario. Un usuario solo puede tener un rol que exista aquí.
-- =====================================================================
CREATE TABLE IF NOT EXISTS rol (
    id_rol INT PRIMARY KEY AUTO_INCREMENT NOT NULL,
    nombre_rol VARCHAR(30) NOT NULL UNIQUE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_spanish2_ci;

-- =====================================================================
-- 2. USUARIO
-- La contraseña se guarda ENCRIPTADA (hash), nunca como texto normal.
-- No se borra: se desactiva con activo = FALSE.
-- nombre_usuario: con él se inicia sesión → no se repite.
-- correo:         identifica a la persona (HU-03) → no se repite.
-- telefono:       solo dato de contacto → SÍ se puede repetir.
-- =====================================================================
CREATE TABLE IF NOT EXISTS usuario (
    id_usuario INT PRIMARY KEY AUTO_INCREMENT NOT NULL,
    nombre VARCHAR(50) NOT NULL,
    apellidos VARCHAR(80) NOT NULL,
    nombre_usuario VARCHAR(50) NOT NULL UNIQUE,
    correo VARCHAR(100) NOT NULL UNIQUE,
    contrasenia_hash VARCHAR(255) NOT NULL,
    telefono VARCHAR(15) NOT NULL,
    id_rol INT NOT NULL,
    activo BOOLEAN NOT NULL DEFAULT TRUE,
    fecha_registro DATETIME NOT NULL,
    INDEX idx_usuario_rol (id_rol),
    FOREIGN KEY (id_rol) REFERENCES rol(id_rol) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_spanish2_ci;

-- =====================================================================
-- 3. SESION
-- token_hash:       hash SHA-256 del token de la cookie (el token real NUNCA se guarda).
-- fecha_expiracion: hasta cuándo es válida; se recorre 20 minutos con cada actividad.
-- fecha_cierre:     NULL = sesión abierta; con fecha = sesión cerrada.
--                   (Reemplaza a la columna "cerrada", que repetía la misma información.)
-- =====================================================================
CREATE TABLE IF NOT EXISTS sesion (
    id_sesion INT PRIMARY KEY AUTO_INCREMENT NOT NULL,
    id_usuario INT NOT NULL,
    token_hash CHAR(64) NOT NULL UNIQUE,
    fecha_inicio DATETIME NOT NULL,
    ultima_actividad DATETIME NOT NULL,
    fecha_expiracion DATETIME NOT NULL,
    fecha_cierre DATETIME NULL,
    INDEX idx_sesion_usuario (id_usuario),
    CHECK (fecha_expiracion > fecha_inicio),
    CHECK (fecha_cierre IS NULL OR fecha_cierre >= fecha_inicio),
    FOREIGN KEY (id_usuario) REFERENCES usuario(id_usuario) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_spanish2_ci;

-- =====================================================================
-- 4. MEDICAMENTO
-- Datos generales. El stock NO va aquí: se calcula sumando los lotes.
-- Identidad del medicamento: nombre comercial + sustancia + presentación + concentración.
-- =====================================================================
CREATE TABLE IF NOT EXISTS medicamento (
    id_medicamento INT PRIMARY KEY AUTO_INCREMENT NOT NULL,
    nombre_comercial VARCHAR(100) NOT NULL,
    nombre_sustancia VARCHAR(100) NOT NULL,
    presentacion VARCHAR(50) NOT NULL,
    concentracion VARCHAR(50) NOT NULL,
    es_controlado BOOLEAN NOT NULL DEFAULT FALSE,
    stock_minimo INT NOT NULL DEFAULT 0,
    activo BOOLEAN NOT NULL DEFAULT TRUE,
    fecha_registro DATETIME NOT NULL,
    UNIQUE (nombre_comercial, nombre_sustancia, presentacion, concentracion),
    INDEX idx_nombre_sustancia (nombre_sustancia),
    CHECK (stock_minimo >= 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_spanish2_ci;

-- =====================================================================
-- 5. LOTE
-- Cada lote de un medicamento con su caducidad y su stock (nunca negativo).
-- idx_lote_fefo: busca rápido los lotes de un medicamento en orden FEFO
--                (primero el que caduca antes; si empatan, el que entró primero).
-- UNIQUE (id_lote, id_medicamento): permite que detalle_movimiento compruebe
--                que el lote es del mismo medicamento del movimiento.
-- =====================================================================
CREATE TABLE IF NOT EXISTS lote (
    id_lote INT PRIMARY KEY AUTO_INCREMENT NOT NULL,
    id_medicamento INT NOT NULL,
    numero_lote VARCHAR(50) NOT NULL,
    fecha_caducidad DATE NOT NULL,
    stock INT NOT NULL DEFAULT 0,
    fecha_entrada DATE NOT NULL,
    UNIQUE (id_medicamento, numero_lote),
    UNIQUE uq_lote_medicamento (id_lote, id_medicamento),
    INDEX idx_lote_fefo (id_medicamento, fecha_caducidad, fecha_entrada),
    INDEX idx_fecha_caducidad (fecha_caducidad),
    INDEX idx_numero_lote (numero_lote),
    CHECK (stock >= 0),
    FOREIGN KEY (id_medicamento) REFERENCES medicamento(id_medicamento) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_spanish2_ci;

-- =====================================================================
-- 6. MOVIMIENTO
-- Bitácora: cada ENTRADA, SALIDA o BAJA. Nunca se borra.
-- fecha: fecha y hora juntas (antes eran dos columnas: fecha y hora).
-- Una BAJA siempre lleva motivo.
-- =====================================================================
CREATE TABLE IF NOT EXISTS movimiento (
    id_movimiento INT PRIMARY KEY AUTO_INCREMENT NOT NULL,
    tipo VARCHAR(20) NOT NULL,
    id_medicamento INT NOT NULL,
    id_usuario INT NOT NULL,
    cantidad_total INT NOT NULL,
    motivo VARCHAR(200) NULL,
    fecha DATETIME NOT NULL,
    UNIQUE uq_movimiento_medicamento (id_movimiento, id_medicamento),
    INDEX idx_movimiento_fecha (fecha),
    INDEX idx_movimiento_tipo_fecha (tipo, fecha),
    INDEX idx_movimiento_medicamento_fecha (id_medicamento, fecha),
    INDEX idx_movimiento_usuario_fecha (id_usuario, fecha),
    CHECK (tipo IN ('ENTRADA', 'SALIDA', 'BAJA')),
    CHECK (cantidad_total > 0),
    CHECK (tipo <> 'BAJA' OR (motivo IS NOT NULL AND TRIM(motivo) <> '')),
    FOREIGN KEY (id_medicamento) REFERENCES medicamento(id_medicamento) ON DELETE RESTRICT ON UPDATE CASCADE,
    FOREIGN KEY (id_usuario) REFERENCES usuario(id_usuario) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_spanish2_ci;

-- =====================================================================
-- 7. DETALLE_MOVIMIENTO
-- Cuánto se usó de cada lote en un movimiento (una salida puede usar varios lotes).
-- id_medicamento + las dos llaves foráneas compuestas GARANTIZAN que el lote
-- pertenece al mismo medicamento del movimiento (MySQL rechaza cualquier otro).
-- La suma de cantidades = cantidad_total del movimiento: lo garantiza el backend
-- dentro de la misma transacción.
-- =====================================================================
CREATE TABLE IF NOT EXISTS detalle_movimiento (
    id_detalle INT PRIMARY KEY AUTO_INCREMENT NOT NULL,
    id_movimiento INT NOT NULL,
    id_medicamento INT NOT NULL,
    id_lote INT NOT NULL,
    cantidad INT NOT NULL,
    UNIQUE (id_movimiento, id_lote),
    INDEX idx_detalle_lote (id_lote, id_medicamento),
    CHECK (cantidad > 0),
    FOREIGN KEY (id_movimiento, id_medicamento)
        REFERENCES movimiento(id_movimiento, id_medicamento) ON DELETE RESTRICT ON UPDATE RESTRICT,
    FOREIGN KEY (id_lote, id_medicamento)
        REFERENCES lote(id_lote, id_medicamento) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_spanish2_ci;

-- =====================================================================
-- 8. AUDITORIA
-- Cambios importantes en usuarios, medicamentos, lotes y movimientos.
-- NUNCA guardar contraseñas, hashes ni tokens en valor_anterior / valor_nuevo.
-- fecha: fecha y hora juntas (antes eran dos columnas).
-- =====================================================================
CREATE TABLE IF NOT EXISTS auditoria (
    id_auditoria INT PRIMARY KEY AUTO_INCREMENT NOT NULL,
    id_usuario INT NOT NULL,
    accion VARCHAR(50) NOT NULL,
    tabla_afectada VARCHAR(50) NOT NULL,
    id_registro INT NOT NULL,
    valor_anterior TEXT NULL,
    valor_nuevo TEXT NULL,
    fecha DATETIME NOT NULL,
    INDEX idx_auditoria_fecha (fecha),
    INDEX idx_auditoria_registro (tabla_afectada, id_registro),
    INDEX idx_auditoria_usuario_fecha (id_usuario, fecha),
    FOREIGN KEY (id_usuario) REFERENCES usuario(id_usuario) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_spanish2_ci;

-- =====================================================================
-- DATOS INICIALES: los dos roles del sistema
-- (El usuario administrador lo crea la API al arrancar por primera vez.)
-- =====================================================================
INSERT IGNORE INTO rol (id_rol, nombre_rol) VALUES
    (1, 'Administrador'),
    (2, 'Empleado');
