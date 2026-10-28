# Guia de Contribuição — JT_Food

Obrigado por contribuir com o **JT_Food**! Siga as diretrizes abaixo para manter a qualidade, segurança e padronização do projeto.

## 1. Configuração do Ambiente

1. Instale o **.NET 10 SDK** (conforme especificado no [`global.json`](global.json)).
2. Certifique-se de que o repositório irmão `JT_ApplicationLayer` esteja clonado no mesmo diretório pai (`../JT_ApplicationLayer`).
3. Nunca versione credenciais reais, connection strings ou chaves privadas (`AppMani`, `GeminiApiKey`) em arquivos `appsettings*.json`. Utilize `dotnet user-secrets` ou variáveis de ambiente localmente.

## 2. Fluxo de Trabalho (Pull Requests)

1. Crie uma branch descritiva a partir da `master`:
   - `feat/nome-da-funcionalidade`
   - `fix/descricao-da-correcao`
   - `refactor/escopo`
2. Adicione ou atualize testes unitários em `JT_Food/JT_Food.Tests` para regras de negócio (`CaixaLogic`, `ProducaoLogic`, `PromocaoLogic`, `AuthPerfilService`).
3. Execute a suíte de testes antes de abrir o Pull Request:
   ```bash
   dotnet test JT_Food/JT_Food.Tests/JT_Food.Tests.csproj -c Release
   ```
4. Certifique-se de que o workflow de CI passe sem erros antes do merge.

## 3. Padrões de Código

- Respeite as regras definidas no [`.editorconfig`](.editorconfig) e [`Directory.Build.props`](Directory.Build.props).
- Operações financeiras e de estoque (FIFO, CMV, Caixa) devem utilizar aritmética `decimal` para evitar erros de arredondamento de ponto flutuante.
- Evite chamadas `GetAll()` dentro de laços de repetição (`foreach` / `for`).

