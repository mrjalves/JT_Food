using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JT_Classes.Restaurante;
using JT_Food.Shared.Domain.Enums;
using JT_Food.Shared.Services.Implementations;
using JT_Food.Shared.Services.Interfaces;
using JT_Servicos.Restaurante;

namespace JT_Food.Shared.BusinessLogic
{
    public class CaixaLogic
    {
        private readonly CaixaService _caixaService;
        private readonly MovimentacaoCaixaService _movimentacaoCaixaService;
        private readonly PagamentoComandaService _pagamentoComandaService;
        private readonly ComandaService _comandaService;
        private readonly MesaService _mesaService;
        private readonly IMesaLayoutService _mesaLayoutService;

        public CaixaLogic(
            CaixaService caixaService,
            MovimentacaoCaixaService movimentacaoCaixaService,
            PagamentoComandaService pagamentoComandaService,
            ComandaService comandaService,
            MesaService mesaService,
            IMesaLayoutService mesaLayoutService)
        {
            _caixaService = caixaService;
            _movimentacaoCaixaService = movimentacaoCaixaService;
            _pagamentoComandaService = pagamentoComandaService;
            _comandaService = comandaService;
            _mesaService = mesaService;
            _mesaLayoutService = mesaLayoutService;
        }

        public async Task<(bool Sucesso, string Mensagem, MCaixa? CaixaAtual)> ObterCaixaAbertoAsync()
        {
            var todos = await _caixaService.GetAll();
            var caixaAberto = todos?.FirstOrDefault(c => string.Equals(c.Status, "Aberto", StringComparison.OrdinalIgnoreCase));
            if (caixaAberto != null)
            {
                return (true, "Caixa aberto encontrado.", caixaAberto);
            }

            return (false, "Nenhum caixa aberto neste momento.", null);
        }

        public async Task<List<MCaixa>> ObterHistoricoCaixasAsync(int quantidade = 10)
        {
            var todos = await _caixaService.GetAll();
            return (todos ?? Enumerable.Empty<MCaixa>())
                .OrderByDescending(c => c.DataAbertura)
                .Take(quantidade)
                .ToList();
        }

        public async Task<(bool Sucesso, string Mensagem)> AbrirCaixaAsync(string operadorId, double saldoInicial, string? observacao)
        {
            try
            {
                var situacaoAtual = await ObterCaixaAbertoAsync();
                if (situacaoAtual.Sucesso)
                {
                    return (false, "Já existe um caixa aberto. Feche-o primeiro.");
                }

                var novoCaixa = new MCaixa
                {
                    OperadorAberturaId = operadorId,
                    SaldoInicial = saldoInicial,
                    SaldoFinalCalculado = saldoInicial,
                    DataAbertura = DateTime.UtcNow,
                    Status = "Aberto",
                    Observacao = observacao
                };

                int idSalvo = await _caixaService.SaveAsync(novoCaixa);
                if (idSalvo > 0)
                {
                    novoCaixa.Id = idSalvo;
                    try
                    {
                        await RegistrarMovimentacaoAsync(novoCaixa.Id.ToString(), operadorId, "Abertura", "Entrada", saldoInicial, "Dinheiro", "Abertura de Caixa");
                    }
                    catch (Exception exMov)
                    {
                        Console.WriteLine($"[CaixaLogic] Aviso ao registrar movimentação de abertura: {exMov.Message}");
                    }
                    return (true, "Caixa aberto com sucesso.");
                }

                return (false, "Erro ao abrir o caixa. Não foi possível registrar a abertura do turno.");
            }
            catch (Exception ex)
            {
                return (false, $"Erro ao abrir o caixa: {ex.Message}");
            }
        }

