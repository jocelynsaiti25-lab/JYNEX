-- =====================================================================
-- Pruebas de la base de datos ETBOTFARMA - versión 2 (se ejecutan en GitHub Actions)
-- Simulan lo que hará el backend en cada historia, dentro de transacciones.
-- Cada verificación imprime OK o FALLA.
--   mysql -u root -p -N < database/pruebas/pruebas_v2.sql
-- (Las operaciones que MySQL debe RECHAZAR están en reglas_que_deben_fallar_v2.sh)
-- =====================================================================
USE etbotfarma;

-- =====================================================================
-- 1. Registro de usuarios (HU-03)
-- =====================================================================
INSERT INTO usuario (nombre, apellidos, nombre_usuario, correo, contrasenia_hash, telefono, id_rol, fecha_registro)
VALUES ('Ana', 'López Pérez', 'ana.admin', 'ana.lopez@etbotfarma.com', 'AQAAAAIAAYagAAAAE-hash-de-prueba', '5512345678', 1, UTC_TIMESTAMP());
SET @admin = LAST_INSERT_ID();

INSERT INTO usuario (nombre, apellidos, nombre_usuario, correo, contrasenia_hash, telefono, id_rol, fecha_registro)
VALUES ('Luis', 'Martínez', 'luis.empleado', 'luis.martinez@etbotfarma.com', 'AQAAAAIAAYagAAAAE-otro-hash', '5587654321', 2, UTC_TIMESTAMP());
SET @empleado = LAST_INSERT_ID();

-- HU-03: dos personas con el MISMO nombre y el mismo teléfono se permiten (cambian su usuario y su correo)
INSERT INTO usuario (nombre, apellidos, nombre_usuario, correo, contrasenia_hash, telefono, id_rol, fecha_registro)
VALUES ('Luis', 'Martínez', 'luis.martinez2', 'luis.m.otro@etbotfarma.com', 'AQAAAAIAAYagAAAAE-tercer-hash', '5587654321', 2, UTC_TIMESTAMP());
SET @homonimo = LAST_INSERT_ID();

SELECT '1. Roles iniciales: 2', IF((SELECT COUNT(*) FROM rol) = 2, 'OK', 'FALLA');
SELECT '1. Usuarios registrados con su rol', IF((SELECT COUNT(*) FROM usuario u INNER JOIN rol r ON r.id_rol = u.id_rol
    WHERE u.id_usuario IN (@admin, @empleado)) = 2, 'OK', 'FALLA');
SELECT '1. Personas con el mismo nombre y teléfono se registran (HU-03)', IF((SELECT COUNT(*) FROM usuario
    WHERE nombre = 'Luis' AND apellidos = 'Martínez' AND telefono = '5587654321') = 2, 'OK', 'FALLA');

-- Sesión: se guarda el hash del token y la fecha de expiración (HU-01)
INSERT INTO sesion (id_usuario, token_hash, fecha_inicio, ultima_actividad, fecha_expiracion)
VALUES (@empleado, SHA2('token-de-prueba', 256), UTC_TIMESTAMP(), UTC_TIMESTAMP(), UTC_TIMESTAMP() + INTERVAL 20 MINUTE);
SELECT '1. Sesión abierta (fecha_cierre vacía) y vigente',
    IF((SELECT COUNT(*) FROM sesion WHERE id_usuario = @empleado AND fecha_cierre IS NULL
        AND fecha_expiracion > UTC_TIMESTAMP()) = 1, 'OK', 'FALLA');

-- =====================================================================
-- 2. Registro de medicamentos (HU-06)
-- =====================================================================
INSERT INTO medicamento (nombre_comercial, nombre_sustancia, presentacion, concentracion, stock_minimo, fecha_registro)
VALUES ('Tempra', 'Paracetamol', 'Tabletas', '500 mg', 20, UTC_TIMESTAMP());
SET @tempra = LAST_INSERT_ID();

INSERT INTO medicamento (nombre_comercial, nombre_sustancia, presentacion, concentracion, stock_minimo, fecha_registro)
VALUES ('Amoxil', 'Amoxicilina', 'Cápsulas', '500 mg', 5, UTC_TIMESTAMP());
SET @amoxil = LAST_INSERT_ID();

