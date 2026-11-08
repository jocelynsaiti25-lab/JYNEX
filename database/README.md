# Base de datos

Motor: **MySQL 8**. El backend la usa con **SQL a mano** (MySqlConnector), sin Entity Framework ni otro ORM.
Explicación completa (cambios, diagrama, reglas): [`docs/base-de-datos.md`](../docs/base-de-datos.md).

| Archivo | Para qué sirve |
|---------|----------------|
| `etbotfarma_mysql_v2.sql` | **Versión actual.** Crea la base `etbotfarma` con sus 8 tablas y los 2 roles. Se puede ejecutar varias veces |
| `migracion_v1_a_v2.sql` | Actualiza una base que ya tenías (versión 1) a la versión 2 **sin perder datos**. Se ejecuta UNA vez |
| `etbotfarma_mysql.sql` | Versión 1 (original). Se conserva como historial; ya no se usa para bases nuevas |
| `pruebas/pruebas_v2.sql` | Registro de usuarios, medicamentos y lotes; entrada; salida FEFO; baja; existencias; próximos a caducar; auditoría; rechazo de stock negativo |
| `pruebas/reglas_que_deben_fallar_v2.sh` | Comprueba que MySQL rechaza datos incorrectos (stock negativo, lote de otro medicamento, baja sin motivo, duplicados...) |
| `pruebas/verificar_migracion.sql` | Comprueba que la migración conservó los datos |
| `pruebas/datos_prueba.sql`, `pruebas/reglas_que_deben_fallar.sh` | Pruebas de la versión 1 (se usan para probar la migración) |

GitHub Actions ejecuta todas en cada push.

## ¿Qué script ejecuto?

| Tu caso | Comando |
|---------|---------|
| No tengo la base (o la quiero desde cero) | `mysql -u root -p < database/etbotfarma_mysql_v2.sql` |
| Ya tengo la base de antes (versión 1) | `mysql -u root -p < database/migracion_v1_a_v2.sql` |
| Codespaces | Nada: `bash .devcontainer/preparar.sh` detecta la versión y hace lo correcto |

¿Cómo sé si tengo la versión 1? Si la tabla `sesion` tiene la columna `cerrada`, es la versión 1.

En MySQL Workbench: File → Open SQL Script → el archivo → ejecutar (rayo).

## Probar el script en tu computadora

```bash
mysql -u root -p < database/etbotfarma_mysql_v2.sql
mysql -u root -p -N < database/pruebas/pruebas_v2.sql          # todas deben decir OK
MYSQL="mysql -uroot -pTU_CONTRASEÑA etbotfarma" bash database/pruebas/reglas_que_deben_fallar_v2.sh
```

Las pruebas insertan datos de ejemplo: hazlo en una base de práctica, no en la que usas con la app.