        public async Task<(bool Sucesso, string Mensagem)> FecharCaixaAsync(string caixaId, string operadorId, double saldoInformado, string? observacao)
        {
            try
            {
                var caixa = await _caixaService.GetById(Convert.ToInt32(caixaId));
                if (caixa == null || caixa.Status != "Aberto")
                {
                    return (false, "Caixa inválido ou já fechado.");
                }

                caixa.DataFechamento = DateTime.UtcNow;
                caixa.OperadorFechamentoId = operadorId;
                caixa.SaldoFinalInformado = saldoInformado;
                caixa.Status = "Fechado";
                caixa.Observacao += $"\nFechamento: {observacao}";

                bool salvou = await _caixaService.SaveAsync(caixa) > 0;
                if (salvou)
                {
                    try
                    {
                        await RegistrarMovimentacaoAsync(caixa.Id.ToString(), operadorId, "Fechamento", "Saida", saldoInformado, "Dinheiro", "Fechamento de Caixa");
                    }
                    catch (Exception exMov)
                    {
                        Console.WriteLine($"[CaixaLogic] Aviso ao registrar movimentação de fechamento: {exMov.Message}");
                    }
                    return (true, "Caixa fechado com sucesso.");
                }

                return (false, "Erro ao fechar o caixa.");
            }
            catch (Exception ex)
            {
                return (false, $"Erro ao fechar o caixa: {ex.Message}");
            }
        }

        public async Task<(bool Sucesso, string Mensagem)> TrocarOperadorCaixaAsync(string caixaId, string antigoOperadorId, string novoOperadorId)
        {
            try
            {
                var caixa = await _caixaService.GetById(Convert.ToInt32(caixaId));
                if (caixa == null || caixa.Status != "Aberto")
                {
                    return (false, "Caixa inválido ou fechado.");
                }
                
                caixa.Observacao += $"\nTroca de operador. Saiu:{antigoOperadorId} Entrou:{novoOperadorId} as {DateTime.UtcNow:dd/MM/yyyy HH:mm}";
                await _caixaService.SaveAsync(caixa);

                try
                {
                    await RegistrarMovimentacaoAsync(caixa.Id.ToString(), novoOperadorId, "TrocaOperador", "Calculo", 0.0, "-", $"Assumiu o caixa de {antigoOperadorId}");
                }
                catch (Exception exMov)
                {
                    Console.WriteLine($"[CaixaLogic] Aviso ao registrar movimentação de troca de operador: {exMov.Message}");
                }

                return (true, "Troca de operador registrada com sucesso.");
            }
            catch (Exception ex)
            {
                return (false, $"Erro na troca de operador: {ex.Message}");
            }
        }

        public async Task RegistrarMovimentacaoAsync(string caixaId, string operadorId, string tipoMovimentacao, string natureza, double valor, string formaPagamento, string? descricao = null, string? comandaId = null)
        {
            var mov = new MMovimentacaoCaixa
            {
                CaixaId = caixaId,
                OperadorId = operadorId,
                TipoMovimentacao = tipoMovimentacao,
                Natureza = natureza,
                Valor = valor,
                FormaPagamento = formaPagamento,
                Descricao = descricao,
                ComandaId = comandaId,
                DataHora = DateTime.UtcNow
            };

            await _movimentacaoCaixaService.SaveAsync(mov);

            // Atualiza saldo final calculado do caixa
            if (natureza == "Entrada" || natureza == "Saida")
            {
                 var caixa = await _caixaService.GetById(Convert.ToInt32(caixaId));
                 if (caixa != null && tipoMovimentacao != "Abertura" && tipoMovimentacao != "Fechamento")
                 {
                     if (natureza == "Entrada") caixa.SaldoFinalCalculado += valor;
                     if (natureza == "Saida") caixa.SaldoFinalCalculado -= valor;

                     await _caixaService.SaveAsync(caixa);
                 }
            }
        }