INSERT INTO medicamento (nombre_comercial, nombre_sustancia, presentacion, concentracion, es_controlado, stock_minimo, fecha_registro)
VALUES ('Ventolin', 'Salbutamol', 'Inhalador', '100 mcg', FALSE, 2, UTC_TIMESTAMP());
SET @ventolin = LAST_INSERT_ID();

SELECT '2. Medicamentos registrados: 3', IF((SELECT COUNT(*) FROM medicamento) = 3, 'OK', 'FALLA');

-- =====================================================================
-- 3. Registro de lotes
--    TEM-A: entró DESPUÉS pero caduca ANTES (2 meses)  → FEFO lo usa primero
--    TEM-B: entró ANTES pero caduca DESPUÉS (18 meses) → FIFO lo usaría primero (incorrecto)
--    TEM-C: ya caducado                                → nunca se usa en salidas
-- =====================================================================
INSERT INTO lote (id_medicamento, numero_lote, fecha_caducidad, stock, fecha_entrada) VALUES
    (@tempra, 'TEM-A', CURDATE() + INTERVAL 2 MONTH,  5, CURDATE() - INTERVAL 100 DAY),
    (@tempra, 'TEM-B', CURDATE() + INTERVAL 18 MONTH, 10, CURDATE() - INTERVAL 300 DAY),
    (@tempra, 'TEM-C', CURDATE() - INTERVAL 5 DAY,    4, CURDATE() - INTERVAL 400 DAY),
    (@amoxil, 'AMX-1', CURDATE() + INTERVAL 8 MONTH, 50, CURDATE() - INTERVAL 30 DAY),
    (@amoxil, 'AMX-2', CURDATE() + INTERVAL 1 MONTH,  3, CURDATE() - INTERVAL 60 DAY);

SET @tem_a = (SELECT id_lote FROM lote WHERE numero_lote = 'TEM-A');
SET @tem_b = (SELECT id_lote FROM lote WHERE numero_lote = 'TEM-B');
SET @tem_c = (SELECT id_lote FROM lote WHERE numero_lote = 'TEM-C');

SELECT '3. Lotes registrados: 5', IF((SELECT COUNT(*) FROM lote) = 5, 'OK', 'FALLA');

-- =====================================================================
-- 4. Entrada de inventario (HU-07): lote nuevo + movimiento ENTRADA + detalle, en UNA transacción
-- =====================================================================
START TRANSACTION;
    INSERT INTO lote (id_medicamento, numero_lote, fecha_caducidad, stock, fecha_entrada)
    VALUES (@tempra, 'TEM-D', CURDATE() + INTERVAL 24 MONTH, 0, CURDATE());
    SET @tem_d = LAST_INSERT_ID();

    UPDATE lote SET stock = stock + 30 WHERE id_lote = @tem_d;

    INSERT INTO movimiento (tipo, id_medicamento, id_usuario, cantidad_total, motivo, fecha)
    VALUES ('ENTRADA', @tempra, @empleado, 30, NULL, UTC_TIMESTAMP());
    SET @mov_entrada = LAST_INSERT_ID();

    INSERT INTO detalle_movimiento (id_movimiento, id_medicamento, id_lote, cantidad)
    VALUES (@mov_entrada, @tempra, @tem_d, 30);
COMMIT;

SELECT '4. Entrada: el lote TEM-D quedó con 30 unidades', IF((SELECT stock FROM lote WHERE id_lote = @tem_d) = 30, 'OK', 'FALLA');
SELECT '4. Entrada: detalle = cantidad_total', IF((SELECT SUM(cantidad) FROM detalle_movimiento WHERE id_movimiento = @mov_entrada)
    = (SELECT cantidad_total FROM movimiento WHERE id_movimiento = @mov_entrada), 'OK', 'FALLA');

