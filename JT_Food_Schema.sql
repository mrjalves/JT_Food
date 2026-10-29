-- =========================================================================
-- Script de Criação / Migração das Tabelas do Módulo Food (JT_Food)
-- Banco de Dados: JT_Food (SQL Server)
-- =========================================================================

-- 1. TABELAS BASE DE CONFIGURAÇÃO
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ConfiguracoesUsuario')
BEGIN
    CREATE TABLE [dbo].[ConfiguracoesUsuario] (
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_ConfiguracoesUsuario] PRIMARY KEY,
        [IdUsuario] INT NULL,
        [UserId] NVARCHAR(255) NULL,
        [Nome] NVARCHAR(255) NULL,
        [Endereco] NVARCHAR(255) NULL,
        [Telefone] NVARCHAR(255) NULL,
        [Cnpj] NVARCHAR(255) NULL,
        [ItensPorPagina] INT NULL,
        [NotificacoesEmail] BIT NULL
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ConfiguracoesIA')
BEGIN
    CREATE TABLE [dbo].[ConfiguracoesIA] (
        [Id] INT NOT NULL CONSTRAINT [PK_ConfiguracoesIA] PRIMARY KEY,
        [UserId] NVARCHAR(255) NULL,
        [Modelo] NVARCHAR(255) NULL,
        [Provedor] NVARCHAR(255) NULL,
        [Endpoint] NVARCHAR(255) NULL,
        [Temperatura] DECIMAL(18,2) NULL,
        [MaxTokens] INT NULL,
        [SystemPromptBase] NVARCHAR(MAX) NULL,
        [LimiteDiarioTokensUsuario] INT NULL,
        [HabilitadoParaRoles] NVARCHAR(255) NULL,
        [ApiKey] NVARCHAR(255) NULL,
        [HabilitarIAFormularios] BIT NULL,
        [HabilitarIAGrid] BIT NULL,
        [HabilitarIAModal] BIT NULL
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'FoodConfiguracoes')
BEGIN
    CREATE TABLE [dbo].[FoodConfiguracoes] (
        [Id] INT NOT NULL CONSTRAINT [PK_FoodConfiguracoes] PRIMARY KEY,
        [UserId] NVARCHAR(255) NULL,
        [NomeRestaurante] NVARCHAR(255) NULL,
        [TaxaServicoPadrao] FLOAT NULL,
        [PermiteEstoqueNegativo] BIT NULL
    );
END;

-- 2. SALÃO & MESAS
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'FoodMesas')
BEGIN
    CREATE TABLE [dbo].[FoodMesas] (
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_FoodMesas] PRIMARY KEY,
        [UserId] NVARCHAR(255) NULL,
        [Identificacao] NVARCHAR(255) NOT NULL,
        [Capacidade] INT NULL,
        [Status] NVARCHAR(255) NULL,
        [DataCadastro] DATETIME2 NULL
    );
END;

-- 3. COMANDAS E ITENS
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'FoodComandas')
BEGIN
    CREATE TABLE [dbo].[FoodComandas] (
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_FoodComandas] PRIMARY KEY,
        [UserId] NVARCHAR(255) NULL,
        [MesaId] NVARCHAR(255) NULL,
        [NomeCliente] NVARCHAR(255) NULL,
        [DataAbertura] DATETIME2 NOT NULL,
        [DataFechamento] DATETIME2 NULL,
        [Status] NVARCHAR(255) NULL,
        [Total] FLOAT NULL,
        [TaxaServico] FLOAT NULL,
        [Desconto] FLOAT NULL,
        [GarcomId] NVARCHAR(255) NULL
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'FoodItensComanda')
BEGIN
    CREATE TABLE [dbo].[FoodItensComanda] (
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_FoodItensComanda] PRIMARY KEY,
        [UserId] NVARCHAR(255) NULL,
        [ComandaId] NVARCHAR(255) NOT NULL,
        [ProdutoId] NVARCHAR(255) NOT NULL,
        [NomeProduto] NVARCHAR(255) NULL,
        [Quantidade] FLOAT NOT NULL,
        [PrecoUnitario] FLOAT NOT NULL,
        [TotalItem] FLOAT NOT NULL,
        [Status] NVARCHAR(255) NULL,
        [Observacao] NVARCHAR(255) NULL
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'FoodAdicionaisItemComanda')
BEGIN
    CREATE TABLE [dbo].[FoodAdicionaisItemComanda] (
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_FoodAdicionaisItemComanda] PRIMARY KEY,
        [UserId] NVARCHAR(255) NULL,
        [ItemComandaId] NVARCHAR(255) NOT NULL,
        [ProdutoId] NVARCHAR(255) NOT NULL,
        [NomeProduto] NVARCHAR(255) NULL,
        [Quantidade] FLOAT NOT NULL,
        [PrecoUnitario] FLOAT NOT NULL,
        [TotalAdicional] FLOAT NOT NULL,
        [DataLancamento] DATETIME2 NULL
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'FoodPagamentosComanda')
BEGIN
    CREATE TABLE [dbo].[FoodPagamentosComanda] (
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_FoodPagamentosComanda] PRIMARY KEY,
        [UserId] NVARCHAR(255) NULL,
        [ComandaId] NVARCHAR(255) NOT NULL,
        [FormaPagamento] NVARCHAR(255) NULL,
        [Valor] FLOAT NOT NULL,
        [OperadorId] NVARCHAR(255) NULL,
        [DataHora] DATETIME2 NULL,
        [Observacao] NVARCHAR(255) NULL
    );
