-- =====================================================================
-- Verifica que database/migracion_v1_a_v2.sql conservó los datos.
-- Se ejecuta en GitHub Actions así:
--   1. etbotfarma_mysql.sql (v1)  2. datos_prueba.sql (datos v1) + 2 sesiones
--   3. migracion_v1_a_v2.sql      4. este archivo
-- =====================================================================
USE etbotfarma;

SELECT 'Migración: se conservan los usuarios, medicamentos y lotes',
    IF((SELECT COUNT(*) FROM usuario) = 1 AND (SELECT COUNT(*) FROM medicamento) = 2
       AND (SELECT COUNT(*) FROM lote) = 3, 'OK', 'FALLA');

SELECT 'Migración: sesion.token pasó a token_hash y se quitó cerrada',
    IF((SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = 'etbotfarma' AND table_name = 'sesion'
        AND column_name IN ('token_hash', 'fecha_expiracion')) = 2
       AND (SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = 'etbotfarma' AND table_name = 'sesion'
        AND column_name IN ('token', 'cerrada')) = 0, 'OK', 'FALLA');

SELECT 'Migración: fecha_expiracion = última actividad + 20 minutos',
    IF((SELECT COUNT(*) FROM sesion WHERE TIMESTAMPDIFF(MINUTE, ultima_actividad, fecha_expiracion) = 20) = 2, 'OK', 'FALLA');

SELECT 'Migración: la sesión que estaba cerrada conserva su fecha de cierre',
    IF((SELECT COUNT(*) FROM sesion WHERE fecha_cierre IS NOT NULL) = 1, 'OK', 'FALLA');

SELECT 'Migración: movimiento.fecha es DATETIME y conserva fecha y hora',
    IF((SELECT data_type FROM information_schema.columns WHERE table_schema = 'etbotfarma'
        AND table_name = 'movimiento' AND column_name = 'fecha') = 'datetime'
       AND (SELECT COUNT(*) FROM movimiento WHERE DATE(fecha) = CURDATE()) = 1
       AND (SELECT COUNT(*) FROM information_schema.columns WHERE table_schema = 'etbotfarma'
        AND table_name IN ('movimiento', 'auditoria') AND column_name = 'hora') = 0, 'OK', 'FALLA');

SELECT 'Migración: los detalles recibieron el id_medicamento de su movimiento',
    IF((SELECT COUNT(*) FROM detalle_movimiento d INNER JOIN movimiento m ON m.id_movimiento = d.id_movimiento
        WHERE d.id_medicamento = m.id_medicamento) = 2, 'OK', 'FALLA');

SELECT 'Migración: el stock de los lotes no cambió (Tempra = 8)',
    IF((SELECT SUM(stock) FROM lote WHERE id_medicamento = 1) = 8, 'OK', 'FALLA');
