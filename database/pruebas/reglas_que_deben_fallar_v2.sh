#!/usr/bin/env bash
# =====================================================================
# Base de datos versión 2.
# Cada sentencia de esta lista ROMPE una regla, así que MySQL debe RECHAZARLA.
# Si alguna se acepta, la prueba falla. Se ejecuta DESPUÉS de pruebas_v2.sql.
# =====================================================================
set -u
MYSQL=${MYSQL:-"mysql -h127.0.0.1 -uroot -proot etbotfarma"}
fallas=0

debe_fallar () {
  if $MYSQL -e "$2" 2>/dev/null; then
    echo "FALLA  $1 (MySQL lo aceptó)"
    fallas=$((fallas + 1))
  else
    echo "OK     $1 (rechazado)"
  fi
}

# ---------- Existencias ----------
debe_fallar "Stock negativo directo" \
  "UPDATE lote SET stock = -1 WHERE numero_lote = 'TEM-B'"

debe_fallar "Descontar más unidades de las disponibles" \
  "UPDATE lote SET stock = stock - 1000 WHERE numero_lote = 'TEM-B'"

# ---------- Movimientos ----------
debe_fallar "Movimiento con cantidad 0" \
  "INSERT INTO movimiento (tipo, id_medicamento, id_usuario, cantidad_total, fecha) SELECT 'SALIDA', m.id_medicamento, u.id_usuario, 0, UTC_TIMESTAMP() FROM medicamento m, usuario u LIMIT 1"

debe_fallar "Movimiento con cantidad negativa" \
  "INSERT INTO movimiento (tipo, id_medicamento, id_usuario, cantidad_total, fecha) SELECT 'ENTRADA', m.id_medicamento, u.id_usuario, -5, UTC_TIMESTAMP() FROM medicamento m, usuario u LIMIT 1"

debe_fallar "Tipo de movimiento que no existe (TRASPASO)" \
  "INSERT INTO movimiento (tipo, id_medicamento, id_usuario, cantidad_total, fecha) SELECT 'TRASPASO', m.id_medicamento, u.id_usuario, 1, UTC_TIMESTAMP() FROM medicamento m, usuario u LIMIT 1"

debe_fallar "Baja sin motivo" \
  "INSERT INTO movimiento (tipo, id_medicamento, id_usuario, cantidad_total, fecha) SELECT 'BAJA', m.id_medicamento, u.id_usuario, 1, UTC_TIMESTAMP() FROM medicamento m, usuario u LIMIT 1"

# ---------- Detalle: el lote debe ser del mismo medicamento ----------
debe_fallar "Detalle con un lote de OTRO medicamento" \
  "INSERT INTO detalle_movimiento (id_movimiento, id_medicamento, id_lote, cantidad)
   SELECT m.id_movimiento, m.id_medicamento, l.id_lote, 1
   FROM movimiento m INNER JOIN lote l ON l.id_medicamento <> m.id_medicamento
   WHERE m.tipo = 'SALIDA' LIMIT 1"

debe_fallar "Detalle que dice otro medicamento distinto al del movimiento" \
  "INSERT INTO detalle_movimiento (id_movimiento, id_medicamento, id_lote, cantidad)
   SELECT m.id_movimiento, l.id_medicamento, l.id_lote, 1
   FROM movimiento m INNER JOIN lote l ON l.id_medicamento <> m.id_medicamento
   WHERE m.tipo = 'SALIDA' LIMIT 1"

debe_fallar "Detalle con cantidad 0" \
  "INSERT INTO detalle_movimiento (id_movimiento, id_medicamento, id_lote, cantidad)
   SELECT m.id_movimiento, m.id_medicamento, l.id_lote, 0
   FROM movimiento m INNER JOIN lote l ON l.id_medicamento = m.id_medicamento AND l.numero_lote = 'TEM-D'
   WHERE m.tipo = 'SALIDA' LIMIT 1"

debe_fallar "El mismo lote dos veces en un movimiento" \
  "INSERT INTO detalle_movimiento (id_movimiento, id_medicamento, id_lote, cantidad)
   SELECT d.id_movimiento, d.id_medicamento, d.id_lote, 1 FROM detalle_movimiento d LIMIT 1"