END;

-- 4. CAIXA E FLUXO
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'FoodCaixa')
BEGIN
    CREATE TABLE [dbo].[FoodCaixa] (
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_FoodCaixa] PRIMARY KEY,
        [UserId] NVARCHAR(255) NULL,
        [OperadorAberturaId] NVARCHAR(255) NOT NULL,
        [OperadorFechamentoId] NVARCHAR(255) NULL,
        [DataAbertura] DATETIME2 NOT NULL,
        [DataFechamento] DATETIME2 NULL,
        [SaldoInicial] FLOAT NULL,
        [SaldoFinalInformado] FLOAT NULL,
        [SaldoFinalCalculado] FLOAT NULL,
        [Status] NVARCHAR(255) NULL,
        [Observacao] NVARCHAR(255) NULL
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'FoodMovimentacaoCaixa')
BEGIN
    CREATE TABLE [dbo].[FoodMovimentacaoCaixa] (
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_FoodMovimentacaoCaixa] PRIMARY KEY,
        [UserId] NVARCHAR(255) NULL,
        [CaixaId] NVARCHAR(255) NOT NULL,
        [OperadorId] NVARCHAR(255) NOT NULL,
        [DataHora] DATETIME2 NOT NULL,
        [TipoMovimentacao] NVARCHAR(255) NULL,
        [Natureza] NVARCHAR(255) NULL,
        [Valor] FLOAT NOT NULL,
        [FormaPagamento] NVARCHAR(255) NULL,
        [ComandaId] NVARCHAR(255) NULL,
        [Descricao] NVARCHAR(255) NULL
    );
END;

-- 5. INSUMOS, ESTOQUE E COMPRAS
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'FoodInsumos')
BEGIN
    CREATE TABLE [dbo].[FoodInsumos] (
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_FoodInsumos] PRIMARY KEY,
        [UserId] NVARCHAR(255) NULL,
        [Nome] NVARCHAR(255) NOT NULL,
        [UnidadeMedida] NVARCHAR(255) NULL,
        [Descricao] NVARCHAR(255) NULL,
        [EstoqueMinimo] FLOAT NULL,
        [CustoMedio] FLOAT NULL,
        [DataCadastro] DATETIME2 NULL
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'FoodEstoque')
BEGIN
    CREATE TABLE [dbo].[FoodEstoque] (
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_FoodEstoque] PRIMARY KEY,
        [UserId] NVARCHAR(255) NULL,
        [InsumoId] NVARCHAR(255) NOT NULL,
        [QuantidadeAtual] FLOAT NOT NULL,
        [DataUltimaAtualizacao] DATETIME2 NULL,
        [UsuarioUltimaAtualizacao] NVARCHAR(255) NULL
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'FoodLoteInsumo')
BEGIN
    CREATE TABLE [dbo].[FoodLoteInsumo] (
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_FoodLoteInsumo] PRIMARY KEY,
        [UserId] NVARCHAR(255) NULL,
        [InsumoId] NVARCHAR(255) NOT NULL,
        [NomeInsumo] NVARCHAR(255) NULL,
        [QuantidadeInicial] FLOAT NOT NULL,
        [QuantidadeDisponivel] FLOAT NOT NULL,
        [CustoTotalLote] FLOAT NOT NULL,
        [CustoUnitario] FLOAT NULL,
        [DataEntrada] DATETIME2 NULL,
        [DataValidade] DATETIME2 NULL,
        [Status] NVARCHAR(255) NULL
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'FoodItensCompra')
BEGIN
    CREATE TABLE [dbo].[FoodItensCompra] (
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_FoodItensCompra] PRIMARY KEY,
        [UserId] NVARCHAR(255) NULL,
        [CompraId] NVARCHAR(255) NOT NULL,
        [InsumoId] NVARCHAR(255) NOT NULL,
        [NomeInsumo] NVARCHAR(255) NULL,
        [Quantidade] FLOAT NOT NULL,
        [ValorUnitario] FLOAT NOT NULL,
        [ValorTotal] FLOAT NOT NULL
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'FoodCompras')
BEGIN
    CREATE TABLE [dbo].[FoodCompras] (
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_FoodCompras] PRIMARY KEY,
        [UserId] NVARCHAR(255) NULL,
        [Valor] FLOAT NULL,
        [DataCompra] DATETIME2 NULL,
        [Observacoes] NVARCHAR(255) NULL,
        [FornecedorId] NVARCHAR(255) NULL,
        [Status] NVARCHAR(255) NULL,
        [FornecedorRestaurante] NVARCHAR(255) NULL
    );
