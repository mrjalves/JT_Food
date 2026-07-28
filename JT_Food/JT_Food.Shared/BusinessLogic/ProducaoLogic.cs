using JT_Aplicacao.Classes;
using JT_UserInterface.Shared.Services.Comum;
using JT_Classes.Restaurante;
using JT_Food.Shared.Services;

namespace JT_Food.Shared.BusinessLogic
{
    public static class ProducaoLogic
    {
        /// <summary>
        /// Processa a produção de um item que tem receita e dá baixa nos insumos utilizando FIFO baseados nos lotes disponíveis.
        /// </summary>
        public static (bool Sucesso, string Mensagem) ProcessarProducao(string produtoId, double quantidadeProduzida, string operadorId)
        {
            var produto = ProdutoService.GetById(Convert.ToInt32(produtoId));
            if (produto == null) return (false, "Produto não encontrado.");

            // Get the Recipe items for this product
            var receitas = ReceitaService.Find(new Dictionary<string, object> { { "ProdutoId", produtoId } });
            if (receitas == null || !receitas.Any()) return (false, "Nenhuma receita cadastrada para este produto.");

            double custoTotalProducao = 0;
            List<MLoteInsumo> lotesAtualizados = new List<MLoteInsumo>();
            List<MMemoriaCalculo> memoriasCalculo = new List<MMemoriaCalculo>();

            foreach (var rec in receitas)
            {
                double quantidadeNecessariaTotal = rec.Quantidade * quantidadeProduzida;
                double quantidadeFaltante = quantidadeNecessariaTotal;

                // Retrieve all unexhausted batches for this ingredient, ordered by DataEntrada (FIFO)
                // Firestore não suporta o filtro complexo e ORDER BY via dicionário, então filtramos/ordenamos na memória
                var lotesInsumo = LoteInsumoService.Find(new Dictionary<string, object> { { "InsumoId", rec.InsumoId } });
                
                if (lotesInsumo == null || !lotesInsumo.Any())
                {
                    return (false, $"Sem estoque para o insumo: {rec.NomeInsumo ?? rec.InsumoId}");
                }

                // Filtrar apenas lotes com estoque > 0 e ordenar por DataEntrada (FIFO)
                var lotesFiltrados = lotesInsumo
                    .Where(l => l.QuantidadeDisponivel > 0)
                    .OrderBy(l => l.DataEntrada)
                    .ToList();

                if (!lotesFiltrados.Any())
                {
                    return (false, $"Sem estoque disponível para o insumo: {rec.NomeInsumo ?? rec.InsumoId}");
                }

                foreach (var lote in lotesFiltrados)
                {
                    if (quantidadeFaltante <= 0) break;

                    double quantidadeConsumirDoLote = Math.Min(lote.QuantidadeDisponivel, quantidadeFaltante);
                    
                    lote.QuantidadeDisponivel -= quantidadeConsumirDoLote;
                    if (lote.QuantidadeDisponivel == 0)
                    {
                        lote.Status = "Finalizado";
                    }

                    double custoFracao = quantidadeConsumirDoLote * lote.CustoUnitario;
                    custoTotalProducao += custoFracao;
                    quantidadeFaltante -= quantidadeConsumirDoLote;

                    lotesAtualizados.Add(lote);

                    // Create log
                    memoriasCalculo.Add(new MMemoriaCalculo
                    {
                        ItemId = produtoId,
                        LoteInsumoId = lote.Id.ToString(), // Assumed Id comes from ClasseBase int properties
                        InsumoId = lote.InsumoId,
                        QuantidadeConsumida = quantidadeConsumirDoLote,
                        CustoUnitarioLote = lote.CustoUnitario,
                        CustoTotalFracao = custoFracao,
                        Descricao = $"Baixa para produção de {quantidadeProduzida}x {produto.Nome}"
                    });
                }

                if (quantidadeFaltante > 0)
                {
                    return (false, $"Estoque insuficiente para o insumo {rec.NomeInsumo ?? rec.InsumoId}. Faltam {quantidadeFaltante} {rec.UnidadeMedida}");
                }
            }

            // All ingredients processed successfully, now persist to DB
            foreach (var lote in lotesAtualizados)
            {
                LoteInsumoService.Save(lote);
            }

            double precoVendaTotalLote = (double)produto.Preco * quantidadeProduzida;
            double lucroLote = precoVendaTotalLote - custoTotalProducao;

            var producao = new MProducao
            {
                UserId = operadorId, // Assuming UserId
                ProdutoId = produtoId,
                NomeProduto = produto.Nome,
                QuantidadeProduzida = quantidadeProduzida,
                ResponsavelId = operadorId,
                CustoTotalCalculado = custoTotalProducao,
                CustoUnitarioSugerido = quantidadeProduzida > 0 ? (custoTotalProducao / quantidadeProduzida) : 0,
                PrecoVendaTotal = precoVendaTotalLote,
                LucroLote = lucroLote,
                Observacoes = "Produzido via regra de consumo FIFO"
            };
            ProducaoService.Save(producao);

            foreach (var mem in memoriasCalculo)
            {
                mem.ReferenciaId = producao.Id.ToString();
                MemoriaCalculoService.Save(mem);
            }

            return (true, $"Produção finalizada com sucesso! Custo: R$ {custoTotalProducao:F2}, Lucro: R$ {lucroLote:F2}");
        }

