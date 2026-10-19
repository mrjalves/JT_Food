#!/usr/bin/env bash
# ==============================================================================
# Script de Configuração Inicial do JT_Food na VPS Linux (Ubuntu / Debian)
# Execute na VPS como root ou com sudo:
#   chmod +x setup-vps.sh
#   sudo ./setup-vps.sh <DEPLOY_USER>
# Exemplo:
#   sudo ./setup-vps.sh ubuntu
# ==============================================================================

set -euo pipefail

DEPLOY_USER="${1:-$USER}"
APP_DIR="/var/www/jt_food"
SERVICE_NAME="jt_sistemas_food"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo "=== [1/6] Configurando diretórios da aplicação em $APP_DIR ==="
mkdir -p "$APP_DIR"
mkdir -p "$APP_DIR/uploads"
mkdir -p "$APP_DIR/Logs"

# O usuário do deploy precisa conseguir gravar via rsync; www-data precisa ler/executar
chown -R "$DEPLOY_USER":www-data "$APP_DIR"
chmod -R 775 "$APP_DIR"

echo "=== [2/6] Criando appsettings.Production.json se não existir ==="
if [ ! -f "$APP_DIR/appsettings.Production.json" ]; then
    if [ -f "$SCRIPT_DIR/appsettings.Production.json.template" ]; then
        cp "$SCRIPT_DIR/appsettings.Production.json.template" "$APP_DIR/appsettings.Production.json"
        echo "Copiado template para $APP_DIR/appsettings.Production.json."
    else
        echo "{}" > "$APP_DIR/appsettings.Production.json"
    fi
    chown "$DEPLOY_USER":www-data "$APP_DIR/appsettings.Production.json"
    chmod 660 "$APP_DIR/appsettings.Production.json"
    echo "ATENÇÃO: Revise a string de conexão e URLs em $APP_DIR/appsettings.Production.json"
else
    echo "Arquivo $APP_DIR/appsettings.Production.json já existente. Preservado."
fi

echo "=== [3/6] Instalando serviço systemd ($SERVICE_NAME) ==="
cp "$SCRIPT_DIR/jt_sistemas_food.service" "/etc/systemd/system/${SERVICE_NAME}.service"
chmod 644 "/etc/systemd/system/${SERVICE_NAME}.service"

systemctl daemon-reload
systemctl enable "${SERVICE_NAME}.service"
echo "Serviço ${SERVICE_NAME} habilitado para inicialização automática."

echo "=== [4/6] Configurando permissão sudo sem senha para o GitHub Actions ==="
# Permite que o usuário de deploy reinicie o serviço via SSH sem pedir senha interativa
SUDOERS_FILE="/etc/sudoers.d/${SERVICE_NAME}"
cat <<EOF > "$SUDOERS_FILE"
# Permissões para deploy automatizado do JT_Food via GitHub Actions
$DEPLOY_USER ALL=(ALL) NOPASSWD: /usr/bin/systemctl restart $SERVICE_NAME
$DEPLOY_USER ALL=(ALL) NOPASSWD: /usr/bin/systemctl is-active $SERVICE_NAME
$DEPLOY_USER ALL=(ALL) NOPASSWD: /usr/bin/systemctl status $SERVICE_NAME
EOF
chmod 440 "$SUDOERS_FILE"
visudo -cf "$SUDOERS_FILE"
echo "Regra sudoers criada com sucesso em $SUDOERS_FILE."

echo "=== [5/6] Configuração do Nginx (opcional) ==="
if [ -d "/etc/nginx" ]; then
    mkdir -p /etc/nginx/sites-available /etc/nginx/sites-enabled
    if [ -f "$SCRIPT_DIR/nginx-jt_food.conf" ]; then
        cp "$SCRIPT_DIR/nginx-jt_food.conf" "/etc/nginx/sites-available/jt_sistemas_food"
        if [ ! -e "/etc/nginx/sites-enabled/jt_sistemas_food" ]; then
            ln -s "/etc/nginx/sites-available/jt_sistemas_food" "/etc/nginx/sites-enabled/"
            echo "Link simbólico criado em /etc/nginx/sites-enabled/jt_sistemas_food"
        fi
        nginx -t && systemctl reload nginx || echo "Verifique as configurações do Nginx manualmente."
    fi
else
    echo "Nginx não detectado nesta máquina. Pulando etapa de proxy reverso."
fi

echo "=== [6/6] Verificação do .NET Runtime ==="
if command -v dotnet >/dev/null 2>&1; then
    echo ".NET Runtime detectado: $(dotnet --version)"
else
    echo "AVISO: dotnet não encontrado no PATH (/usr/bin/dotnet)!"
    echo "Instale o ASP.NET Core Runtime (.NET 10) na VPS."
fi

echo ""
echo "=============================================================================="
echo "Configuração concluída com sucesso!"
echo "Diretório do app : $APP_DIR"
echo "Serviço systemd  : $SERVICE_NAME"
echo "Usuário deploy   : $DEPLOY_USER"
echo ""
echo "Próximos passos:"
echo "1. Ajuste o server_name em /etc/nginx/sites-available/jt_sistemas_food e rode: nginx -t && systemctl reload nginx"
echo "2. Revise $APP_DIR/appsettings.Production.json com as credenciais do banco."
echo "3. Execute o workflow do GitHub Actions para realizar o primeiro deploy!"
echo "=============================================================================="
