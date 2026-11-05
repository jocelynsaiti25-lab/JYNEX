-- =====================================================================
-- Pruebas de la base de datos ETBOTFARMA (se ejecutan en GitHub Actions)
-- Inserta datos de ejemplo y verifica las reglas. Cada verificación
-- imprime OK o FALLA; el CI falla si aparece alguna FALLA.
-- =====================================================================
USE etbotfarma;

-- ---------- Datos de ejemplo ----------
INSERT INTO usuario (nombre_completo, nombre_usuario, telefono, password_hash, rol_id)
VALUES ('Administrador Prueba', 'admin', '5512345678', 'hash-de-prueba', 1);

INSERT INTO medicamento (nombre_comercial, sustancia_activa, presentacion, concentracion, stock_minimo) VALUES
  ('Tempra', 'Paracetamol', 'Tabletas', '500 mg', 20),
  ('Amoxil', 'Amoxicilina', 'Cápsulas', '500 mg', 5);

INSERT INTO lote (medicamento_id, numero_lote, fecha_caducidad, existencias) VALUES
  (1, 'L-VERDE',    DATE_ADD(CURDATE(), INTERVAL 13 MONTH), 10),
  (1, 'L-AMARILLO', DATE_ADD(CURDATE(), INTERVAL 6 MONTH),   5),
  (1, 'L-ROJO',     DATE_ADD(CURDATE(), INTERVAL 2 MONTH),   3),
  (2, 'L-CADUCADO', DATE_SUB(CURDATE(), INTERVAL 1 DAY),    50);

-- Salida FEFO de 4 unidades de Tempra: 3 del lote ROJO + 1 del AMARILLO
START TRANSACTION;
INSERT INTO movimiento_inventario (tipo, medicamento_id, usuario_id, cantidad_total)
VALUES ('SALIDA', 1, 1, 4);
INSERT INTO movimiento_detalle (movimiento_id, lote_id, cantidad) VALUES
  (LAST_INSERT_ID(), 3, 3),
  (LAST_INSERT_ID(), 2, 1);
UPDATE lote SET existencias = existencias - 3, version = version + 1 WHERE id = 3;
UPDATE lote SET existencias = existencias - 1, version = version + 1 WHERE id = 2;
COMMIT;

-- Baja del lote caducado con motivo
START TRANSACTION;
INSERT INTO movimiento_inventario (tipo, medicamento_id, usuario_id, cantidad_total, motivo)
VALUES ('BAJA', 2, 1, 50, 'Lote caducado');
INSERT INTO movimiento_detalle (movimiento_id, lote_id, cantidad) VALUES (LAST_INSERT_ID(), 4, 50);
UPDATE lote SET existencias = 0, version = version + 1 WHERE id = 4;
COMMIT;

-- ---------- Verificaciones ----------
SELECT 'Semáforo VERDE (>12 meses)',
  IF((SELECT semaforo FROM vw_inventario_lotes WHERE numero_lote = 'L-VERDE') = 'VERDE', 'OK', 'FALLA');

SELECT 'Semáforo AMARILLO (3-12 meses)',
  IF((SELECT semaforo FROM vw_inventario_lotes WHERE numero_lote = 'L-AMARILLO') = 'AMARILLO', 'OK', 'FALLA');

SELECT 'Semáforo ROJO (<=3 meses)',
  IF((SELECT semaforo FROM vw_inventario_lotes WHERE numero_lote = 'L-ROJO') = 'ROJO', 'OK', 'FALLA');

SELECT 'Caducado = ROJO + CADUCADO',
  IF((SELECT CONCAT(semaforo, caducado) FROM vw_inventario_lotes WHERE numero_lote = 'L-CADUCADO') = 'ROJO1', 'OK', 'FALLA');

SELECT 'Existencias después de la salida (ROJO=0, AMARILLO=4)',
  IF((SELECT existencias FROM lote WHERE id = 3) = 0 AND (SELECT existencias FROM lote WHERE id = 2) = 4, 'OK', 'FALLA');

SELECT 'FEFO: siguiente lote a usar es AMARILLO',
  IF((SELECT numero_lote FROM lote
       WHERE medicamento_id = 1 AND fecha_caducidad >= CURDATE() AND existencias > 0
       ORDER BY fecha_caducidad, id LIMIT 1) = 'L-AMARILLO', 'OK', 'FALLA');

SELECT 'Tempra: 14 vigentes <= mínimo 20 -> STOCK_BAJO',
  IF((SELECT CONCAT(existencias_vigentes, estado_stock) FROM vw_existencias_medicamento WHERE medicamento_id = 1) = '14STOCK_BAJO', 'OK', 'FALLA');

SELECT 'Amoxil: solo lote caducado -> AGOTADO',
  IF((SELECT estado_stock FROM vw_existencias_medicamento WHERE medicamento_id = 2) = 'AGOTADO', 'OK', 'FALLA');

SELECT 'Bitácora: 3 filas (2 de la salida + 1 de la baja)',
  IF((SELECT COUNT(*) FROM vw_bitacora) = 3, 'OK', 'FALLA');

SELECT 'Roles iniciales: 2',
  IF((SELECT COUNT(*) FROM rol) = 2, 'OK', 'FALLA');
