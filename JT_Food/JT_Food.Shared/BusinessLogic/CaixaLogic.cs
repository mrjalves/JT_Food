using System;
using System.Linq;
using System.Threading.Tasks;
using JT_Classes.Restaurante;
using JT_Food.Shared.Domain.Enums;
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

        public async Task<(bool Sucesso, string Mensagem)> AbrirCaixaAsync(string operadorId, double saldoInicial, string? observacao)
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

            bool salvou = await _caixaService.SaveAsync(novoCaixa) > 0;
            
            if (salvou)
            {
                await RegistrarMovimentacaoAsync(novoCaixa.Id.ToString(), operadorId, "Abertura", "Entrada", saldoInicial, "Dinheiro", "Abertura de Caixa");
                return (true, "Caixa aberto com sucesso.");
            }

            return (false, "Erro ao abrir o caixa.");
        }

        public async Task<(bool Sucesso, string Mensagem)> FecharCaixaAsync(string caixaId, string operadorId, double saldoInformado, string? observacao)
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
                await RegistrarMovimentacaoAsync(caixa.Id.ToString(), operadorId, "Fechamento", "Saida", saldoInformado, "Dinheiro", "Fechamento de Caixa");
                return (true, "Caixa fechado com sucesso.");
            }

            return (false, "Erro ao fechar o caixa.");
        }

        public async Task<(bool Sucesso, string Mensagem)> TrocarOperadorCaixaAsync(string caixaId, string antigoOperadorId, string novoOperadorId)
        {
            var caixa = await _caixaService.GetById(Convert.ToInt32(caixaId));
            if (caixa == null || caixa.Status != "Aberto")
            {
                return (false, "Caixa inválido ou fechado.");
            }
            
            caixa.Observacao += $"\nTroca de operador. Saiu:{antigoOperadorId} Entrou:{novoOperadorId} as {DateTime.UtcNow:dd/MM/yyyy HH:mm}";
            await _caixaService.SaveAsync(caixa);

            await RegistrarMovimentacaoAsync(caixa.Id.ToString(), novoOperadorId, "TrocaOperador", "Calculo", 0.0, "-", $"Assumiu o caixa de {antigoOperadorId}");

            return (true, "Troca de operador registrada com sucesso.");
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
            var comanda = await _comandaService.GetById(comandaId);
            if (comanda == null) return (false, "Comanda não encontrada.");

            var (quitada, totalPago, saldoPendente) = await VerificarComandaQuitadaAsync(comanda.Id.ToString(), comanda.Total);
            if (!quitada)
            {
                return (false, $"Não é possível fechar a comanda. Saldo pendente: R$ {saldoPendente:F2}");
            }

            comanda.Status = "Fechada";
            comanda.DataFechamento = DateTime.UtcNow;
            await _comandaService.SaveAsync(comanda);

            // Verificar se ainda há outras comandas abertas na mesa
            if (!string.IsNullOrWhiteSpace(comanda.MesaId))
            {
                var todasComandas = await _comandaService.GetAll();
                string mesaNorm = comanda.MesaId.Replace("Mesa ", "").Trim();
                var outrasComandas = (todasComandas ?? Enumerable.Empty<Comanda>())
                    .Where(c => c.Id != comandaId && c.Status != "Fechada" && c.Status != "Cancelada" &&
                        !string.IsNullOrWhiteSpace(c.MesaId) &&
                        (c.MesaId.Equals(comanda.MesaId, StringComparison.OrdinalIgnoreCase) ||
                         c.MesaId.Replace("Mesa ", "").Trim().Equals(mesaNorm, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                if (!outrasComandas.Any())
                {
                    // Libera a mesa no banco
                    var todasMesas = await _mesaService.GetAll();
                    var mesa = (todasMesas ?? Enumerable.Empty<Mesa>())
                        .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m.Identificacao) &&
                            (m.Identificacao.Equals(comanda.MesaId, StringComparison.OrdinalIgnoreCase) ||
                             m.Identificacao.Replace("Mesa ", "").Trim().Equals(mesaNorm, StringComparison.OrdinalIgnoreCase)));

                    if (mesa != null)
                    {
                        mesa.Status = StatusMesaConsts.Livre;
                        await _mesaService.SaveAsync(mesa);
                    }

                    // Libera no mapa do salão
                    await _mesaLayoutService.AtualizarStatusOperacionalAsync(comanda.MesaId, StatusMesaConsts.Livre, 0, 0);
                }
            }

            return (true, $"Comanda #{comanda.Id} encerrada com sucesso!");
        }
    }
}
