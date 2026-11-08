#!/usr/bin/env bash
# =====================================================================
# Cada sentencia de esta lista ROMPE una regla de la base de datos,
# así que MySQL debe RECHAZARLA. Si alguna se acepta, la prueba falla.
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

debe_fallar "Stock negativo" \
  "UPDATE lote SET stock = -1 WHERE id_lote = 1"

debe_fallar "Movimiento con cantidad 0" \
  "INSERT INTO movimiento (tipo, id_medicamento, id_usuario, cantidad_total, fecha, hora) VALUES ('SALIDA', 1, 1, 0, CURDATE(), CURTIME())"

debe_fallar "Nombre de usuario repetido" \
  "INSERT INTO usuario (nombre, apellidos, nombre_usuario, contrasenia_hash, telefono, id_rol, fecha_registro) VALUES ('Otro', 'Otro', 'admin', 'x', '5512345678', 2, NOW())"

debe_fallar "Rol que no existe" \
  "INSERT INTO usuario (nombre, apellidos, nombre_usuario, contrasenia_hash, telefono, id_rol, fecha_registro) VALUES ('Otro', 'Otro', 'otro', 'x', '5512345678', 9, NOW())"

debe_fallar "Medicamento repetido" \
  "INSERT INTO medicamento (nombre_comercial, nombre_sustancia, presentacion, concentracion, fecha_registro) VALUES ('Tempra', 'Paracetamol', 'Tabletas', '500 mg', NOW())"

debe_fallar "Lote repetido para el mismo medicamento" \
  "INSERT INTO lote (id_medicamento, numero_lote, fecha_caducidad, stock, fecha_entrada) VALUES (1, 'L-001', '2030-01-01', 1, CURDATE())"

debe_fallar "Stock mínimo negativo" \
  "UPDATE medicamento SET stock_minimo = -5 WHERE id_medicamento = 1"

debe_fallar "Borrar un medicamento que tiene lotes" \
  "DELETE FROM medicamento WHERE id_medicamento = 1"

debe_fallar "Borrar un usuario que tiene movimientos" \
  "DELETE FROM usuario WHERE id_usuario = 1"

echo "-----"
if [ "$fallas" -gt 0 ]; then
  echo "$fallas regla(s) NO se cumplieron"
  exit 1
fi
echo "Todas las reglas se cumplen"
