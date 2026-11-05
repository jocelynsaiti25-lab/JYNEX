# Base de datos

Motor: **MySQL 8.0.16 o superior** (se necesita esa versión para que se apliquen las reglas `CHECK`).

| Archivo | Para qué sirve |
|---------|----------------|
| `etbotfarma_mysql.sql` | Crea la base `etbotfarma`: 8 tablas, relaciones, índices, 3 vistas y los roles iniciales |
| `pruebas/datos_prueba.sql` | Inserta datos de ejemplo y comprueba el semáforo, FEFO, agotados y la bitácora |
| `pruebas/reglas_que_deben_fallar.sh` | Intenta romper las reglas (existencias negativas, duplicados...) y comprueba que MySQL lo impide |

GitHub Actions ejecuta los tres en cada push.

## Crear la base en tu computadora

```bash
mysql -u root -p < database/etbotfarma_mysql.sql
```

O en MySQL Workbench: File → Open SQL Script → `etbotfarma_mysql.sql` → ejecutar (rayo).

## Tablas

| Tabla | Propósito | Historias |
|-------|-----------|-----------|
| `rol` | Roles: Administrador y Empleado | HU-01, HU-03 |
| `usuario` | Cuentas del personal (contraseña solo como hash) | HU-01, HU-03, HU-04, HU-05 |
| `sesion` | Sesiones de inicio; permite cerrarlas en el servidor | HU-01, HU-02, HU-05 |
| `medicamento` | Catálogo (datos descriptivos y stock mínimo) | HU-06, HU-07, HU-08, HU-13, HU-14 |
| `lote` | Lotes con caducidad y existencias | HU-06, HU-09, HU-11, HU-12 |
| `movimiento_inventario` | Encabezado de cada entrada, salida o baja | HU-10, HU-11, HU-12, HU-15 |
| `movimiento_detalle` | Lote(s) afectados por cada movimiento | HU-10, HU-11 (FEFO), HU-12 |
| `auditoria` | Cambios administrativos (antes / después) | HU-03, HU-05, HU-08, HU-13 |
