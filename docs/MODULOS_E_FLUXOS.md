# Módulos Funcionais e Fluxos de Negócio - JT_Food

O **JT_Food** unifica todo o ciclo operacional de um estabelecimento gastronômico — desde a recepção e abertura de mesas até a produção na cozinha, baixa de insumos por FIFO, gestão de compras e fechamento de caixa.

Este documento detalha o funcionamento de cada módulo, as máquinas de estado e as regras de negócio implementadas.

---

## 1. Mapa de Navegação e Módulos

Os módulos estão registrados em [MenuLateral.cs](file:///c:/Users/sirja/source/repos/mrjalves/JT_Food/JT_Food/JT_Food.Shared/Layout/MenuLateral.cs):

| Rota | Módulo / Tela | Descrição | Componente Principal |
| :--- | :--- | :--- | :--- |
| `/` | **Home** | Dashboard com indicadores operacionais rápidos | [Home.razor](file:///c:/Users/sirja/source/repos/mrjalves/JT_Food/JT_Food/JT_Food.Shared/Pages/Home.razor) |
| `/mesas` | **Mesas & Salão** | Monitoramento e gestão do salão e ocupação de mesas | [Mesas.razor](file:///c:/Users/sirja/source/repos/mrjalves/JT_Food/JT_Food/JT_Food.Shared/Pages/Mesas.razor) |
| `/comandas` | **Comandas** | Abertura, consulta e edição de comandas/pedidos | [VerComandas.razor](file:///c:/Users/sirja/source/repos/mrjalves/JT_Food/JT_Food/JT_Food.Shared/Pages/VerComandas.razor) |
| `/fechamento/{id}` | **Fechamento de Comanda** | Checkout, conferência de itens e registro de pagamentos | [FechamentoPedido.razor](file:///c:/Users/sirja/source/repos/mrjalves/JT_Food/JT_Food/JT_Food.Shared/Pages/FechamentoPedido.razor) |
| `/kds` | **KDS (Kitchen Display)** | Painel em tempo real para a cozinha e expedição | [KDS.razor](file:///c:/Users/sirja/source/repos/mrjalves/JT_Food/JT_Food/JT_Food.Shared/Pages/KDS.razor) |
| `/menu` | **Cardápio / Menu** | Cadastro de produtos comercializados, categorias e preços | [Menu.razor](file:///c:/Users/sirja/source/repos/mrjalves/JT_Food/JT_Food/JT_Food.Shared/Pages/Menu.razor) |
| `/receita` | **Fichas Técnicas** | Vinculação de insumos e proporções necessárias para produzir cada item | [Receita.razor](file:///c:/Users/sirja/source/repos/mrjalves/JT_Food/JT_Food/JT_Food.Shared/Pages/Receita.razor) |
| `/estoque` | **Insumos & Estoque** | Consulta de matérias-primas e saldo disponível | [Estoque.razor](file:///c:/Users/sirja/source/repos/mrjalves/JT_Food/JT_Food/JT_Food.Shared/Pages/Estoque.razor) |
| `/compras` | **Compras / Entradas** | Registro de aquisição de insumos de fornecedores | [Compras.razor](file:///c:/Users/sirja/source/repos/mrjalves/JT_Food/JT_Food/JT_Food.Shared/Pages/Compras.razor) |
| `/producao` | **Produção Manual** | Apontamento avulso de produção com baixa FIFO de insumos | [Producao.razor](file:///c:/Users/sirja/source/repos/mrjalves/JT_Food/JT_Food/JT_Food.Shared/Pages/Producao.razor) |
| `/caixa` | **Controle de Caixa** | Abertura, fechamento, sangrias, suprimentos e conferência | [Caixa.razor](file:///c:/Users/sirja/source/repos/mrjalves/JT_Food/JT_Food/JT_Food.Shared/Pages/Caixa.razor) |
| `/configuracoes` | **Configurações** | Parâmetros operacionais do módulo Food Service | [Configuracoes.razor](file:///c:/Users/sirja/source/repos/mrjalves/JT_Food/JT_Food/JT_Food.Shared/Pages/Configuracoes.razor) |

---

## 2. Fluxo Operacional de Atendimento (Salão, Mesa e Comanda)

```mermaid
sequenceDiagram
    autonumber
    actor Garcom as Garçom / Atendente
    participant Mesas as Gestão de Mesas
    participant Comandas as Módulo de Comandas
    participant KDS as KDS (Cozinha)
    participant Caixa as Frente de Caixa

    Garcom->>Mesas: Aloca cliente em Mesa (Status: Ocupada)
    Garcom->>Comandas: Abre Comanda vinculada à Mesa
    Garcom->>Comandas: Adiciona Itens e Adicionais (Status: Confirmado)
    Comandas->>KDS: Item enviado automaticamente para a Cozinha
    KDS-->>Garcom: Produção finalizada (Saída para Entrega / Entregue)
    Garcom->>Caixa: Solicita Fechamento da Comanda (/fechamento/{id})
    Caixa->>Caixa: Registra Pagamento(s) e Emite Recibo
    Caixa->>Mesas: Libera Mesa (Status: Livre)
```

### 2.1. Gestão de Mesas
- Entidade `Mesa` com campos: `Identificacao`, `Capacidade`, `Status`.
- O enum `StatusMesa` define 4 estados:
  1. `Livre`: Mesa disponível para receber clientes.
  2. `Ocupada`: Há clientes consumindo com comanda ativa.
  3. `AguardandoPagamento`: Conta solicitada, aguardando liquidação no caixa.
  4. `Reservada`: Mesa com reserva antecipada.

### 2.2. Gestão de Comandas
- Permite comandas avulsas (ex: balcão/delivery) ou vinculadas a mesas específicas.
- Itens da comanda (`MItemComanda`) possuem controle de quantidade, valor unitário e opcionais vinculados (`MAdicionaisItemComanda`).
- Fechamento de comanda com suporte a divisão de pagamento (múltiplas formas de pagamento: Dinheiro, Cartão de Crédito, Cartão de Débito, PIX).

---

## 3. Kitchen Display System (KDS)

O KDS ([KDS.razor](file:///c:/Users/sirja/source/repos/mrjalves/JT_Food/JT_Food/JT_Food.Shared/Pages/KDS.razor) e [KDSLogic.cs](file:///c:/Users/sirja/source/repos/mrjalves/JT_Food/JT_Food/JT_Food.Shared/BusinessLogic/KDSLogic.cs)) substitui comandas impressas por monitores inteligentes na cozinha e bar.

### 3.1. Máquina de Estados dos Itens

```mermaid
stateDiagram-v2
    [*] --> Confirmado: Pedido lançado pelo Garçom
    Confirmado --> Pendente: Carregado no KDS
    Pendente --> EmProducao: "Iniciar Preparo"
    EmProducao --> SaidaParaEntrega: "Avisar Garçom (Pronto)" <br/> [Dispara Baixa FIFO de Insumos]
    SaidaParaEntrega --> Entregue: "Entregar à Mesa"
    Entregue --> [*]
    
    Confirmado --> Cancelado: Cancelamento
    Pendente --> Cancelado: Cancelamento
```

### 3.2. Características do KDS
- **Fila em Tempo Real:** Atualização automática periódica (timer a cada 30 segundos com descarte limpo via `IDisposable`).
- **Cards Informativos:** Exibição destacada do nome do produto, quantidade, tempo decorrido, mesa, observações do cliente e lista de adicionais/modificadores.
- **Rastreabilidade e Log:** A cada avanço de status, o sistema anexa uma entrada com data e hora no histórico do item:
  `[dd/MM HH:mm] StatusAnterior → NovoStatus`.
- **Gatilho de Produção:** Ao transicionar de `Em Produção` para `Saída para Entrega`, o sistema aciona automaticamente a `ProducaoLogic.ProcessarProducaoAsync`, realizando a baixa imediata no estoque de insumos.

---

## 4. Controle de Produção e Baixa de Insumos por FIFO

Implementado em [ProducaoLogic.cs](file:///c:/Users/sirja/source/repos/mrjalves/JT_Food/JT_Food/JT_Food.Shared/BusinessLogic/ProducaoLogic.cs).

### 4.1. Algoritmo FIFO (First-In, First-Out)
Quando um produto é produzido:

```mermaid
flowchart TD
    Inicio[Item Pronto na Cozinha ou Produção Manual] --> ChecaReceita{Produto Possui Receita?}
    ChecaReceita -- Não --> Sucesso[Conclui sem baixa de insumos]
    ChecaReceita -- Sim --> BuscaInsumos[Recupera Ficha Técnica do Produto]
    BuscaInsumos --> LoopInsumo[Para cada Insumo da Receita]
    LoopInsumo --> BuscaLotes[Busca Lotes Disponíveis com Saldo > 0]
    BuscaLotes --> OrdenaLotes[Ordena por Data de Entrada ASC - FIFO]
    OrdenaLotes --> ConsomeLote[Consome saldo do lote mais antigo]
    ConsomeLote --> ChecaFalta{Quantidade atendida?}
    ChecaFalta -- Não --> ConsomeProx[Avança para o próximo lote mais antigo]
    ConsomeProx --> ChecaFalta
    ChecaFalta -- Sim --> RegistraMemoria[Grava Memória de Cálculo MMemoriaCalculo]
    RegistraMemoria --> SalvaProducao[Grava Registro Geral MProducao]
    SalvaProducao --> Adicionais[Processa Adicionais do Item]
```

### 4.2. Rastreabilidade Contábil (`MMemoriaCalculo`)
Para cada lote consumido, o sistema grava um registro de memória de cálculo contendo:
- ID do Lote (`LoteInsumoId`)
- Quantidade exata extraída do lote
- Custo unitário do lote no momento da extração
- Custo total rateado
- Timestamp da baixa

Isso garante que o custo da mercadoria vendida (CMV) reflita com precisão o custo real de aquisição dos insumos consumidos.

---

## 5. Gestão de Compras e Custo Médio

Implementado em [ComercialLogic.cs](file:///c:/Users/sirja/source/repos/mrjalves/JT_Food/JT_Food/JT_Food.Shared/BusinessLogic/ComercialLogic.cs) e [ModalCompra.razor](file:///c:/Users/sirja/source/repos/mrjalves/JT_Food/JT_Food/JT_Food.Shared/Components/ModalCompra.razor):

1. **Entrada de Mercadorias (`MCompra` e `MItemCompra`):**
   - Registro de fornecedor, data e múltiplos itens em uma única transação.
2. **Criação de Novos Lotes (`MLoteInsumo`):**
   - Para cada insumo recebido, um novo lote é gerado com:
     - `QuantidadeInicial` e `QuantidadeDisponivel`
     - `CustoUnitario` e `CustoTotalLote`
     - `DataEntrada` (UTC)
3. **Recálculo do Custo Médio Ponderado:**
   - O sistema calcula a média ponderada de todos os lotes ativos do insumo:
     $$\text{Custo Médio} = \frac{\sum (\text{QuantidadeDisponivel} \times \text{CustoUnitario})}{\sum \text{QuantidadeDisponivel}}$$
   - O valor apurado é atualizado na ficha cadastral do insumo (`MInsumo.CustoMedio`).

---

## 6. Frente de Caixa e Tesouraria (PDV)

Implementado em [CaixaLogic.cs](file:///c:/Users/sirja/source/repos/mrjalves/JT_Food/JT_Food/JT_Food.Shared/BusinessLogic/CaixaLogic.cs) e [Caixa.razor](file:///c:/Users/sirja/source/repos/mrjalves/JT_Food/JT_Food/JT_Food.Shared/Pages/Caixa.razor):

```mermaid
stateDiagram-v2
    [*] --> Fechado
    Fechado --> Aberto: AbrirCaixaAsync(saldoInicial) <br/> [Registra Movimentação: Abertura / Entrada]
    
    state Aberto {
        [*] --> Operando
        Operando --> Operando: Sangria (Retirada de Dinheiro)
        Operando --> Operando: Suprimento (Aporte de Troco)
        Operando --> Operando: Recebimento de Comanda (Entrada Automática)
    }
    
    Aberto --> Fechado: FecharCaixaAsync(saldoFinalInformado) <br/> [Apura Diferença / Quebra de Caixa]
```

### 6.1. Operações Suportadas
- **Abertura de Caixa:** Informação de operador e saldo inicial (fundo de troco). Bloqueia a abertura de múltiplos caixas simultâneos pelo mesmo terminal.
- **Sangria (Retirada):** Saída de dinheiro para cofre/tesouraria com justificativa obrigatória.
- **Suprimento (Aporte):** Entrada de reforço de troco.
- **Entrada Automática de Vendas:** Ao finalizar o pagamento de qualquer comanda em dinheiro ou outros meios, o `CaixaLogic` grava uma movimentação vinculando o ID da comanda.
- **Fechamento e Auditoria de Quebra:**
  $$\text{Saldo Final Calculado} = \text{Saldo Inicial} + \text{Total Entradas} - \text{Total Saídas}$$
  $$\text{Diferença (Quebra/Sobra)} = \text{Saldo Final Informado} - \text{Saldo Final Calculado}$$
  Os valores são consolidados no registro `MCaixa` com timestamp de encerramento.
