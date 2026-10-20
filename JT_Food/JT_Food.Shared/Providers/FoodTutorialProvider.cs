using System.Collections.Generic;
using JT_UserInterface.Shared.Models;
using JT_UserInterface.Shared.Services.Interfaces.Comum;

namespace JT_Food.Shared.Providers
{
    /// <summary>
    /// Provedor oficial de tutoriais interativos guiados para as páginas do JT_Food.
    /// Registrado via <see cref="ITutorialProvider"/> e gerenciado pelo TutorialService / TutorialHost.
    /// Permite exibição automática na primeira visita e reabertura manual pelo botão (?) no cabeçalho.
    /// </summary>
    public class FoodTutorialProvider : ITutorialProvider
    {
        public List<TutorialDefinicao> ObterTutoriais() => new()
        {
            // ──────────────────────────────────────────────
            // 1. Dashboard Executivo & Inteligência
            // ──────────────────────────────────────────────
            new TutorialDefinicao
            {
                Chave = "food-dashboard",
                Rotas = new List<string> { "/", "/home" },
                Titulo = "Dashboard Executivo JT_Food",
                Descricao = "Visão panorâmica da operação do restaurante em tempo real, métricas financeiras e inteligência analítica.",
                Icone = "bi-speedometer2",
                MostrarUmaVez = true,
                Passos = new List<TutorialPasso>
                {
                    new TutorialPasso
                    {
                        Titulo = "Visão Geral e Operador",
                        Descricao = "Acompanhe quem é o operador conectado atualmente e utilize o botão PIN para alternar rapidamente entre garçom, caixa ou gerente.",
                        Seletor = "[data-tutorial='dashboard-header']",
                        Posicao = "bottom"
                    },
                    new TutorialPasso
                    {
                        Titulo = "Faturamento & Consumo Ativo",
                        Descricao = "Consolidação em tempo real do total recebido no caixa versus o consumo ainda em aberto nas mesas e comandas do salão.",
                        Seletor = "[data-tutorial='kpi-faturamento']",
                        Posicao = "bottom"
                    },
                    new TutorialPasso
                    {
                        Titulo = "Ticket Médio & Atendimentos",
                        Descricao = "Média de valor gasto por conta fechada e a contagem de clientes atualmente em consumo no salão.",
                        Seletor = "[data-tutorial='kpi-ticket']",
                        Posicao = "bottom"
                    },
                    new TutorialPasso
                    {
                        Titulo = "Couvert Artístico & Taxas",
                        Descricao = "Valores arrecadados com apresentações musicais ao vivo e taxas de serviço (10%) para a brigada.",
                        Seletor = "[data-tutorial='kpi-couvert']",
                        Posicao = "bottom"
                    },
                    new TutorialPasso
                    {
                        Titulo = "Taxa de Ocupação do Salão",
                        Descricao = "Indicador visual com percentual de mesas ocupadas versus mesas livres para gerenciamento de fluxo e capacidade.",
                        Seletor = "[data-tutorial='kpi-ocupacao']",
                        Posicao = "bottom"
                    },
                    new TutorialPasso
                    {
                        Titulo = "Acessos Rápidos Operacionais",
                        Descricao = "Atalhos imediatos para o Mapa de Mesas, Comandas Ativas e o sistema de exibição de cozinha (KDS).",
                        Seletor = "[data-tutorial='dashboard-atalhos']",
                        Posicao = "top"
                    },
                    new TutorialPasso
                    {
                        Titulo = "Pedidos em Tempo Real",
                        Descricao = "Feed dinâmico dos últimos itens lançados pelos garçons, com quantidades, valores e status de preparo.",
                        Seletor = "[data-tutorial='dashboard-pedidos']",
                        Posicao = "top"
                    },
                    new TutorialPasso
                    {
                        Titulo = "Comandas Abertas no Salão",
                        Descricao = "Listagem de comandas em atendimento com horário de abertura e atalho direto para fechamento de conta.",
                        Seletor = "[data-tutorial='dashboard-comandas']",
                        Posicao = "top"
                    },
                    new TutorialPasso
                    {
                        Titulo = "Relatório com Inteligência Artificial",
                        Descricao = "Gere diagnósticos automatizados do turno utilizando o Gemini para identificar horários de pico, itens mais rentáveis e sugestões operacionais.",
                        Seletor = "[data-tutorial='dashboard-ia']",
                        Posicao = "top"
                    },
                    new TutorialPasso
                    {
                        Titulo = "Assistente de IA & Chat Integrado",
                        Descricao = "Clique no ícone de chat no topo a qualquer momento para conversar com o Assistente IA sobre receitas, procedimentos ou dúvidas operacionais.",
                        Seletor = "header .header-actions button[title='Chat IA']",
                        Posicao = "bottom"
                    },
                    new TutorialPasso
                    {
                        Titulo = "Ajuda e Tutoriais da Página",
                        Descricao = "Sempre que tiver dúvidas sobre uma tela, clique no botão (?) para reabrir este tour guiado com as principais explicações.",
                        Seletor = "header .header-actions button[title='Tutorial da página']",
                        Posicao = "bottom"
                    }
                }
            },

            // ──────────────────────────────────────────────
            // 2. Salão e Mapa Interativo de Mesas
            // ──────────────────────────────────────────────
            new TutorialDefinicao
            {
                Chave = "food-mesas",
                Rotas = new List<string> { "/mesas" },
                Titulo = "Salão & Mapa Interativo de Mesas",
                Descricao = "Gestão visual de salão, ocupação de mesas, rotação de mobiliário e abertura de comandas.",
                Icone = "bi-grid-1x2-fill",
                MostrarUmaVez = true,
                Passos = new List<TutorialPasso>
                {
                    new TutorialPasso
                    {
                        Titulo = "Modos de Visualização",
                        Descricao = "Alterne entre o 'Mapa Operacional' interativo, o 'Construtor de Salão' (arraste, rotação e layout das mesas) e a 'Lista em Tabela'.",
                        Seletor = "[data-tutorial='mesas-modos']",
                        Posicao = "bottom"
                    },
                    new TutorialPasso
                    {
                        Titulo = "Cadastrar Nova Mesa",
                        Descricao = "Clique aqui para criar uma nova mesa definindo o número, capacidade de assentos e setor do restaurante.",
                        Seletor = "[data-tutorial='mesas-novo']",
                        Posicao = "bottom"
                    },
                    new TutorialPasso
                    {
                        Titulo = "Status do Turno de Caixa",
                        Descricao = "Para abrir comandas e lançar pedidos no salão, certifique-se de que há um turno de caixa ativo.",
                        Seletor = "[data-tutorial='mesas-alerta-caixa']",
                        Posicao = "bottom",
                        TextoElementoNaoEncontrado = "O caixa precisa estar aberto para lançamento de pedidos no salão."
                    },
                    new TutorialPasso
                    {
                        Titulo = "Mapa Interativo do Salão",
                        Descricao = "Clique sobre qualquer mesa para ver seu status (Livre, Ocupada, Reservada, Conta Solicitada), abrir comandas ou lançar pedidos imediatamente.",
                        Seletor = "[data-tutorial='mesas-mapa']",
                        Posicao = "top"
                    }
                }
            },

            // ──────────────────────────────────────────────
            // 3. Gestão de Comandas e Atendimento
            // ──────────────────────────────────────────────
            new TutorialDefinicao
            {
                Chave = "food-comandas",
                Rotas = new List<string> { "/comandas" },
                Titulo = "Comandas & Atendimento",
                Descricao = "Abertura flexível de comandas com ou sem mesa, pesquisa rápida por cartão ou cliente e controle de pedidos.",
                Icone = "bi-card-checklist",
                MostrarUmaVez = true,
                Passos = new List<TutorialPasso>
                {
                    new TutorialPasso
                    {
                        Titulo = "Nova Comanda Flexível",
                        Descricao = "Abra uma nova comanda associando a uma mesa, balcão ou diretamente pelo número do cartão magnético/RFID do cliente.",
                        Seletor = "[data-tutorial='comandas-nova']",
                        Posicao = "bottom"
                    },
                    new TutorialPasso
                    {
                        Titulo = "Grade de Comandas Ativas",
                        Descricao = "Consulte todas as comandas abertas, o nome do cliente, o valor acumulado e acerte pedidos ou solicite fechamento.",
                        Seletor = "[data-tutorial='comandas-grid']",
                        Posicao = "top"
                    }
                }
            },

            // ──────────────────────────────────────────────
            // 4. KDS — Display de Cozinha e Bar
            // ──────────────────────────────────────────────
            new TutorialDefinicao
            {
                Chave = "food-kds",
                Rotas = new List<string> { "/kds" },
                Titulo = "KDS — Display de Cozinha & Bar",
                Descricao = "Monitoramento visual de pedidos por praça de preparo com atualização automática em tempo real.",
                Icone = "bi-display",
                MostrarUmaVez = true,
                Passos = new List<TutorialPasso>
                {
                    new TutorialPasso
                    {
                        Titulo = "Filtro de Setor / Praça",
                        Descricao = "Selecione entre Cozinha, Bar, Chapa ou veja todos os setores para direcionar a tela da estação correta.",
                        Seletor = "[data-tutorial='kds-filtros-setor']",
                        Posicao = "bottom"
                    },
                    new TutorialPasso
                    {
                        Titulo = "Fila: A Preparar",
                        Descricao = "Pedidos recém-enviados pelos garçons aguardando início de preparo. Contém mesa, comanda e observações especiais de produção.",
                        Seletor = "[data-tutorial='kds-preparar']",
                        Posicao = "right"
                    },
                    new TutorialPasso
                    {
                        Titulo = "Fila: Em Preparo / Pronto",
                        Descricao = "Itens em cocção ativa. Ao finalizar, clique no botão de ação para marcar como Pronto ou Entregue ao garçom.",
                        Seletor = "[data-tutorial='kds-em-preparo']",
                        Posicao = "left"
                    }
                }
            },

            // ──────────────────────────────────────────────
            // 5. Cardápio & Motor de Promoções
            // ──────────────────────────────────────────────
            new TutorialDefinicao
            {
                Chave = "food-menu",
                Rotas = new List<string> { "/menu" },
                Titulo = "Cardápio & Promoções",
                Descricao = "Gestão de itens vendíveis, adicionais, regras automatizadas de Happy Hour e roteamento de pedidos.",
                Icone = "bi-journal-text",
                MostrarUmaVez = true,
                Passos = new List<TutorialPasso>
                {
                    new TutorialPasso
                    {
                        Titulo = "Abas de Gestão",
                        Descricao = "Navegue entre os Produtos do Cardápio, o Motor de Promoções & Happy Hour e as Categorias de Roteamento.",
                        Seletor = "[data-tutorial='menu-abas']",
                        Posicao = "bottom"
                    },
                    new TutorialPasso
                    {
                        Titulo = "Ações & Cadastros Rápidos",
                        Descricao = "Cadastre novos pratos, crie promoções por horário ou adicione categorias operacionais com poucos cliques.",
                        Seletor = "[data-tutorial='menu-acoes']",
                        Posicao = "bottom"
                    },
                    new TutorialPasso
                    {
                        Titulo = "Filtros por Categoria",
                        Descricao = "Filtre instantaneamente os itens por Bebidas, Porções, Lanches, Pratos, Sobremesas ou Taxas de Couvert.",
                        Seletor = "[data-tutorial='menu-filtros']",
                        Posicao = "bottom"
                    }
                }
            },

            // ──────────────────────────────────────────────
            // 6. Controle de Caixa & PDV
            // ──────────────────────────────────────────────
            new TutorialDefinicao
            {
                Chave = "food-caixa",
                Rotas = new List<string> { "/caixa" },
                Titulo = "Controle de Caixa & PDV",
                Descricao = "Gestão de turnos, controle de fundo de troco, sangrias de segurança e liquidação de vendas.",
                Icone = "bi-cash-coin",
                MostrarUmaVez = true,
                Passos = new List<TutorialPasso>
                {
                    new TutorialPasso
                    {
                        Titulo = "Cabeçalho e Turno Operacional",
                        Descricao = "Indica claramente se o caixa está aberto ou fechado, o número do turno e o operador responsável pelo saldo.",
                        Seletor = "[data-tutorial='caixa-header']",
                        Posicao = "bottom"
                    },
                    new TutorialPasso
                    {
                        Titulo = "Abertura de Turno",
                        Descricao = "Quando o caixa estiver fechado, selecione o operador e informe o valor em dinheiro para fundo de troco.",
                        Seletor = "[data-tutorial='caixa-abertura']",
                        Posicao = "right",
                        TextoElementoNaoEncontrado = "O formulário de abertura fica disponível quando o caixa está fechado."
                    },
                    new TutorialPasso
                    {
                        Titulo = "Balanço e KPIs do Turno",
                        Descricao = "Acompanhe em tempo real o saldo na gaveta, faturamento em cartão, PIX, dinheiro e conferência de totais.",
                        Seletor = "[data-tutorial='caixa-balanco']",
                        Posicao = "bottom",
                        TextoElementoNaoEncontrado = "Os indicadores do balanço aparecem enquanto o turno estiver aberto."
                    },
                    new TutorialPasso
                    {
                        Titulo = "Sangrias & Reforços de Gaveta",
                        Descricao = "Realize retiradas seguras de dinheiro para o cofre ou adicione novos reforços de troco com justificativa gravada.",
                        Seletor = "[data-tutorial='caixa-sangria-reforco']",
                        Posicao = "right",
                        TextoElementoNaoEncontrado = "Ajustes de sangria e reforço ficam ativos durante o turno aberto."
                    },
                    new TutorialPasso
                    {
                        Titulo = "Encerramento e Fechamento de Turno",
                        Descricao = "Finalize o turno conferindo o saldo final em gaveta e registrando a prestação de contas do operador.",
                        Seletor = "[data-tutorial='caixa-fechamento']",
                        Posicao = "right",
                        TextoElementoNaoEncontrado = "O encerramento do turno é realizado após o encerramento do expediente."
                    }
                }
            },

            // ──────────────────────────────────────────────
            // 7. Fechamento de Pedido & Quitação
            // ──────────────────────────────────────────────
            new TutorialDefinicao
            {
                Chave = "food-fechamento",
                Rotas = new List<string> { "/fechamento" },
                Titulo = "Fechamento de Conta & Pagamentos",
                Descricao = "Conferência de itens consumidos, aplicação de couvert, taxa de serviço e quitação em múltiplas formas de pagamento.",
                Icone = "bi-receipt",
                MostrarUmaVez = true,
                Passos = new List<TutorialPasso>
                {
                    new TutorialPasso
                    {
                        Titulo = "Dados da Comanda e Mesa",
                        Descricao = "Identificação do cliente, número da mesa de origem e horário de início do atendimento.",
                        Seletor = "[data-tutorial='fechamento-header']",
                        Posicao = "bottom"
                    },
                    new TutorialPasso
                    {
                        Titulo = "Itens Consumidos",
                        Descricao = "Audite cada item consumido, quantidades e valores unitários antes de emitir a conta para a mesa.",
                        Seletor = "[data-tutorial='fechamento-itens']",
                        Posicao = "right"
                    },
                    new TutorialPasso
                    {
                        Titulo = "Totais, Couvert & Taxas",
                        Descricao = "Ajuste o número de pagantes para o couvert artístico ou isente-o, e defina se a taxa de 10% de serviço será aplicada.",
                        Seletor = "[data-tutorial='fechamento-totais']",
                        Posicao = "left"
                    },
                    new TutorialPasso
                    {
                        Titulo = "Múltiplas Formas de Pagamento",
                        Descricao = "Permite dividir a conta: registre parte no PIX, parte no Cartão ou Dinheiro até quitar o saldo total.",
                        Seletor = "[data-tutorial='fechamento-pagamentos']",
                        Posicao = "left",
                        TextoElementoNaoEncontrado = "Disponível enquanto houver saldo pendente na comanda."
                    },
                    new TutorialPasso
                    {
                        Titulo = "Quitar e Liberar Mesa",
                        Descricao = "Após zerar o saldo pendente, clique para concluir a liquidação no caixa e liberar a mesa para novos clientes.",
                        Seletor = "[data-tutorial='fechamento-finalizar']",
                        Posicao = "top"
                    }
                }
            },

            // ──────────────────────────────────────────────
            // 8. Configurações Gerais & Parâmetros
            // ──────────────────────────────────────────────
            new TutorialDefinicao
            {
                Chave = "food-configuracoes",
                Rotas = new List<string> { "/configuracoes" },
                Titulo = "Configurações Gerais & Parâmetros",
                Descricao = "Configuração de impressoras térmicas ESC/POS em rede, regras de couvert artístico e integração com IA Gemini.",
                Icone = "bi-gear-fill",
                MostrarUmaVez = true,
                Passos = new List<TutorialPasso>
                {
                    new TutorialPasso
                    {
                        Titulo = "Abas de Parâmetros",
                        Descricao = "Navegue entre as abas para configurar os dados da empresa, regras operacionais do restaurante e chaves de IA.",
                        Seletor = ".nav-tabs, .nav-pills, [role='tablist']",
                        Posicao = "bottom"
                    },
                    new TutorialPasso
                    {
                        Titulo = "Impressoras de Rede (ESC/POS)",
                        Descricao = "Cadastre as impressoras térmicas por endereço IP e porta TCP (ex: 9100) para direcionar pedidos da Cozinha e Bar.",
                        Seletor = "button:has(.bi-printer)",
                        Posicao = "bottom",
                        TextoElementoNaoEncontrado = "Configure impressoras por IP nas abas de configuração do restaurante."
                    },
                    new TutorialPasso
                    {
                        Titulo = "Inteligência Artificial (Gemini)",
                        Descricao = "Insira sua API Key do Google Gemini e escolha o modelo padrão (ex: gemini-1.5-flash) para habilitar análises e relatórios inteligentes.",
                        Seletor = "input[type='password'], select:has(option)",
                        Posicao = "top",
                        TextoElementoNaoEncontrado = "Informe sua Gemini API Key na aba de Inteligência Artificial."
                    },
                    new TutorialPasso
                    {
                        Titulo = "Salvar Alterações",
                        Descricao = "Grave suas preferências para que os novos parâmetros comecem a valer imediatamente em todo o sistema.",
                        Seletor = "button[type='submit'], .btn-primary:has(.bi-save)",
                        Posicao = "top"
                    }
                }
            },

            // ──────────────────────────────────────────────
            // 9. Linha de Produção & Fichas Técnicas
            // ──────────────────────────────────────────────
            new TutorialDefinicao
            {
                Chave = "food-producao",
                Rotas = new List<string> { "/producao" },
                Titulo = "Linha de Produção & Fichas Técnicas",
                Descricao = "Acompanhamento de fabricação prévia de bases, molhos, porções e controle de insumos na cozinha.",
                Icone = "bi-fire",
                MostrarUmaVez = true,
                Passos = new List<TutorialPasso>
                {
                    new TutorialPasso
                    {
                        Titulo = "Planejamento de Produção",
                        Descricao = "Monitore lotes de alimentos produzidos internamente e seus prazos de validade para atender a demanda do salão.",
                        Seletor = ".container-fluid",
                        Posicao = "bottom"
                    }
                }
            },

            // ──────────────────────────────────────────────
            // 10. Gestão de Insumos, Compras e Receitas
            // ──────────────────────────────────────────────
            new TutorialDefinicao
            {
                Chave = "food-estoque-receita",
                Rotas = new List<string> { "/estoque", "/compras", "/receita" },
                Titulo = "Insumos, Receitas & Estoque",
                Descricao = "Gestão de matérias-primas alimentícias, fichas técnicas de pratos e compras de reposição com baixa automática.",
                Icone = "bi-box-seam",
                MostrarUmaVez = true,
                Passos = new List<TutorialPasso>
                {
                    new TutorialPasso
                    {
                        Titulo = "Fichas Técnicas & Composição",
                        Descricao = "Cada produto do cardápio pode ter sua receita cadastrada para deduzir automaticamente os insumos do estoque conforme os pedidos são lançados.",
                        Seletor = ".container-fluid",
                        Posicao = "bottom"
                    }
                }
            }
        };
    }
}
