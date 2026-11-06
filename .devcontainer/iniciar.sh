#!/usr/bin/env bash
# Arranca la API (.NET, puerto 5080) y Angular (puerto 4200) en el Codespace.
# Para detener todo: Ctrl + C
set -euo pipefail
cd "$(dirname "$0")/.."

# Si la preparación no terminó, instala lo que falte
if [ ! -d frontend/node_modules ]; then
  echo "==> Faltan los paquetes de Angular: instalándolos (solo la primera vez)"
  (cd frontend && npm install --no-audit --no-fund)
fi

echo "==> Iniciando la API en el puerto 5080 (registro en api.log)"
dotnet run --project backend/src/EtbotFarma.Api --launch-profile http > api.log 2>&1 &
API_PID=$!
trap 'kill $API_PID 2>/dev/null' EXIT

echo "==> Esperando a que la API responda"
for i in $(seq 1 90); do
  curl -sf http://localhost:5080/api/sistema/estado >/dev/null && break
  if ! kill -0 $API_PID 2>/dev/null; then
    echo "La API se detuvo. Revisa api.log"; exit 1
  fi
  sleep 2
done

echo "==> Iniciando Angular en el puerto 4200"
echo "    Se abrirá una pestaña del navegador. Usuario: admin  Contraseña: Prueba123!"
cd frontend && npm start