# ---------- Usuarios, roles y sesiones ----------
debe_fallar "Nombre de usuario repetido" \
  "INSERT INTO usuario (nombre, apellidos, nombre_usuario, correo, contrasenia_hash, telefono, id_rol, fecha_registro) VALUES ('Otro', 'Otro', 'ana.admin', 'otro1@etbotfarma.com', 'x', '5512345678', 2, UTC_TIMESTAMP())"

# HU-03: el correo identifica a la persona, no se puede repetir
debe_fallar "Correo repetido" \
  "INSERT INTO usuario (nombre, apellidos, nombre_usuario, correo, contrasenia_hash, telefono, id_rol, fecha_registro) VALUES ('Otro', 'Otro', 'otro.usuario', 'ana.lopez@etbotfarma.com', 'x', '5512345678', 2, UTC_TIMESTAMP())"

debe_fallar "Usuario sin correo" \
  "INSERT INTO usuario (nombre, apellidos, nombre_usuario, correo, contrasenia_hash, telefono, id_rol, fecha_registro) VALUES ('Otro', 'Otro', 'sin.correo', NULL, 'x', '5512345678', 2, UTC_TIMESTAMP())"

debe_fallar "Rol que no existe" \
  "INSERT INTO usuario (nombre, apellidos, nombre_usuario, correo, contrasenia_hash, telefono, id_rol, fecha_registro) VALUES ('Otro', 'Otro', 'otro', 'otro2@etbotfarma.com', 'x', '5512345678', 9, UTC_TIMESTAMP())"

debe_fallar "Borrar un rol que tiene usuarios" \
  "DELETE FROM rol WHERE id_rol = 2"

debe_fallar "Borrar un usuario que tiene movimientos" \
  "DELETE FROM usuario WHERE nombre_usuario = 'luis.empleado'"

debe_fallar "Sesión que expira antes de iniciar" \
  "INSERT INTO sesion (id_usuario, token_hash, fecha_inicio, ultima_actividad, fecha_expiracion) SELECT id_usuario, SHA2('otro', 256), UTC_TIMESTAMP(), UTC_TIMESTAMP(), UTC_TIMESTAMP() - INTERVAL 1 MINUTE FROM usuario LIMIT 1"

debe_fallar "Token de sesión repetido" \
  "INSERT INTO sesion (id_usuario, token_hash, fecha_inicio, ultima_actividad, fecha_expiracion) SELECT id_usuario, SHA2('token-de-prueba', 256), UTC_TIMESTAMP(), UTC_TIMESTAMP(), UTC_TIMESTAMP() + INTERVAL 20 MINUTE FROM usuario LIMIT 1"

# ---------- Medicamentos y lotes ----------
debe_fallar "Medicamento repetido" \
  "INSERT INTO medicamento (nombre_comercial, nombre_sustancia, presentacion, concentracion, fecha_registro) VALUES ('Tempra', 'Paracetamol', 'Tabletas', '500 mg', UTC_TIMESTAMP())"

debe_fallar "Lote repetido para el mismo medicamento" \
  "INSERT INTO lote (id_medicamento, numero_lote, fecha_caducidad, stock, fecha_entrada) SELECT id_medicamento, 'TEM-A', '2030-01-01', 1, CURDATE() FROM lote WHERE numero_lote = 'TEM-A'"

debe_fallar "Stock mínimo negativo" \
  "UPDATE medicamento SET stock_minimo = -5 WHERE nombre_comercial = 'Tempra'"

debe_fallar "Borrar un medicamento que tiene lotes" \
  "DELETE FROM medicamento WHERE nombre_comercial = 'Tempra'"

debe_fallar "Borrar un lote que aparece en movimientos" \
  "DELETE FROM lote WHERE numero_lote = 'TEM-A'"

echo "-----"
if [ "$fallas" -gt 0 ]; then
  echo "$fallas regla(s) NO se cumplieron"
  exit 1
fi
echo "Todas las reglas se cumplen"
