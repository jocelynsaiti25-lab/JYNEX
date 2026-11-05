# Historias de usuario

Fuente: `Historias_Usuario.xlsx` (Google Drive del equipo). 16 historias ordenadas por dependencias.

| Fase | ID | Historia | Módulo | Tipo CQRS | Estado |
|------|----|----------|--------|-----------|--------|
| 1. Acceso | HU-01 | Autenticación e inicio de sesión | Auth | Command | En revisión |
| | HU-02 | Cierre seguro de sesión | Auth | Command | Pendiente |
| 2. Usuarios | HU-03 | Creación de usuarios | Usuarios | Command | Pendiente |
| | HU-04 | Consulta de usuarios registrados | Usuarios | Query | Pendiente |
| | HU-05 | Reasignación de contraseña | Usuarios | Command | Pendiente |
| 3. Catálogo | HU-06 | Registro de medicamentos | Medicamentos | Command | Pendiente |
| | HU-07 | Registro de entrada de lote | Inventario | Command | Pendiente |
| | HU-08 | Búsqueda de medicamentos | Inventario | Query | Pendiente |
| | HU-09 | Edición de información de medicamentos | Medicamentos | Command | Pendiente |
| 4. Estado del inventario | HU-10 | Semáforo y filtros de caducidad | Inventario | Query | Pendiente |
| | HU-11 | Control de stock mínimo | Medicamentos | Command | Pendiente |
| | HU-12 | Consulta de medicamentos agotados | Inventario | Query | Pendiente |
| 5. Movimientos | HU-13 | Registro de salida (FEFO) | Inventario | Query + Command | Pendiente |
| | HU-14 | Baja de lotes en estado rojo | Inventario | Command | Pendiente |
| 6. Bitácora | HU-15 | Bitácora de movimientos | Bitacora | Query | Pendiente |
| | HU-16 | Exportación de bitácora a Excel | Bitacora | Query | Pendiente |

## Pendientes de definir con el equipo

- Roles contradictorios en HU-14 (baja) y HU-15 (bitácora) entre el índice y sus criterios.
- Dato que se pide en la salida de un medicamento controlado (HU-13 criterio 1).
