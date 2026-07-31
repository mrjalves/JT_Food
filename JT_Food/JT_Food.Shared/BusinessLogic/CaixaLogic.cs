using JT_Classes.Restaurante;
using JT_Servicos.Restaurante;

namespace JT_Food.Shared.BusinessLogic
{
    public class CaixaLogic
    {
        private readonly CaixaService _caixaService;
        private readonly MovimentacaoCaixaService _movimentacaoCaixaService;
        private readonly PagamentoComandaService _pagamentoComandaService;

        public CaixaLogic(
            CaixaService caixaService,
            MovimentacaoCaixaService movimentacaoCaixaService,
            PagamentoComandaService pagamentoComandaService)
        {
            _caixaService = caixaService;
            _movimentacaoCaixaService = movimentacaoCaixaService;
            _pagamentoComandaService = pagamentoComandaService;
        }

        public async Task<(bool Sucesso, string Mensagem, MCaixa? CaixaAtual)> ObterCaixaAbertoAsync()
        {
            var caixas = await _caixaService.Find("Status = 'Aberto'");
            if (caixas != null && caixas.Any())
            {
                return (true, "Caixa aberto encontrado.", caixas.FirstOrDefault());
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

            // Update running total
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

        /// <summary>
        /// Registra um pagamento parcial na comanda e lança a movimentação no caixa aberto.
        /// Suporta rateio de conta com múltiplas formas de pagamento.
        /// </summary>
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

            // Registrar a entrada no caixa para manter o saldo calculado
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

        /// <summary>
        /// Verifica se a soma dos pagamentos registrados cobre o total da comanda.
        /// A comanda só pode ser baixada quando SaldoPendente &lt;= 0.
        /// </summary>
        public async Task<(bool Quitada, double TotalPago, double SaldoPendente)> VerificarComandaQuitadaAsync(
            string comandaId, double totalComanda)
        {
            var pagamentos = await _pagamentoComandaService.Find($"ComandaId = '{comandaId}'");
            double totalPago = pagamentos?.Sum(p => p.Valor) ?? 0;
            double saldoPendente = totalComanda - totalPago;

            // Tolerância de 1 centavo para evitar problemas de ponto flutuante
            bool quitada = saldoPendente <= 0.01;

            return (quitada, totalPago, saldoPendente < 0 ? 0 : saldoPendente);
        }
    }
}
