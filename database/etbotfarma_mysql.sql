-- =====================================================================
--  ETBOTFARMA WEB - Base de datos (MySQL 8)
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
-- =====================================================================

CREATE DATABASE IF NOT EXISTS etbotfarma;
USE etbotfarma;


-- =====================================================================
-- 1. ROL
-- Guarda los tipos de usuario. Así un usuario solo puede tener
-- un rol que exista en esta tabla.
-- =====================================================================
CREATE TABLE IF NOT EXISTS rol (
    id_rol INT PRIMARY KEY AUTO_INCREMENT NOT NULL,
    nombre_rol VARCHAR(30) NOT NULL UNIQUE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_spanish2_ci;


-- =====================================================================
-- 2. USUARIO
-- Personal que entra al sistema.
-- La contraseña se guarda ENCRIPTADA (hash), nunca como texto normal.
-- =====================================================================
CREATE TABLE IF NOT EXISTS usuario (
    id_usuario INT PRIMARY KEY AUTO_INCREMENT NOT NULL,
    nombre VARCHAR(50) NOT NULL,
    apellidos VARCHAR(80) NOT NULL,
    nombre_usuario VARCHAR(50) NOT NULL UNIQUE,
    contrasenia_hash VARCHAR(255) NOT NULL,
    telefono VARCHAR(15) NOT NULL,
    id_rol INT NOT NULL,
    activo BOOLEAN NOT NULL DEFAULT TRUE,
    fecha_registro DATETIME NOT NULL,
    FOREIGN KEY (id_rol) REFERENCES rol(id_rol) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_spanish2_ci;


-- =====================================================================
-- 3. SESION
-- Cada vez que alguien inicia sesión se guarda una fila.
-- Al cerrar sesión se marca como cerrada.
-- =====================================================================
CREATE TABLE IF NOT EXISTS sesion (
    id_sesion INT PRIMARY KEY AUTO_INCREMENT NOT NULL,
    id_usuario INT NOT NULL,
    token VARCHAR(255) NOT NULL UNIQUE,
    fecha_inicio DATETIME NOT NULL,
    ultima_actividad DATETIME NOT NULL,
    fecha_cierre DATETIME NULL,
    cerrada BOOLEAN NOT NULL DEFAULT FALSE,
    FOREIGN KEY (id_usuario) REFERENCES usuario(id_usuario) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_spanish2_ci;


-- =====================================================================
-- 4. MEDICAMENTO
-- Datos generales del medicamento. El stock NO va aquí,
-- va en cada lote (un medicamento puede tener varios lotes).
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
-- Cada lote de un medicamento con su fecha de caducidad y su stock.
-- =====================================================================
CREATE TABLE IF NOT EXISTS lote (
    id_lote INT PRIMARY KEY AUTO_INCREMENT NOT NULL,
    id_medicamento INT NOT NULL,
    numero_lote VARCHAR(50) NOT NULL,
    fecha_caducidad DATE NOT NULL,
    stock INT NOT NULL DEFAULT 0,
    fecha_entrada DATE NOT NULL,
    UNIQUE (id_medicamento, numero_lote),
    INDEX idx_fecha_caducidad (fecha_caducidad),
    INDEX idx_numero_lote (numero_lote),
    CHECK (stock >= 0),
    FOREIGN KEY (id_medicamento) REFERENCES medicamento(id_medicamento) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_spanish2_ci;


-- =====================================================================
-- 6. MOVIMIENTO
-- Bitácora: cada entrada, salida o baja de medicamento.
-- tipo puede ser: 'ENTRADA', 'SALIDA' o 'BAJA'
-- =====================================================================
CREATE TABLE IF NOT EXISTS movimiento (
    id_movimiento INT PRIMARY KEY AUTO_INCREMENT NOT NULL,
    tipo VARCHAR(20) NOT NULL,
    id_medicamento INT NOT NULL,
    id_usuario INT NOT NULL,
    cantidad_total INT NOT NULL,
    motivo VARCHAR(200) NULL,
    fecha DATE NOT NULL,
    hora TIME NOT NULL,
    INDEX idx_fecha (fecha),
    INDEX idx_tipo (tipo),
    CHECK (cantidad_total > 0),
    FOREIGN KEY (id_medicamento) REFERENCES medicamento(id_medicamento) ON DELETE RESTRICT ON UPDATE CASCADE,
    FOREIGN KEY (id_usuario) REFERENCES usuario(id_usuario) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_spanish2_ci;


-- =====================================================================
-- 7. DETALLE_MOVIMIENTO
-- Una salida puede tomar medicamento de VARIOS lotes.
-- Aquí se guarda cuánto se tomó de cada lote.
-- =====================================================================
CREATE TABLE IF NOT EXISTS detalle_movimiento (
    id_detalle INT PRIMARY KEY AUTO_INCREMENT NOT NULL,
    id_movimiento INT NOT NULL,
    id_lote INT NOT NULL,
    cantidad INT NOT NULL,
    CHECK (cantidad > 0),
    FOREIGN KEY (id_movimiento) REFERENCES movimiento(id_movimiento) ON DELETE RESTRICT ON UPDATE CASCADE,
    FOREIGN KEY (id_lote) REFERENCES lote(id_lote) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_spanish2_ci;


-- =====================================================================
-- 8. AUDITORIA
-- Registra cambios importantes: crear usuarios, cambiar contraseñas,
-- editar medicamentos y cambiar el stock mínimo.
-- (Nunca guardar contraseñas aquí.)
-- =====================================================================
CREATE TABLE IF NOT EXISTS auditoria (
    id_auditoria INT PRIMARY KEY AUTO_INCREMENT NOT NULL,
    id_usuario INT NOT NULL,
    accion VARCHAR(50) NOT NULL,
    tabla_afectada VARCHAR(50) NOT NULL,
    id_registro INT NOT NULL,
    valor_anterior TEXT NULL,
    valor_nuevo TEXT NULL,
    fecha DATE NOT NULL,
    hora TIME NOT NULL,
    INDEX idx_fecha (fecha),
    FOREIGN KEY (id_usuario) REFERENCES usuario(id_usuario) ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_spanish2_ci;


-- =====================================================================
-- DATOS INICIALES: los dos roles del sistema
-- =====================================================================
INSERT IGNORE INTO rol (id_rol, nombre_rol) VALUES
    (1, 'Administrador'),
    (2, 'Empleado');