END;

-- 6. FICHA TÉCNICA, RECEITAS E PRODUÇÃO
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'FoodProdutos')
BEGIN
    CREATE TABLE [dbo].[FoodProdutos] (
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_FoodProdutos] PRIMARY KEY,
        [UserId] NVARCHAR(255) NULL,
        [Nome] NVARCHAR(255) NULL,
        [Preco] FLOAT NULL,
        [Descricao] NVARCHAR(255) NULL,
        [Categoria] NVARCHAR(255) NULL,
        [Tipo] NVARCHAR(255) NULL,
        [CustoInformado] FLOAT NULL,
        [PossuiReceita] BIT NULL,
        [FotoUrl] NVARCHAR(255) NULL,
        [DataCadastro] DATETIME2 NULL
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'FoodReceitas')
BEGIN
    CREATE TABLE [dbo].[FoodReceitas] (
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_FoodReceitas] PRIMARY KEY,
        [UserId] NVARCHAR(255) NULL,
        [ProdutoId] NVARCHAR(255) NOT NULL,
        [InsumoId] NVARCHAR(255) NOT NULL,
        [NomeInsumo] NVARCHAR(255) NULL,
        [Quantidade] FLOAT NOT NULL,
        [UnidadeMedida] NVARCHAR(255) NULL
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'FoodProducao')
BEGIN
    CREATE TABLE [dbo].[FoodProducao] (
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_FoodProducao] PRIMARY KEY,
        [UserId] NVARCHAR(255) NULL,
        [ProdutoId] NVARCHAR(255) NOT NULL,
        [NomeProduto] NVARCHAR(255) NULL,
        [QuantidadeProduzida] FLOAT NOT NULL,
        [DataProducao] DATETIME2 NULL,
        [ResponsavelId] NVARCHAR(255) NULL,
        [CustoTotalCalculado] FLOAT NULL,
        [CustoUnitarioSugerido] FLOAT NULL,
        [PrecoVendaTotal] FLOAT NULL,
        [LucroLote] FLOAT NULL,
        [Observacoes] NVARCHAR(255) NULL
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'FoodMemoriaCalculo')
BEGIN
    CREATE TABLE [dbo].[FoodMemoriaCalculo] (
        [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_FoodMemoriaCalculo] PRIMARY KEY,
        [UserId] NVARCHAR(255) NULL,
        [ReferenciaId] NVARCHAR(255) NULL,
        [ItemId] NVARCHAR(255) NULL,
        [LoteInsumoId] NVARCHAR(255) NULL,
        [InsumoId] NVARCHAR(255) NULL,
        [QuantidadeConsumida] FLOAT NULL,
        [CustoUnitarioLote] FLOAT NULL,
        [CustoTotalFracao] FLOAT NULL,
        [DataProcessamento] DATETIME2 NULL,
        [Descricao] NVARCHAR(255) NULL
    );
END;

