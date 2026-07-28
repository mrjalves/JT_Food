using JT_Classes.Restaurante;
using JT_Food.Shared.Services;

namespace JT_Food.Shared.BusinessLogic
{
    public static class CaixaLogic
    {
        public static (bool Sucesso, string Mensagem, MCaixa? CaixaAtual) ObterCaixaAberto()
        {
            var caixas = CaixaService.Find(new Dictionary<string, object> { { "Status", "Aberto" } });
            if (caixas != null && caixas.Any())
            {
                return (true, "Caixa aberto encontrado.", caixas.FirstOrDefault());
            }

            return (false, "Nenhum caixa aberto neste momento.", null);
        }

        public static (bool Sucesso, string Mensagem) AbrirCaixa(string operadorId, double saldoInicial, string? observacao)
        {
            var situacaoAtual = ObterCaixaAberto();
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

            bool salvou = CaixaService.Save(novoCaixa);
            
            if (salvou)
            {
                RegistrarMovimentacao(novoCaixa.Id.ToString(), operadorId, "Abertura", "Entrada", saldoInicial, "Dinheiro", "Abertura de Caixa");
                return (true, "Caixa aberto com sucesso.");
            }

            return (false, "Erro ao abrir o caixa.");
        }

        public static (bool Sucesso, string Mensagem) FecharCaixa(string caixaId, string operadorId, double saldoInformado, string? observacao)
        {
            var caixa = CaixaService.GetById(Convert.ToInt32(caixaId));
            if (caixa == null || caixa.Status != "Aberto")
            {
                return (false, "Caixa inválido ou já fechado.");
            }

            caixa.DataFechamento = DateTime.UtcNow;
            caixa.OperadorFechamentoId = operadorId;
            caixa.SaldoFinalInformado = saldoInformado;
            caixa.Status = "Fechado";
            caixa.Observacao += $"\nFechamento: {observacao}";

            bool salvou = CaixaService.Save(caixa);
            if (salvou)
            {
                RegistrarMovimentacao(caixa.Id.ToString(), operadorId, "Fechamento", "Saida", saldoInformado, "Dinheiro", "Fechamento de Caixa");
                return (true, "Caixa fechado com sucesso.");
            }

            return (false, "Erro ao fechar o caixa.");
        }

        public static (bool Sucesso, string Mensagem) TrocarOperadorCaixa(string caixaId, string antigoOperadorId, string novoOperadorId)
        {
             var caixa = CaixaService.GetById(Convert.ToInt32(caixaId));
            if (caixa == null || caixa.Status != "Aberto")
            {
                return (false, "Caixa inválido ou fechado.");
            }
            
            caixa.Observacao += $"\nTroca de operador. Saiu:{antigoOperadorId} Entrou:{novoOperadorId} as {DateTime.UtcNow:dd/MM/yyyy HH:mm}";
            CaixaService.Save(caixa);

            RegistrarMovimentacao(caixa.Id.ToString(), novoOperadorId, "TrocaOperador", "Calculo", 0.0, "-", $"Assumiu o caixa de {antigoOperadorId}");

            return (true, "Troca de operador registrada com sucesso.");
        }

        public static void RegistrarMovimentacao(string caixaId, string operadorId, string tipoMovimentacao, string natureza, double valor, string formaPagamento, string? descricao = null, string? comandaId = null)
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

            MovimentacaoCaixaService.Save(mov);

            // Update running total
            if (natureza == "Entrada" || natureza == "Saida")
            {
                 var caixa = CaixaService.GetById(Convert.ToInt32(caixaId));
                 if (caixa != null && tipoMovimentacao != "Abertura" && tipoMovimentacao != "Fechamento")
                 {
                     if (natureza == "Entrada") caixa.SaldoFinalCalculado += valor;
                     if (natureza == "Saida") caixa.SaldoFinalCalculado -= valor;

                     CaixaService.Save(caixa);
                 }
            }
        }

        /// <summary>
        /// Registra um pagamento parcial na comanda e lança a movimentação no caixa aberto.
        /// Suporta rateio de conta com múltiplas formas de pagamento.
        /// </summary>
        public static (bool Sucesso, string Mensagem) RegistrarPagamentoParcial(
            string comandaId, string formaPagamento, double valor, string operadorId, string? observacao = null)
        {
            if (valor <= 0)
                return (false, "O valor do pagamento deve ser maior que zero.");

            var resultCaixa = ObterCaixaAberto();
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

            bool salvo = PagamentoComandaService.Save(pagamento);
            if (!salvo)
                return (false, "Erro ao gravar o pagamento parcial.");

            // Registrar a entrada no caixa para manter o saldo calculado
            RegistrarMovimentacao(
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
        public static (bool Quitada, double TotalPago, double SaldoPendente) VerificarComandaQuitada(
            string comandaId, double totalComanda)
        {
            var pagamentos = PagamentoComandaService.Find(new Dictionary<string, object> { { "ComandaId", comandaId } });
            double totalPago = pagamentos?.Sum(p => p.Valor) ?? 0;
            double saldoPendente = totalComanda - totalPago;

            // Tolerância de 1 centavo para evitar problemas de ponto flutuante
            bool quitada = saldoPendente <= 0.01;

            return (quitada, totalPago, saldoPendente < 0 ? 0 : saldoPendente);
        }
    }
}

