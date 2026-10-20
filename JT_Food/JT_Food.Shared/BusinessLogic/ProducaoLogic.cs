using JT_Classes.Restaurante;
using JT_Servicos.Restaurante;

namespace JT_Food.Shared.BusinessLogic
{
    public class ProducaoLogic
    {
        private readonly ProdutoRestauranteService _produtoService;
        private readonly ReceitaService _receitaService;
        private readonly LoteInsumoService _loteInsumoService;
        private readonly ProducaoService _producaoService;
        private readonly MemoriaCalculoService _memoriaCalculoService;
        private readonly AdicionaisItemComandaService _adicionaisItemComandaService;

        public ProducaoLogic(
            ProdutoRestauranteService produtoService,
            ReceitaService receitaService,
            LoteInsumoService loteInsumoService,
            ProducaoService producaoService,
            MemoriaCalculoService memoriaCalculoService,
            AdicionaisItemComandaService adicionaisItemComandaService)
        {
            _produtoService = produtoService;
            _receitaService = receitaService;
            _loteInsumoService = loteInsumoService;
            _producaoService = producaoService;
            _memoriaCalculoService = memoriaCalculoService;
            _adicionaisItemComandaService = adicionaisItemComandaService;
        }

        public async Task<(bool Sucesso, string Mensagem)> ProcessarProducaoAsync(string produtoId, double quantidadeProduzida, string ResponsavelId)
        {
            var produto = await _produtoService.GetById(Convert.ToInt32(produtoId));
            if (produto == null) return (false, "Produto não encontrado.");
            if (!produto.PossuiReceita) return (true, "Produto sem receita. Sem necessidade de baixa de insumos.");

            var todasReceitas = await _receitaService.GetAll();
            var receitas = (todasReceitas ?? Enumerable.Empty<MReceita>())
                .Where(r => r.ProdutoId == produtoId)
                .ToList();
            if (!receitas.Any()) return (false, "Nenhuma receita cadastrada para este produto.");

            double custoProducaoTotal = 0;
            var lotesAtualizados = new List<MLoteInsumo>();
            var memoriasCalculo = new List<MMemoriaCalculo>();

            foreach (var rec in receitas)
            {
                double quantidadeNecessariaTotal = rec.Quantidade * quantidadeProduzida;
                double quantidadeFaltante = quantidadeNecessariaTotal;

                if (quantidadeFaltante > 0)
                {
                    var allLotesInsumo = await _loteInsumoService.GetAll();
                    var lotesInsumo = (allLotesInsumo ?? Enumerable.Empty<MLoteInsumo>())
                        .Where(l => l.InsumoId == rec.InsumoId)
                        .ToList();
                    
                    if (!lotesInsumo.Any())
                        return (false, $"Sem estoque para o insumo: {rec.NomeInsumo ?? rec.InsumoId}");

                    var lotesFiltrados = lotesInsumo
                        .Where(l => l.Status != "Finalizado")
                        .Where(l => l.QuantidadeDisponivel > 0)
                        .OrderBy(l => l.DataEntrada)
                        .ToList();

                    if (!lotesFiltrados.Any())
                        return (false, $"Sem estoque disponível para o insumo: {rec.NomeInsumo ?? rec.InsumoId}");

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
                        custoProducaoTotal += custoFracao;
                        quantidadeFaltante -= quantidadeConsumirDoLote;

                        lotesAtualizados.Add(lote);
                        
                        // Registrar memoria
                        memoriasCalculo.Add(new MMemoriaCalculo
                        {
                            DataProcessamento = DateTime.UtcNow,
                            LoteInsumoId = lote.Id.ToString(),
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
            }

            foreach (var lote in lotesAtualizados)
            {
                await _loteInsumoService.SaveAsync(lote);
            }

            double precoVendaTotalLote = (double)produto.Preco * quantidadeProduzida;
            double margemLucro = precoVendaTotalLote > 0 ? ((precoVendaTotalLote - custoProducaoTotal) / precoVendaTotalLote) * 100 : 0;

            var producao = new MProducao
            {
                ProdutoId = produtoId,
                NomeProduto = produto.Nome,
                DataProducao = DateTime.UtcNow,
                QuantidadeProduzida = quantidadeProduzida,
                CustoTotalCalculado = custoProducaoTotal,
                LucroLote = margemLucro,
                ResponsavelId = ResponsavelId
            };

            await _producaoService.SaveAsync(producao);

            foreach (var mem in memoriasCalculo)
            {
                mem.ReferenciaId = producao.Id.ToString();
                await _memoriaCalculoService.SaveAsync(mem);
            }

            return (true, $"Produção registrada com sucesso. Custo total: R$ {custoProducaoTotal:F2}");
        }

        public async Task<(bool Sucesso, string Mensagem, double CustoAdicionais)> ProcessarAdicionaisItemAsync(string itemComandaId, string ResponsavelId)
        {
            var todosAds = await _adicionaisItemComandaService.GetAll();
            var adicionais = (todosAds ?? Enumerable.Empty<MAdicionaisItemComanda>())
                .Where(a => a.ItemComandaId.ToString() == itemComandaId)
                .ToList();
            if (!adicionais.Any())
                return (true, "Nenhum adicional a processar.", 0);

            double custoTotalAdicionais = 0;
            var memoriasCalculo = new List<MMemoriaCalculo>();
            var lotesAtualizados = new List<MLoteInsumo>();

            var todasReceitas = await _receitaService.GetAll();
            var todosLotes = await _loteInsumoService.GetAll();

            foreach (var adicional in adicionais)
            {
                var produto = await _produtoService.GetById(Convert.ToInt32(adicional.ProdutoId));
                if (produto == null) continue;

                if (!produto.PossuiReceita) continue;

                var receitas = (todasReceitas ?? Enumerable.Empty<MReceita>())
                    .Where(r => r.ProdutoId == adicional.ProdutoId)
                    .ToList();
                if (!receitas.Any()) continue;

                foreach (var rec in receitas)
                {
                    double quantidadeNecessaria = rec.Quantidade * adicional.Quantidade;
                    double quantidadeFaltante = quantidadeNecessaria;

                    var allLotes = (todosLotes ?? Enumerable.Empty<MLoteInsumo>())
                        .Where(l => l.InsumoId == rec.InsumoId)
                        .ToList();
                    
                    if (!allLotes.Any())
                        return (false, $"Sem estoque para o insumo do adicional: {rec.NomeInsumo ?? rec.InsumoId}", 0);

                    var lotesFiltrados = allLotes
                        .Where(l => l.Status != "Finalizado")
                        .Where(l => l.QuantidadeDisponivel > 0)
                        .OrderBy(l => l.DataEntrada)
                        .ToList();

                    if (!lotesFiltrados.Any())
                        return (false, $"Sem estoque disponível para o insumo do adicional: {rec.NomeInsumo ?? rec.InsumoId}", 0);

                    foreach (var lote in lotesFiltrados)
                    {
                        if (quantidadeFaltante <= 0) break;

                        double qtyConsumo = Math.Min(lote.QuantidadeDisponivel, quantidadeFaltante);
                        
                        lote.QuantidadeDisponivel -= qtyConsumo;
                        if (lote.QuantidadeDisponivel == 0)
                            lote.Status = "Finalizado";

                        double custoFracao = qtyConsumo * lote.CustoUnitario;
                        custoTotalAdicionais += custoFracao;
                        quantidadeFaltante -= qtyConsumo;

                        lotesAtualizados.Add(lote);

                        memoriasCalculo.Add(new MMemoriaCalculo
                        {
                            DataProcessamento = DateTime.UtcNow,
                            LoteInsumoId = lote.Id.ToString(),
                            InsumoId = lote.InsumoId,
                            QuantidadeConsumida = qtyConsumo,
                            CustoUnitarioLote = lote.CustoUnitario,
                            CustoTotalFracao = custoFracao,
                            Descricao = $"Baixa para adicional {adicional.NomeProduto} (Item {itemComandaId})"
                        });
                    }

                    if (quantidadeFaltante > 0)
                    {
                         return (false, $"Estoque insuficiente para o adicional {adicional.NomeProduto}. Falta insumo {rec.NomeInsumo ?? rec.InsumoId}", 0);
                    }
                }
            }

            foreach (var lote in lotesAtualizados)
            {
                await _loteInsumoService.SaveAsync(lote);
            }

            foreach (var mem in memoriasCalculo)
            {
                mem.ReferenciaId = "Adicional-" + itemComandaId;
                await _memoriaCalculoService.SaveAsync(mem);
            }

            return (true, "Adicionais baixados com sucesso.", custoTotalAdicionais);
        }
    }
}

