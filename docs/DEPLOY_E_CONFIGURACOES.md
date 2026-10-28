# Guia de Configuração, Infraestrutura e Deploy - JT_Food

Este documento apresenta todas as orientações para configuração de ambiente, parâmetros de conexão, hospedagem em VPS Linux e automação de integração contínua (CI/CD) via GitHub Actions para o **JT_Food**.

---

## 1. Estrutura de Configurações (`appsettings.json`)

A aplicação utiliza o sistema de configuração hierárquico padrão do .NET. O arquivo base [appsettings.json](file:///c:/Users/sirja/source/repos/mrjalves/JT_Food/JT_Food/JT_Food.Web/appsettings.json) é carregado inicialmente, sendo sobrescrito pelo arquivo específico do ambiente (`appsettings.Development.json` ou `appsettings.Production.json`).

### 1.1. Seção `Config`

```json
{
  "Config": {
    "Ambiente": "Producao",
    "Nome": "JT Food",
    "Versao": "1.0.0",
    "TimeoutPadrao": 30,
    "Conexao": {
      "TipoConexao": "API",
      "BaseApiUrl": "https://api.jtecsistemas.com.br",
      "TipoBanco": "SqlServer",
      "StringConexao": "Data Source=...;Initial Catalog=...;User ID=...;Password=...;"
    },
    "FirebaseConfig": {
      "ApiKey": "<FIREBASE_PUBLIC_API_KEY>",
      "DatabaseUrl": "https://api.jtecsistemas.com.br",
      "BaseApiUrl": "https://api.jtecsistemas.com.br",
      "ProjetoID": "<FIREBASE_PROJECT_ID>",
      "AuthorizedDomains": [
        "localhost",
        "127.0.0.1",
        "food.jtecsistemas.com.br"
      ],
      "GoogleClientIdAndroid": "<GOOGLE_CLIENT_ID_ANDROID>.apps.googleusercontent.com",
      "GoogleClientIdWindows": "<GOOGLE_CLIENT_ID_WINDOWS>.apps.googleusercontent.com",
      "AppMani": {
        "type": "service_account",
        "project_id": "<FIREBASE_PROJECT_ID>",
        "client_email": "firebase-adminsdk-xxxx@<FIREBASE_PROJECT_ID>.iam.gserviceaccount.com"
      }
    }
  }
}
```

### 1.2. Principais Parâmetros

| Chave | Descrição |
| :--- | :--- |
| `Config:Conexao:BaseApiUrl` | Endpoint principal da API de retaguarda (`JT_API`). |
| `Config:Conexao:TipoBanco` | Tipo de banco para migrações de schema (`SqlServer`, `PostgreSql`, `MySql`). |
| `Config:Conexao:StringConexao` | String de conexão direta para execução de migrações automáticas de schema via DDL. |
| `Config:FirebaseConfig:ApiKey` | Chave de API pública do projeto Firebase para autenticação de clientes. |
| `Config:FirebaseConfig:AppMani` | Credenciais da *Service Account* do Google para validação de tokens e operações privilegiadas no servidor. |

> [!WARNING]
> Nunca versione credenciais de produção no Git. O arquivo `appsettings.Production.json` deve residir exclusivamente no servidor de produção (`/var/www/jt_food/`) e é explicitamente ignorado pelo `.gitignore` e pelo fluxo de deploy do GitHub Actions.

---

## 2. Infraestrutura Linux VPS e Scripts de Setup

Na pasta [scripts/linux](file:///c:/Users/sirja/source/repos/mrjalves/JT_Food/scripts/linux), encontram-se os arquivos prontos para provisionamento do ambiente em servidores Linux (Ubuntu 22.04 / 24.04 LTS ou Debian 12):

### 2.1. Execução do Script de Provisionamento

Para preparar a VPS pela primeira vez:

```bash
cd /caminho/para/scripts/linux
chmod +x setup-vps.sh
sudo ./setup-vps.sh SEU_USUARIO_SSH
```

O script [setup-vps.sh](file:///c:/Users/sirja/source/repos/mrjalves/JT_Food/scripts/linux/setup-vps.sh) executa automaticamente:
1. Criação do diretório de destino `/var/www/jt_food` com propriedade para o usuário SSH e grupo `www-data`.
2. Criação do arquivo de modelo `/var/www/jt_food/appsettings.Production.json` (se não existir).
3. Instalação do serviço systemd `/etc/systemd/system/jt_sistemas_food.service`.
4. Concessão de privilégio no `/etc/sudoers.d/jt_sistemas_food` para reiniciar o serviço sem senha interativa.
5. Habilitação do serviço para inicialização com o sistema operacional (`systemctl enable`).
6. Cópia da configuração do Nginx com suporte a WebSockets para `/etc/nginx/sites-available/jt_food`.

---

## 3. Configuração do Serviço Systemd

Arquivo de serviço [jt_sistemas_food.service](file:///c:/Users/sirja/source/repos/mrjalves/JT_Food/scripts/linux/jt_sistemas_food.service):

```ini
[Unit]
Description=JT Sistemas - Modulo Food (Blazor Web App)
After=network.target

[Service]
WorkingDirectory=/var/www/jt_food
ExecStart=/usr/bin/dotnet /var/www/jt_food/JT_Food.Web.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=jt_food
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false
Environment=ASPNETCORE_URLS=http://127.0.0.1:5005

[Install]
WantedBy=multi-user.target
```

---

## 4. Configuração do Servidor Web Nginx

O arquivo [nginx-jt_food.conf](file:///c:/Users/sirja/source/repos/mrjalves/JT_Food/scripts/linux/nginx-jt_food.conf) atua como proxy reverso para o Kestrel:

```nginx
server {
    listen 80;
    server_name food.jtecsistemas.com.br;

    location / {
        proxy_pass         http://127.0.0.1:5005;
        proxy_http_version 1.1;

        # Suporte fundamental para Blazor Server / WebSockets / SignalR
        proxy_set_header   Upgrade $http_upgrade;
        proxy_set_header   Connection "upgrade";

        proxy_set_header   Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;

        proxy_connect_timeout 60s;
        proxy_send_timeout    300s;
        proxy_read_timeout    300s;
    }
}
```

> [!TIP]
> Para ativar SSL gratuito com Certbot após apontar o DNS:
> ```bash
> sudo certbot --nginx -d food.jtecsistemas.com.br
> ```

---

## 5. Pipeline de CI/CD (GitHub Actions)

O workflow [.github/workflows/deploy.yml](file:///c:/Users/sirja/source/repos/mrjalves/JT_Food/.github/workflows/deploy.yml) realiza a publicação contínua automática a cada `push` na branch `master` ou via gatilho manual (`workflow_dispatch`).

### 5.1. Secrets Necessárias no GitHub

Configure em **Settings** > **Secrets and variables** > **Actions**:

| Secret | Descrição | Exemplo |
| :--- | :--- | :--- |
| `VPS_HOST` | Endereço IP ou domínio público da VPS | `185.123.45.67` |
| `VPS_PORT` | Porta SSH do servidor | `22` |
| `VPS_USER` | Usuário com permissão de escrita e sudo | `ubuntu` ou `deploy` |
| `VPS_SSH_KEY` | Chave privada SSH (formato OpenSSH/ED25519) | `-----BEGIN OPENSSH PRIVATE KEY-----` |
| `VPS_PATH` | *(Opcional)* Caminho de destino na VPS (padrão: `/var/www/jt_food`) | `/var/www/jt_food` |
| `VPS_KNOWN_HOSTS` | *(Recomendado)* Chave pública do host SSH (`ssh-keyscan`) para evitar MITM | `185.123.45.67 ssh-ed25519 AAAA...` |
| `GH_PAT` | Token de leitura para clonar o repositório `JT_ApplicationLayer` | `ghp_...` |

### 5.2. Etapas do Pipeline

```mermaid
flowchart LR
    A[Push na branch master] --> B[Checkout do Repositório]
    B --> C[Setup .NET SDK]
    C --> D[dotnet publish Release]
    D --> E[Configuração do Agente SSH]
    E --> F[rsync incremental para VPS]
    F --> G[sudo systemctl restart jt_sistemas_food]
    G --> H[Verificação do status do serviço]
```

### 5.3. Exclusões de Segurança no Rsync
Durante a sincronização de arquivos para a VPS, o comando `rsync` preserva dados sensíveis e dinâmicos:
```bash
rsync -az --delete \
  --exclude 'appsettings.json' \
  --exclude 'appsettings.Production.json' \
  --exclude '*.db' \
  --exclude '*.db-shm' \
  --exclude '*.db-wal' \
  --exclude 'uploads/' \
  --exclude 'files/'
```

---

## 6. Comandos Operacionais na VPS

### Verificar status do serviço
```bash
sudo systemctl status jt_sistemas_food
```

### Monitorar logs em tempo real
```bash
sudo journalctl -u jt_sistemas_food -f
```

### Verificar resultado da migração de banco
```bash
sudo journalctl -u jt_sistemas_food -n 50 --no-pager | grep -A5 MIGRACAO
```

### Reiniciar o serviço manualmente
```bash
sudo systemctl restart jt_sistemas_food
```

### Testar e recarregar o Nginx
```bash
sudo nginx -t && sudo systemctl reload nginx
```
