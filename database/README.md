# Base de datos

Motor: **MySQL 8**.

| Archivo | Para qué sirve |
|---------|----------------|
| `etbotfarma_mysql.sql` | Crea la base `etbotfarma` con sus 8 tablas y los 2 roles iniciales |
| `pruebas/datos_prueba.sql` | Inserta datos de ejemplo y comprueba que todo cuadre |
| `pruebas/reglas_que_deben_fallar.sh` | Comprueba que MySQL rechaza datos incorrectos (stock negativo, duplicados...) |

GitHub Actions ejecuta los tres en cada push.

## Crear la base en tu computadora

```bash
mysql -u root -p < database/etbotfarma_mysql.sql
```

O en MySQL Workbench: File → Open SQL Script → `etbotfarma_mysql.sql` → ejecutar (rayo).

## Tablas

| Tabla | Propósito | Historias |
|-------|-----------|-----------|
| `rol` | Tipos de usuario: Administrador y Empleado | HU-01, HU-03 |
| `usuario` | Personal que usa el sistema | HU-01, HU-03, HU-04, HU-05 |
| `sesion` | Inicios y cierres de sesión | HU-01, HU-02, HU-05 |
| `medicamento` | Catálogo de medicamentos | HU-06, HU-07, HU-08, HU-13, HU-14 |
| `lote` | Lotes con fecha de caducidad y stock | HU-06, HU-09, HU-11, HU-12 |
| `movimiento` | Bitácora de entradas, salidas y bajas | HU-10, HU-11, HU-12, HU-15 |
| `detalle_movimiento` | Cuánto se tomó de cada lote en cada movimiento | HU-10, HU-11, HU-12 |
| `auditoria` | Cambios importantes hechos por los usuarios | HU-03, HU-05, HU-08, HU-13 |