-- =====================================================================
-- 5. Salida FEFO (HU-13): 7 unidades de Tempra
--    Consulta FEFO que usará el backend: solo lotes con stock y NO caducados,
--    primero el que caduca antes; si empatan, el que entró primero.
-- =====================================================================
SELECT '5. Orden FEFO: primero TEM-A (caduca antes aunque entró después)',
    IF((SELECT numero_lote FROM lote
        WHERE id_medicamento = @tempra AND stock > 0 AND fecha_caducidad > CURDATE()
        ORDER BY fecha_caducidad, fecha_entrada LIMIT 1) = 'TEM-A', 'OK', 'FALLA');

SELECT '5. FEFO no incluye el lote caducado TEM-C',
    IF((SELECT COUNT(*) FROM lote
        WHERE id_medicamento = @tempra AND stock > 0 AND fecha_caducidad > CURDATE()
          AND numero_lote = 'TEM-C') = 0, 'OK', 'FALLA');

-- La salida toma 5 de TEM-A y 2 de TEM-B. "AND stock >= cantidad" impide dejar stock negativo.
START TRANSACTION;
    INSERT INTO movimiento (tipo, id_medicamento, id_usuario, cantidad_total, motivo, fecha)
    VALUES ('SALIDA', @tempra, @empleado, 7, 'Venta mostrador', UTC_TIMESTAMP());
    SET @mov_salida = LAST_INSERT_ID();

    UPDATE lote SET stock = stock - 5 WHERE id_lote = @tem_a AND stock >= 5;
    SET @filas_a = ROW_COUNT();
    UPDATE lote SET stock = stock - 2 WHERE id_lote = @tem_b AND stock >= 2;
    SET @filas_b = ROW_COUNT();

    INSERT INTO detalle_movimiento (id_movimiento, id_medicamento, id_lote, cantidad) VALUES
        (@mov_salida, @tempra, @tem_a, 5),
        (@mov_salida, @tempra, @tem_b, 2);
COMMIT;

SELECT '5. Salida: se descontó de los dos lotes', IF(@filas_a = 1 AND @filas_b = 1, 'OK', 'FALLA');
SELECT '5. Salida: TEM-A quedó en 0 y TEM-B en 8',
    IF((SELECT stock FROM lote WHERE id_lote = @tem_a) = 0 AND (SELECT stock FROM lote WHERE id_lote = @tem_b) = 8, 'OK', 'FALLA');
SELECT '5. Salida: los detalles suman la cantidad_total (7)', IF((SELECT SUM(cantidad) FROM detalle_movimiento WHERE id_movimiento = @mov_salida)
    = (SELECT cantidad_total FROM movimiento WHERE id_movimiento = @mov_salida), 'OK', 'FALLA');

-- =====================================================================
-- 6. Baja (HU-14): el lote caducado TEM-C se da de baja con motivo
-- =====================================================================
START TRANSACTION;
    INSERT INTO movimiento (tipo, id_medicamento, id_usuario, cantidad_total, motivo, fecha)
    VALUES ('BAJA', @tempra, @admin, 4, 'Lote caducado', UTC_TIMESTAMP());
    SET @mov_baja = LAST_INSERT_ID();

    UPDATE lote SET stock = stock - 4 WHERE id_lote = @tem_c AND stock >= 4;

    INSERT INTO detalle_movimiento (id_movimiento, id_medicamento, id_lote, cantidad)
    VALUES (@mov_baja, @tempra, @tem_c, 4);
COMMIT;

SELECT '6. Baja: TEM-C quedó en 0 y el motivo se conserva',
    IF((SELECT stock FROM lote WHERE id_lote = @tem_c) = 0
       AND (SELECT motivo FROM movimiento WHERE id_movimiento = @mov_baja) = 'Lote caducado', 'OK', 'FALLA');

-- =====================================================================
-- 7. Consulta de existencias (HU-08, HU-11, HU-12): suma de lotes NO caducados
-- =====================================================================
SELECT '7. Existencias vigentes de Tempra: 38 (8 de TEM-B + 30 de TEM-D)',
    IF((SELECT COALESCE(SUM(stock), 0) FROM lote WHERE id_medicamento = @tempra AND fecha_caducidad > CURDATE()) = 38, 'OK', 'FALLA');

