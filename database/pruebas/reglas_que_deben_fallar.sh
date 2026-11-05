#!/usr/bin/env bash
# =====================================================================
# Cada sentencia de esta lista VIOLA una regla de la base de datos,
# así que MySQL debe RECHAZARLA. Si alguna se acepta, la prueba falla.
# Uso: MYSQL="mysql -h127.0.0.1 -uroot -proot etbotfarma" ./reglas_que_deben_fallar.sh
# =====================================================================
set -u
MYSQL=${MYSQL:-"mysql -h127.0.0.1 -uroot -proot etbotfarma"}
fallas=0

debe_fallar () {
  local descripcion="$1"
  local sql="$2"
  if $MYSQL -e "$sql" 2>/dev/null; then
    echo "FALLA  $descripcion (MySQL lo aceptó)"
    fallas=$((fallas + 1))
  else
    echo "OK     $descripcion (rechazado)"
  fi
}

debe_fallar "Existencias negativas" \
  "UPDATE lote SET existencias = -1 WHERE id = 1"

debe_fallar "Baja sin motivo" \
  "INSERT INTO movimiento_inventario (tipo, medicamento_id, usuario_id, cantidad_total) VALUES ('BAJA', 1, 1, 1)"

debe_fallar "Movimiento con cantidad 0" \
  "INSERT INTO movimiento_inventario (tipo, medicamento_id, usuario_id, cantidad_total) VALUES ('SALIDA', 1, 1, 0)"

debe_fallar "Nombre de usuario duplicado (ADMIN = admin)" \
  "INSERT INTO usuario (nombre_completo, nombre_usuario, telefono, password_hash, rol_id) VALUES ('Otro', 'ADMIN', '5512345678', 'x', 2)"

debe_fallar "Teléfono con formato inválido" \
  "INSERT INTO usuario (nombre_completo, nombre_usuario, telefono, password_hash, rol_id) VALUES ('Otro', 'otro', '12ab', 'x', 2)"

debe_fallar "Rol inexistente" \
  "INSERT INTO usuario (nombre_completo, nombre_usuario, telefono, password_hash, rol_id) VALUES ('Otro', 'otro2', '5512345678', 'x', 9)"

debe_fallar "Medicamento duplicado" \
  "INSERT INTO medicamento (nombre_comercial, sustancia_activa, presentacion, concentracion) VALUES ('Tempra', 'Paracetamol', 'Tabletas', '500 mg')"

debe_fallar "Lote repetido para el mismo medicamento" \
  "INSERT INTO lote (medicamento_id, numero_lote, fecha_caducidad, existencias) VALUES (1, 'L-VERDE', '2030-01-01', 1)"

debe_fallar "Stock mínimo negativo" \
  "UPDATE medicamento SET stock_minimo = -5 WHERE id = 1"

debe_fallar "Borrar un medicamento con historial" \
  "DELETE FROM medicamento WHERE id = 1"

debe_fallar "Borrar un usuario con movimientos" \
  "DELETE FROM usuario WHERE id = 1"

echo "-----"
if [ "$fallas" -gt 0 ]; then
  echo "$fallas regla(s) NO se cumplieron"
  exit 1
fi
echo "Todas las reglas se cumplen"
