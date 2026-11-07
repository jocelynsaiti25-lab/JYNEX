#!/usr/bin/env bash
# Se ejecuta UNA vez al crear el Codespace: instala dependencias y crea la base.
set -euo pipefail
cd "$(dirname "$0")/.."

echo "==> Instalando cliente de MySQL"
sudo apt-get update -qq || true
sudo apt-get install -y -qq mysql-client >/dev/null || sudo apt-get install -y -qq default-mysql-client >/dev/null

echo "==> Esperando a MySQL"
for i in $(seq 1 60); do
  mysqladmin ping -h127.0.0.1 -uroot -proot --silent 2>/dev/null && break
  sleep 2
done

# Si la base ya existe en la versión 1 (tiene sesion.cerrada), se migra sin perder datos;
# si no, se crea con la versión 2.
version1=$(mysql -h127.0.0.1 -uroot -proot -N -e "SELECT COUNT(*) FROM information_schema.columns WHERE table_schema='etbotfarma' AND table_name='sesion' AND column_name='cerrada'" 2>/dev/null || echo 0)
if [ "$version1" = "1" ]; then
  echo "==> Migrando la base de datos a la versión 2 (se conservan los datos)"
  mysql -h127.0.0.1 -uroot -proot < database/migracion_v1_a_v2.sql
else
  echo "==> Creando la base de datos etbotfarma (versión 2)"
  mysql -h127.0.0.1 -uroot -proot < database/etbotfarma_mysql_v2.sql
fi

echo "==> Restaurando paquetes del backend"
dotnet restore backend/EtbotFarma.slnx

echo "==> Instalando paquetes del frontend"
(cd frontend && npm install --no-audit --no-fund)

echo ""
echo "Listo. Para arrancar la aplicación escribe:  bash .devcontainer/iniciar.sh"
