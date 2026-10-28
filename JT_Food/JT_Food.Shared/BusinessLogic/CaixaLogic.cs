using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
        private static readonly SemaphoreSlim _caixaLock = new(1, 1);

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
            if (saldoInicial < 0)
            {
                return (false, "O saldo inicial não pode ser negativo.");
            }

            await _caixaLock.WaitAsync();
            try
            {
                var situacaoAtual = await ObterCaixaAbertoAsync();
                if (situacaoAtual.Sucesso)
                {
                    return (false, "Já existe um caixa aberto. Feche-o primeiro.");
                }

                double saldoInicialArredondado = (double)Math.Round((decimal)saldoInicial, 2);

                var novoCaixa = new MCaixa
                {
                    OperadorAberturaId = operadorId,
                    SaldoInicial = saldoInicialArredondado,
                    SaldoFinalCalculado = saldoInicialArredondado,
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
                        await RegistrarMovimentacaoInternaAsync(novoCaixa.Id.ToString(), operadorId, "Abertura", "Entrada", saldoInicialArredondado, "Dinheiro", "Abertura de Caixa");
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
            finally
            {
                _caixaLock.Release();
            }
        }

        public async Task<(bool Sucesso, string Mensagem)> FecharCaixaAsync(string caixaId, string operadorId, double saldoInformado, string? observacao)
        {
            if (!int.TryParse(caixaId, out int idCaixaInt))
                return (false, "Identificador de caixa inválido.");

            await _caixaLock.WaitAsync();
            try
            {
                var caixa = await _caixaService.GetById(idCaixaInt);
                if (caixa == null || !string.Equals(caixa.Status, "Aberto", StringComparison.OrdinalIgnoreCase))
                {
                    return (false, "Caixa inválido ou já fechado.");
                }

                double saldoInformadoArredondado = (double)Math.Round((decimal)saldoInformado, 2);

                caixa.DataFechamento = DateTime.UtcNow;
                caixa.OperadorFechamentoId = operadorId;
                caixa.SaldoFinalInformado = saldoInformadoArredondado;
                caixa.Status = "Fechado";
                caixa.Observacao += $"\nFechamento: {observacao}";

                bool salvou = await _caixaService.SaveAsync(caixa) > 0;
                if (salvou)
                {
                    try
                    {
                        await RegistrarMovimentacaoInternaAsync(caixa.Id.ToString(), operadorId, "Fechamento", "Saida", saldoInformadoArredondado, "Dinheiro", "Fechamento de Caixa");
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
            finally
            {
                _caixaLock.Release();
            }
        }

        public async Task<(bool Sucesso, string Mensagem)> TrocarOperadorCaixaAsync(string caixaId, string antigoOperadorId, string novoOperadorId)
        {
            if (!int.TryParse(caixaId, out int idCaixaInt))
                return (false, "Identificador de caixa inválido.");

            await _caixaLock.WaitAsync();
            try
            {
                var caixa = await _caixaService.GetById(idCaixaInt);
                if (caixa == null || !string.Equals(caixa.Status, "Aberto", StringComparison.OrdinalIgnoreCase))
                {
                    return (false, "Caixa inválido ou fechado.");
                }

                caixa.Observacao += $"\nTroca de operador. Saiu:{antigoOperadorId} Entrou:{novoOperadorId} as {DateTime.UtcNow:dd/MM/yyyy HH:mm}";
                await _caixaService.SaveAsync(caixa);

                try
                {
                    await RegistrarMovimentacaoInternaAsync(caixa.Id.ToString(), novoOperadorId, "TrocaOperador", "Calculo", 0.0, "-", $"Assumiu o caixa de {antigoOperadorId}");
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
            finally
            {
                _caixaLock.Release();
            }
        }

        public async Task RegistrarMovimentacaoAsync(string caixaId, string operadorId, string tipoMovimentacao, string natureza, double valor, string formaPagamento, string? descricao = null, string? comandaId = null)
        {
            await _caixaLock.WaitAsync();
            try
            {
                await RegistrarMovimentacaoInternaAsync(caixaId, operadorId, tipoMovimentacao, natureza, valor, formaPagamento, descricao, comandaId);
            }
            finally
            {
                _caixaLock.Release();
            }
        }

        private async Task RegistrarMovimentacaoInternaAsync(string caixaId, string operadorId, string tipoMovimentacao, string natureza, double valor, string formaPagamento, string? descricao = null, string? comandaId = null)
        {
            decimal valorDec = Math.Round((decimal)valor, 2);

            var mov = new MMovimentacaoCaixa
            {
                CaixaId = caixaId,
                OperadorId = operadorId,
                TipoMovimentacao = tipoMovimentacao,
                Natureza = natureza,
                Valor = (double)valorDec,
                FormaPagamento = formaPagamento,
                Descricao = descricao,
                ComandaId = comandaId,
                DataHora = DateTime.UtcNow
            };

            int idMov = await _movimentacaoCaixaService.SaveAsync(mov);
            if (idMov <= 0)
            {
                throw new InvalidOperationException("Não foi possível persistir a movimentação de caixa.");
            }

            // Atualiza saldo final calculado do caixa usando aritmética decimal exata
            if ((natureza == "Entrada" || natureza == "Saida") && int.TryParse(caixaId, out int idCaixaInt))
            {
                var caixa = await _caixaService.GetById(idCaixaInt);
                if (caixa != null && tipoMovimentacao != "Abertura" && tipoMovimentacao != "Fechamento")
                {
                    decimal saldoAtual = (decimal)caixa.SaldoFinalCalculado;
                    if (natureza == "Entrada") saldoAtual += valorDec;
                    if (natureza == "Saida") saldoAtual -= valorDec;

                    caixa.SaldoFinalCalculado = (double)Math.Round(saldoAtual, 2);
                    await _caixaService.SaveAsync(caixa);
                }
            }
        }

        public async Task<(bool Sucesso, string Mensagem)> RegistrarPagamentoParcialAsync(
            string comandaId, string formaPagamento, double valor, string operadorId, string? observacao = null)
        {
            decimal valorDec = Math.Round((decimal)valor, 2);
            if (valorDec <= 0m)
                return (false, "O valor do pagamento deve ser maior que zero.");

            await _caixaLock.WaitAsync();
            try
            {
                var resultCaixa = await ObterCaixaAbertoAsync();
                if (!resultCaixa.Sucesso || resultCaixa.CaixaAtual == null)
                    return (false, "Nenhum caixa aberto. Abra o caixa antes de registrar pagamentos.");

                var pagamento = new MPagamentoComanda
                {
                    ComandaId = comandaId,
                    FormaPagamento = formaPagamento,
                    Valor = (double)valorDec,
                    OperadorId = operadorId,
                    DataHora = DateTime.UtcNow,
                    Observacao = observacao
                };

                int idPagamento = await _pagamentoComandaService.SaveAsync(pagamento);
                if (idPagamento <= 0)
                    return (false, "Não foi possível confirmar a gravação do pagamento parcial.");

                try
                {
                    await RegistrarMovimentacaoInternaAsync(
                        resultCaixa.CaixaAtual.Id.ToString(),
                        operadorId,
                        "Venda",
                        "Entrada",
                        (double)valorDec,
                        formaPagamento,
                        $"Pagamento parcial Comanda #{comandaId} ({formaPagamento})",
                        comandaId
                    );
                }
                catch (Exception exMov)
                {
                    // Compensação transacional: se a movimentação no caixa falhar, estorna o pagamento da comanda para evitar divergência financeira
                    try { await _pagamentoComandaService.Delete(idPagamento); } catch { }
                    return (false, $"Falha ao registrar entrada no caixa (pagamento revertido): {exMov.Message}");
                }

                return (true, $"Pagamento de R$ {valorDec:F2} via {formaPagamento} registrado.");
            }
            catch (Exception ex)
            {
                return (false, $"Erro ao gravar o pagamento parcial: {ex.Message}");
            }
            finally
            {
                _caixaLock.Release();
            }
        }

        public async Task<(bool Quitada, double TotalPago, double SaldoPendente)> VerificarComandaQuitadaAsync(
            string comandaId, double totalComanda)
        {
            var todosPagamentos = await _pagamentoComandaService.GetAll();
            var pagamentos = (todosPagamentos ?? Enumerable.Empty<MPagamentoComanda>())
                .Where(p => p.ComandaId == comandaId)
                .ToList();

            decimal totalComandaDec = Math.Round((decimal)totalComanda, 2);
            decimal totalPagoDec = Math.Round(pagamentos.Sum(p => (decimal)p.Valor), 2);
            decimal saldoPendenteDec = totalComandaDec - totalPagoDec;

            bool quitada = saldoPendenteDec <= 0m;
            return (quitada, (double)totalPagoDec, (double)Math.Max(0m, saldoPendenteDec));
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
            decimal subtotalDec = (decimal)subtotalItens;
            decimal totalCouvertDec = cobrarCouvert ? Math.Round((decimal)valorCouvertUnitario * pessoas, 2) : 0m;
            decimal totalServicoDec = cobrarServico ? Math.Round(subtotalDec * ((decimal)percentualServico / 100m), 2) : 0m;
            decimal descontoDec = (decimal)desconto;

            decimal totalFinalDec = Math.Max(0m, subtotalDec + totalCouvertDec + totalServicoDec - descontoDec);
            decimal porPessoaDec = Math.Round(totalFinalDec / pessoas, 2);

            return ((double)porPessoaDec, (double)totalCouvertDec, (double)totalServicoDec);
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

            decimal totalConsolidadoDec = Math.Round(grupoComandas.Sum(c => (decimal)c.Total), 2);
            var todosPagamentos = await _pagamentoComandaService.GetAll() ?? new List<MPagamentoComanda>();
            var idsString = grupoComandas.Select(c => c.Id.ToString()).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var pagamentosDoGrupo = todosPagamentos
                .Where(p => !string.IsNullOrWhiteSpace(p.ComandaId) &&
                            (idsString.Contains(p.ComandaId) || idsString.Contains(p.ComandaId.Trim().TrimStart('#'))))
                .ToList();

            decimal totalPagoDec = Math.Round(pagamentosDoGrupo.Sum(p => (decimal)p.Valor), 2);
            decimal saldoPendenteDec = totalConsolidadoDec - totalPagoDec;

            if (saldoPendenteDec > 0m)
            {
                return (false, $"Não é possível fechar a comanda. Saldo pendente: R$ {saldoPendenteDec:F2}");
            }

            DateTime dataFechamento = DateTime.UtcNow;
            foreach (var c in grupoComandas)
            {
                c.Status = "Fechada";
                c.DataFechamento = dataFechamento;
                await _comandaService.SaveAsync(c);
            }

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
                    var todasMesas = await _mesaService.GetAll() ?? new List<Mesa>();
                    var mesa = todasMesas.FirstOrDefault(m =>
                        !string.IsNullOrWhiteSpace(m.Identificacao) &&
                        ComandaOperacionalService.MesmaMesa(m.Identificacao, mesaId));

                    if (mesa != null)
                    {
                        mesa.Status = StatusMesaConsts.Livre;
                        await _mesaService.SaveAsync(mesa);
                    }

                    await _mesaLayoutService.AtualizarStatusOperacionalAsync(mesaId, StatusMesaConsts.Livre, 0, 0);
                }
                else
                {
                    int totalCmds = outrasComandasMesa.Count;
                    double totalSub = (double)Math.Round(outrasComandasMesa.Sum(c => (decimal)c.Total), 2);
                    await _mesaLayoutService.AtualizarStatusOperacionalAsync(mesaId, StatusMesaConsts.Ocupada, totalCmds, totalSub);
                }
            }

            return (true, $"Comanda #{comandaPrincipal.Id} e todos os registros associados foram fechados com sucesso!");
        }
    }
}
