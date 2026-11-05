-- =====================================================================
-- Pruebas de la base de datos ETBOTFARMA (se ejecutan en GitHub Actions)
-- Inserta datos de ejemplo y verifica que todo cuadre.
-- Cada verificación imprime OK o FALLA.
-- =====================================================================
USE etbotfarma;

-- ---------- Datos de ejemplo ----------
INSERT INTO usuario (nombre, apellidos, nombre_usuario, contrasenia_hash, telefono, id_rol, fecha_registro)
VALUES ('Ana', 'López Pérez', 'admin', 'hash-de-prueba', '5512345678', 1, NOW());

INSERT INTO medicamento (nombre_comercial, nombre_sustancia, presentacion, concentracion, stock_minimo, fecha_registro) VALUES
    ('Tempra', 'Paracetamol', 'Tabletas', '500 mg', 20, NOW()),
    ('Amoxil', 'Amoxicilina', 'Cápsulas', '500 mg', 5, NOW());

INSERT INTO lote (id_medicamento, numero_lote, fecha_caducidad, stock, fecha_entrada) VALUES
    (1, 'L-001', '2028-06-30', 10, CURDATE()),
    (1, 'L-002', '2027-01-31',  5, CURDATE()),
    (2, 'L-003', '2027-03-31', 50, CURDATE());

-- Salida de 7 unidades de Tempra: 5 del lote que caduca primero (L-002) y 2 del L-001
INSERT INTO movimiento (tipo, id_medicamento, id_usuario, cantidad_total, fecha, hora)
VALUES ('SALIDA', 1, 1, 7, CURDATE(), CURTIME());
INSERT INTO detalle_movimiento (id_movimiento, id_lote, cantidad) VALUES (1, 2, 5), (1, 1, 2);
UPDATE lote SET stock = stock - 5 WHERE id_lote = 2;
UPDATE lote SET stock = stock - 2 WHERE id_lote = 1;

-- ---------- Verificaciones ----------
SELECT 'Roles iniciales: 2',
    IF((SELECT COUNT(*) FROM rol) = 2, 'OK', 'FALLA');

SELECT 'Stock de Tempra después de la salida: 8',
    IF((SELECT SUM(stock) FROM lote WHERE id_medicamento = 1) = 8, 'OK', 'FALLA');

SELECT 'La salida tiene 2 detalles que suman 7',
    IF((SELECT SUM(cantidad) FROM detalle_movimiento WHERE id_movimiento = 1) = 7, 'OK', 'FALLA');

SELECT 'Lotes de Tempra ordenados por caducidad: primero L-002',
    IF((SELECT numero_lote FROM lote WHERE id_medicamento = 1
        ORDER BY fecha_caducidad LIMIT 1) = 'L-002', 'OK', 'FALLA');
