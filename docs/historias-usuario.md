# Historias de usuario

Fuente: `Historias_Usuario.xlsx` (Google Drive del equipo). 16 historias ordenadas por dependencias.

| Fase | ID | Historia | Carpeta | Tipo CQRS | Estado |
|------|----|----------|--------|-----------|--------|
| 1. Acceso | HU-01 | Autenticación e inicio de sesión | `InicioSesion` | Command | Terminada |
| | HU-02 | Cierre seguro de sesión | `CierreSesion` | Command | Pendiente |
| 2. Usuarios | HU-03 | Creación de usuarios | `CreacionUsuarios` | Command | Pendiente |
| | HU-04 | Consulta de usuarios registrados | `ConsultaUsuarios` | Query | Pendiente |
| | HU-05 | Reasignación de contraseña | `ReasignacionContrasena` | Command | Pendiente |
| 3. Catálogo | HU-06 | Registro de medicamentos | `RegistroMedicamentos` | Command | Pendiente |
| | HU-07 | Registro de entrada de lote | `EntradaLote` | Command | Pendiente |
| | HU-08 | Búsqueda de medicamentos | `BusquedaMedicamentos` | Query | Pendiente |
| | HU-09 | Edición de información de medicamentos | `EdicionMedicamentos` | Command | Pendiente |
| 4. Estado del inventario | HU-10 | Semáforo y filtros de caducidad | `SemaforoCaducidad` | Query | Pendiente |
| | HU-11 | Control de stock mínimo | `StockMinimo` | Command | Pendiente |
| | HU-12 | Consulta de medicamentos agotados | `MedicamentosAgotados` | Query | Pendiente |
| 5. Movimientos | HU-13 | Registro de salida (FEFO) | `SalidaMedicamentos` | Query + Command | Pendiente |
| | HU-14 | Baja de lotes en estado rojo | `BajaLotes` | Command | Pendiente |
| 6. Bitácora | HU-15 | Bitácora de movimientos | `BitacoraMovimientos` | Query | Pendiente |
| | HU-16 | Exportación de bitácora a Excel | `ExportacionBitacora` | Query | Pendiente |

## Pendientes de definir con el equipo

- Roles contradictorios en HU-14 (baja) y HU-15 (bitácora) entre el índice y sus criterios.
- Dato que se pide en la salida de un medicamento controlado (HU-13 criterio 1).