        public async Task<(bool Sucesso, string Mensagem)> RegistrarPagamentoParcialAsync(
            string comandaId, string formaPagamento, double valor, string operadorId, string? observacao = null)
        {
            if (valor <= 0)
                return (false, "O valor do pagamento deve ser maior que zero.");

            var resultCaixa = await ObterCaixaAbertoAsync();
            if (!resultCaixa.Sucesso)
                return (false, "Nenhum caixa aberto. Abra o caixa antes de registrar pagamentos.");

            var pagamento = new MPagamentoComanda
            {
                ComandaId = comandaId,
                FormaPagamento = formaPagamento,
                Valor = valor,
                OperadorId = operadorId,
                DataHora = DateTime.UtcNow,
                Observacao = observacao
            };

            bool salvo = await _pagamentoComandaService.SaveAsync(pagamento) > 0;
            if (!salvo)
                return (false, "Erro ao gravar o pagamento parcial.");

            await RegistrarMovimentacaoAsync(
                resultCaixa.CaixaAtual!.Id.ToString(),
                operadorId,
                "Venda",
                "Entrada",
                valor,
                formaPagamento,
                $"Pagamento parcial Comanda #{comandaId} ({formaPagamento})",
                comandaId
            );

            return (true, $"Pagamento de R$ {valor:F2} via {formaPagamento} registrado.");
        }

        public async Task<(bool Quitada, double TotalPago, double SaldoPendente)> VerificarComandaQuitadaAsync(
            string comandaId, double totalComanda)
        {
            var todosPagamentos = await _pagamentoComandaService.GetAll();
            var pagamentos = (todosPagamentos ?? Enumerable.Empty<MPagamentoComanda>())
                .Where(p => p.ComandaId == comandaId)
                .ToList();
            double totalPago = pagamentos.Sum(p => p.Valor);
            double saldoPendente = totalComanda - totalPago;

            bool quitada = saldoPendente <= 0.01;
            return (quitada, totalPago, saldoPendente < 0 ? 0 : saldoPendente);
        }

        public (double TotalPorPessoa, double ValorCouvertTotal, double ValorServicoTotal) CalcularDivisaoConta(
            double subtotalItens,
            int quantidadePessoas,
            bool cobrarCouvert,
            double valorCouvertUnitario,
            bool cobrarServico,
            double percentualServico,
            double desconto)
        {
            int pessoas = Math.Max(1, quantidadePessoas);
            double totalCouvert = cobrarCouvert ? (valorCouvertUnitario * pessoas) : 0.0;
            double totalServico = cobrarServico ? (subtotalItens * (percentualServico / 100.0)) : 0.0;
            double totalFinal = Math.Max(0, subtotalItens + totalCouvert + totalServico - desconto);
            double porPessoa = totalFinal / pessoas;

            return (Math.Round(porPessoa, 2), Math.Round(totalCouvert, 2), Math.Round(totalServico, 2));
        }

        public async Task<(bool Sucesso, string Mensagem)> FinalizarComandaELiberarMesaAsync(int comandaId)
        {
            return await FinalizarComandaConsolidadaELiberarMesaAsync(comandaId, null);
        }