SELECT '7. Ventolin sin lotes: SIN EXISTENCIAS (0)',
    IF((SELECT COALESCE(SUM(l.stock), 0) FROM medicamento m
        LEFT JOIN lote l ON l.id_medicamento = m.id_medicamento AND l.fecha_caducidad > CURDATE()
        WHERE m.id_medicamento = @ventolin) = 0, 'OK', 'FALLA');

SELECT '7. Bajo stock mínimo: Amoxil NO (53 >= 5), Tempra NO (38 >= 20)',
    IF((SELECT COUNT(*) FROM medicamento m
        WHERE m.id_medicamento IN (@tempra, @amoxil)
          AND (SELECT COALESCE(SUM(stock), 0) FROM lote l
               WHERE l.id_medicamento = m.id_medicamento AND l.fecha_caducidad > CURDATE()) < m.stock_minimo) = 0, 'OK', 'FALLA');

-- =====================================================================
-- 8. Lotes próximos a caducar (HU-10, rojo = 3 meses o menos) con stock
-- =====================================================================
SELECT '8. Próximos a caducar con stock: solo AMX-2',
    IF((SELECT GROUP_CONCAT(numero_lote ORDER BY fecha_caducidad) FROM lote
        WHERE stock > 0 AND fecha_caducidad > CURDATE()
          AND fecha_caducidad <= CURDATE() + INTERVAL 3 MONTH) = 'AMX-2', 'OK', 'FALLA');

-- =====================================================================
-- 9. Auditoría (HU-11): cambio de stock mínimo + registro, en UNA transacción
-- =====================================================================
START TRANSACTION;
    UPDATE medicamento SET stock_minimo = 25 WHERE id_medicamento = @tempra;
    INSERT INTO auditoria (id_usuario, accion, tabla_afectada, id_registro, valor_anterior, valor_nuevo, fecha)
    VALUES (@admin, 'EDITAR_STOCK_MINIMO', 'medicamento', @tempra, '{"stock_minimo":20}', '{"stock_minimo":25}', UTC_TIMESTAMP());
COMMIT;

SELECT '9. Auditoría registrada con usuario, tabla y valores',
    IF((SELECT COUNT(*) FROM auditoria WHERE tabla_afectada = 'medicamento' AND id_registro = @tempra
        AND id_usuario = @admin AND valor_anterior LIKE '%20%' AND valor_nuevo LIKE '%25%') = 1, 'OK', 'FALLA');
SELECT '9. La auditoría no guarda contraseñas ni tokens',
    IF((SELECT COUNT(*) FROM auditoria WHERE CONCAT_WS(' ', valor_anterior, valor_nuevo)
        REGEXP 'contrasenia|password|token|hash') = 0, 'OK', 'FALLA');

-- =====================================================================
-- 10. Rechazo de existencias negativas
--     a) El backend descuenta con "AND stock >= cantidad": si no alcanza, no cambia nada.
--     b) Si el backend detecta 0 filas afectadas hace ROLLBACK: no queda el movimiento.
--     (El CHECK (stock >= 0) que rechaza un UPDATE directo se prueba en reglas_que_deben_fallar_v2.sh)
-- =====================================================================
SET @movimientos_antes = (SELECT COUNT(*) FROM movimiento);

START TRANSACTION;
    INSERT INTO movimiento (tipo, id_medicamento, id_usuario, cantidad_total, motivo, fecha)
    VALUES ('SALIDA', @tempra, @empleado, 999, NULL, UTC_TIMESTAMP());
    UPDATE lote SET stock = stock - 999 WHERE id_lote = @tem_b AND stock >= 999;
    SET @filas_insuficiente = ROW_COUNT();
ROLLBACK;   -- el backend hace ROLLBACK porque no alcanzó el stock

SELECT '10. Salida de 999: ninguna fila descontada', IF(@filas_insuficiente = 0, 'OK', 'FALLA');
SELECT '10. TEM-B conserva sus 8 unidades', IF((SELECT stock FROM lote WHERE id_lote = @tem_b) = 8, 'OK', 'FALLA');
SELECT '10. El ROLLBACK no dejó el movimiento a medias', IF((SELECT COUNT(*) FROM movimiento) = @movimientos_antes, 'OK', 'FALLA');
