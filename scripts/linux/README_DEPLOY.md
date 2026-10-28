# Guia de Deploy Contínuo (CI/CD) - JT_Food

Este documento descreve como configurar a VPS Linux e as Secrets no GitHub para ativar o deploy automático do **JT_Food** via **GitHub Actions** (baseado no padrão utilizado no projeto `JT_Finance`).

---

## 1. Secrets Necessárias no Repositório GitHub

No repositório do `JT_Food` no GitHub, vá em **Settings** > **Secrets and variables** > **Actions** > **New repository secret** e adicione:

| Secret | Descrição | Exemplo |
| :--- | :--- | :--- |
| `VPS_HOST` | IP ou domínio da sua VPS | `123.45.67.89` ou `vps.seudominio.com` |
| `VPS_PORT` | Porta SSH da sua VPS | `22` |
| `VPS_USER` | Usuário SSH com permissão de escrita | `ubuntu` ou `root` |
| `VPS_SSH_KEY` | Chave privada SSH (formato OpenSSH/ed25519) | `-----BEGIN OPENSSH PRIVATE KEY----- ...` |
| `VPS_PATH` | Diretório de destino da aplicação na VPS | `/var/www/jt_sistemas_food` |
| `GH_PAT` *(Opcional)* | Personal Access Token com escopo `repo` (necessário se `JT_ApplicationLayer` for privado) | `ghp_xxxxxxxxxxxx` |

---

## 2. Preparação Inicial na VPS Linux

Copie os arquivos da pasta `scripts/linux` para a sua VPS (ou clone o repositório temporariamente) e execute:

```bash
cd /caminho/para/scripts/linux
chmod +x setup-vps.sh
sudo ./setup-vps.sh SEU_USUARIO_SSH
```

O script irá automaticamente:
1. Criar a pasta `/var/www/jt_food` com as permissões corretas para seu usuário SSH e para o `www-data`.
2. Instalar o serviço systemd `jt_sistemas_food.service`.
3. Criar a regra no `/etc/sudoers.d/jt_sistemas_food` para permitir que o GitHub Actions reinicie o serviço sem exigir senha interativa (`sudo systemctl restart jt_sistemas_food`).
4. Habilitar o serviço para iniciar no boot (`systemctl enable`).
5. Copiar a configuração do Nginx com suporte a Blazor WebSockets / SignalR.

---

## 3. Configurações de Produção na VPS

No arquivo `/var/www/jt_food/appsettings.Production.json` na VPS, configure a conexão real do banco e URLs:

```json
{
  "Config": {
    "Ambiente": "Producao",
    "Conexao": {
      "TipoConexao": "API",
      "BaseApiUrl": "https://api.jtecsistemas.com.br",
      "TipoBanco": "SqlServer",
      "StringConexao": "Data Source=...;Initial Catalog=...;User ID=...;Password=...;"
    }
  }
}
```

> **Nota:** O `rsync` configurado no GitHub Actions ignora `appsettings.Production.json`, portanto suas senhas de produção não são sobrescritas durante o deploy.

---

## 4. Comandos Úteis na VPS

- **Status do serviço:**
  ```bash
  sudo systemctl status jt_sistemas_food
  ```

- **Logs em tempo real:**
  ```bash
  sudo journalctl -u jt_sistemas_food -f
  ```

- **Verificar migração do banco:**
  ```bash
  sudo journalctl -u jt_sistemas_food -n 50 --no-pager | grep -A5 MIGRACAO
  ```

- **Reiniciar manualmente:**
  ```bash
  sudo systemctl restart jt_sistemas_food
  ```