        public async Task<(bool Sucesso, string Mensagem)> FinalizarComandaConsolidadaELiberarMesaAsync(
            int comandaId,
            List<int>? idsComandasConsolidadas = null)
        {
            var todasComandas = await _comandaService.GetAll() ?? new List<Comanda>();
            var comandaPrincipal = todasComandas.FirstOrDefault(c => c.Id == comandaId);
            if (comandaPrincipal == null) return (false, "Comanda não encontrada.");

            var abertas = todasComandas.Where(c => c.Status != "Fechada" && c.Status != "Cancelada").ToList();
            List<Comanda> grupoComandas = new();

            if (idsComandasConsolidadas != null && idsComandasConsolidadas.Any())
            {
                var setIds = idsComandasConsolidadas.ToHashSet();
                grupoComandas = abertas.Where(c => setIds.Contains(c.Id)).ToList();
            }

            if (!grupoComandas.Any(c => c.Id == comandaPrincipal.Id))
            {
                grupoComandas.Insert(0, comandaPrincipal);
            }

            // Se não foi fornecida lista explícita, agrupa por cartão ou por mesa
            if (idsComandasConsolidadas == null || !idsComandasConsolidadas.Any())
            {
                string idCartao = ComandaOperacionalService.ExtrairNumeroCartao(comandaPrincipal.NomeCliente);
                var irmas = abertas.Where(c =>
                    c.Id == comandaPrincipal.Id ||
                    (!string.IsNullOrEmpty(idCartao) && ComandaOperacionalService.ComandaPertenceAoCartao(c, idCartao)) ||
                    (!string.IsNullOrWhiteSpace(comandaPrincipal.MesaId) &&
                     !comandaPrincipal.MesaId.Contains("Balcão", StringComparison.OrdinalIgnoreCase) &&
                     !comandaPrincipal.MesaId.Contains("Balcao", StringComparison.OrdinalIgnoreCase) &&
                     ComandaOperacionalService.MesmaMesa(c.MesaId, comandaPrincipal.MesaId))
                ).ToList();

                foreach (var irma in irmas)
                {
                    if (!grupoComandas.Any(g => g.Id == irma.Id))
                        grupoComandas.Add(irma);
                }
            }

            // Calcular o total e verificar os pagamentos consolidados do grupo
            double totalConsolidado = grupoComandas.Sum(c => c.Total);
            var todosPagamentos = await _pagamentoComandaService.GetAll() ?? new List<MPagamentoComanda>();
            var idsString = grupoComandas.Select(c => c.Id.ToString()).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var pagamentosDoGrupo = todosPagamentos
                .Where(p => !string.IsNullOrWhiteSpace(p.ComandaId) &&
                            (idsString.Contains(p.ComandaId) || idsString.Contains(p.ComandaId.Trim().TrimStart('#'))))
                .ToList();

            double totalPago = pagamentosDoGrupo.Sum(p => p.Valor);
            double saldoPendente = totalConsolidado - totalPago;

            if (saldoPendente > 0.01)
            {
                return (false, $"Não é possível fechar a comanda. Saldo pendente: R$ {saldoPendente:F2}");
            }

            // Fecha TODOS os registros abertos da mesma comanda
            DateTime dataFechamento = DateTime.UtcNow;
            foreach (var c in grupoComandas)
            {
                c.Status = "Fechada";
                c.DataFechamento = dataFechamento;
                await _comandaService.SaveAsync(c);
            }

            // Verificar se ainda há outras comandas abertas na mesa que NÃO pertencem a este grupo
            string? mesaId = comandaPrincipal.MesaId ?? grupoComandas.FirstOrDefault(g => !string.IsNullOrWhiteSpace(g.MesaId))?.MesaId;
            if (!string.IsNullOrWhiteSpace(mesaId))
            {
                var outrasComandasMesa = todasComandas
                    .Where(c => !grupoComandas.Any(g => g.Id == c.Id) &&
                                c.Status != "Fechada" && c.Status != "Cancelada" &&
                                !string.IsNullOrWhiteSpace(c.MesaId) &&
                                ComandaOperacionalService.MesmaMesa(c.MesaId, mesaId))
                    .ToList();

                if (!outrasComandasMesa.Any())
                {
                    // Libera a mesa no banco
                    var todasMesas = await _mesaService.GetAll() ?? new List<Mesa>();
                    var mesa = todasMesas.FirstOrDefault(m =>
                        !string.IsNullOrWhiteSpace(m.Identificacao) &&
                        ComandaOperacionalService.MesmaMesa(m.Identificacao, mesaId));

                    if (mesa != null)
                    {
                        mesa.Status = StatusMesaConsts.Livre;
                        await _mesaService.SaveAsync(mesa);
                    }

                    // Libera no mapa do salão
                    await _mesaLayoutService.AtualizarStatusOperacionalAsync(mesaId, StatusMesaConsts.Livre, 0, 0);
                }
                else
                {
                    // Atualiza contagem e total das comandas que permaneceram na mesa
                    int totalCmds = outrasComandasMesa.Count;
                    double totalSub = outrasComandasMesa.Sum(c => c.Total);
                    await _mesaLayoutService.AtualizarStatusOperacionalAsync(mesaId, StatusMesaConsts.Ocupada, totalCmds, totalSub);
                }
            }

            return (true, $"Comanda #{comandaPrincipal.Id} e todos os registros associados foram fechados com sucesso!");
        }
    }
}
