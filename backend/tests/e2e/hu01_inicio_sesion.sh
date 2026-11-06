#!/usr/bin/env bash
# =====================================================================
# Prueba de punta a punta de HU-01 (se ejecuta en GitHub Actions).
# Requiere: la API corriendo en $API y MySQL con la base etbotfarma.
# =====================================================================
set -u
API=${API:-"http://127.0.0.1:5080"}
MYSQL=${MYSQL:-"mysql -h127.0.0.1 -uroot -proot etbotfarma -N"}
CONTRASENIA=${CONTRASENIA:-"Prueba123!"}
fallas=0

revisar () {
  local descripcion="$1" esperado="$2" obtenido="$3"
  if [ "$esperado" = "$obtenido" ]; then
    echo "OK     $descripcion"
  else
    echo "FALLA  $descripcion (esperado: $esperado, obtenido: $obtenido)"
    fallas=$((fallas + 1))
  fi
}

codigo () { curl -s -o /dev/null -w "%{http_code}" "$@"; }

# Criterio 5: sin sesión, lo protegido responde 401
revisar "Sin sesión: GET /api/sesiones/actual responde 401" "401" \
  "$(codigo "$API/api/sesiones/actual")"

# Criterio 1: datos obligatorios
revisar "Sin datos: responde 400" "400" \
  "$(codigo -X POST "$API/api/sesiones/iniciar" -H "Content-Type: application/json" -d '{}')"

# Criterio 4: mensaje genérico
respuesta=$(curl -s -X POST "$API/api/sesiones/iniciar" -H "Content-Type: application/json" \
  -d '{"nombreUsuario":"admin","contrasenia":"Equivocada"}')
revisar "Contraseña incorrecta: mensaje genérico" "1" \
  "$(echo "$respuesta" | grep -c 'Usuario o contraseña incorrectos.')"

revisar "Usuario inexistente: responde 401" "401" \
  "$(codigo -X POST "$API/api/sesiones/iniciar" -H "Content-Type: application/json" \
     -d '{"nombreUsuario":"noexiste","contrasenia":"x"}')"

# Criterio 3: credenciales correctas crean la sesión y la cookie
encabezados=$(mktemp)
cuerpo=$(curl -s -D "$encabezados" -X POST "$API/api/sesiones/iniciar" -H "Content-Type: application/json" \
  -d "{\"nombreUsuario\":\"admin\",\"contrasenia\":\"$CONTRASENIA\"}")
revisar "Credenciales correctas: devuelve el usuario admin" "1" \
  "$(echo "$cuerpo" | grep -c '"nombreUsuario":"admin"')"
revisar "La respuesta JSON NO incluye el token" "0" \
  "$(echo "$cuerpo" | grep -ci 'token')"

cookie=$(grep -i '^set-cookie: etbotfarma_sesion=' "$encabezados" | head -1)
revisar "Cookie HttpOnly" "1" "$(echo "$cookie" | grep -ci 'httponly')"
revisar "Cookie Secure" "1" "$(echo "$cookie" | grep -ci 'secure')"
revisar "Cookie SameSite=Strict" "1" "$(echo "$cookie" | grep -ci 'samesite=strict')"
token=$(echo "$cookie" | sed -E 's/^[Ss]et-[Cc]ookie: etbotfarma_sesion=([^;]+).*/\1/' | tr -d '\r')

# Con la cookie se puede entrar a lo protegido
revisar "Con sesión: GET /api/sesiones/actual responde 200" "200" \
  "$(codigo "$API/api/sesiones/actual" -H "Cookie: etbotfarma_sesion=$token")"

# Criterio 2: la contraseña está encriptada y el token guardado es un hash
revisar "La contraseña del admin está encriptada (no es el texto original)" "0" \
  "$($MYSQL -e "SELECT COUNT(*) FROM usuario WHERE contrasenia_hash = '$CONTRASENIA'")"
revisar "En la BD se guarda el hash del token, no el token" "0" \
  "$($MYSQL -e "SELECT COUNT(*) FROM sesion WHERE token = '$token'")"

# Criterio 6: expira por inactividad (se simula que pasaron 30 minutos)
$MYSQL -e "UPDATE sesion SET ultima_actividad = UTC_TIMESTAMP() - INTERVAL 30 MINUTE"
revisar "Sesión inactiva 30 min: responde 401" "401" \
  "$(codigo "$API/api/sesiones/actual" -H "Cookie: etbotfarma_sesion=$token")"
revisar "La sesión expirada quedó cerrada en la BD" "1" \
  "$($MYSQL -e "SELECT cerrada FROM sesion ORDER BY id_sesion DESC LIMIT 1")"

echo "-----"
if [ "$fallas" -gt 0 ]; then
  echo "$fallas prueba(s) fallaron"
  exit 1
fi
echo "HU-01: todas las pruebas pasaron"
