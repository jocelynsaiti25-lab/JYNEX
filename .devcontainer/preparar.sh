#!/usr/bin/env bash
# Se ejecuta UNA vez al crear el Codespace: instala dependencias y crea la base.
set -euo pipefail
cd "$(dirname "$0")/.."

echo "==> Instalando cliente de MySQL"
sudo apt-get update -qq && sudo apt-get install -y -qq mysql-client >/dev/null

echo "==> Esperando a MySQL"
for i in $(seq 1 60); do
  mysqladmin ping -h127.0.0.1 -uroot -proot --silent 2>/dev/null && break
  sleep 2
done

echo "==> Creando la base de datos etbotfarma"
mysql -h127.0.0.1 -uroot -proot < database/etbotfarma_mysql.sql

echo "==> Restaurando paquetes del backend"
dotnet restore backend/EtbotFarma.slnx

echo "==> Instalando paquetes del frontend"
(cd frontend && npm install --no-audit --no-fund)

echo ""
echo "Listo. Para arrancar la aplicación escribe:  bash .devcontainer/iniciar.sh"