        /// <summary>
        /// Processa os adicionais vinculados a um MItemComanda, dando baixa FIFO nos insumos
        /// de cada adicional que possui receita, somando ao custo da receita principal.
        /// </summary>
        public static (bool Sucesso, string Mensagem, double CustoAdicionais) ProcessarAdicionaisItem(string itemComandaId, string operadorId)
        {
            var adicionais = AdicionaisItemComandaService.Find(new Dictionary<string, object> { { "ItemComandaId", itemComandaId } });
            if (adicionais == null || !adicionais.Any())
                return (true, "Nenhum adicional a processar.", 0);

            double custoTotalAdicionais = 0;

            foreach (var adicional in adicionais)
            {
                var produto = ProdutoService.GetById(Convert.ToInt32(adicional.ProdutoId));
                if (produto == null) continue;

                // Só dá baixa em estoque se o produto adicional tiver receita
                if (!produto.PossuiReceita) continue;

                var receitas = ReceitaService.Find(new Dictionary<string, object> { { "ProdutoId", adicional.ProdutoId } });
                if (receitas == null || !receitas.Any()) continue;

                foreach (var rec in receitas)
                {
                    double quantidadeNecessaria = rec.Quantidade * adicional.Quantidade;
                    double quantidadeFaltante = quantidadeNecessaria;

                    var allLotes = LoteInsumoService.Find(new Dictionary<string, object> { { "InsumoId", rec.InsumoId } });

                    if (allLotes == null || !allLotes.Any())
                        return (false, $"Sem estoque para o insumo do adicional: {rec.NomeInsumo ?? rec.InsumoId}", 0);

                    var lotesDisponiveis = allLotes
                        .Where(l => l.QuantidadeDisponivel > 0)
                        .OrderBy(l => l.DataEntrada)
                        .ToList();

                    if (!lotesDisponiveis.Any())
                        return (false, $"Sem estoque para o insumo do adicional: {rec.NomeInsumo ?? rec.InsumoId}", 0);

                    foreach (var lote in lotesDisponiveis)
                    {
                        if (quantidadeFaltante <= 0) break;

                        double qtdConsumida = Math.Min(lote.QuantidadeDisponivel, quantidadeFaltante);
                        lote.QuantidadeDisponivel -= qtdConsumida;
                        if (lote.QuantidadeDisponivel == 0) lote.Status = "Finalizado";

                        double custoFracao = qtdConsumida * lote.CustoUnitario;
                        custoTotalAdicionais += custoFracao;
                        quantidadeFaltante -= qtdConsumida;

                        LoteInsumoService.Save(lote);

                        MemoriaCalculoService.Save(new MMemoriaCalculo
                        {
                            ItemId = itemComandaId,
                            LoteInsumoId = lote.Id.ToString(),
                            InsumoId = lote.InsumoId,
                            QuantidadeConsumida = qtdConsumida,
                            CustoUnitarioLote = lote.CustoUnitario,
                            CustoTotalFracao = custoFracao,
                            Descricao = $"Baixa para adicional '{produto.Nome}' no ItemComanda #{itemComandaId}"
                        });
                    }

                    if (quantidadeFaltante > 0)
                        return (false, $"Estoque insuficiente para {rec.NomeInsumo ?? rec.InsumoId} (adicional {produto.Nome}).", 0);
                }
            }

            return (true, $"Adicionais processados. Custo extra: R$ {custoTotalAdicionais:F2}", custoTotalAdicionais);
        }
    }
}





