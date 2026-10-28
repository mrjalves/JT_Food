using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        private static readonly SemaphoreSlim _fifoLock = new(1, 1);

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
            if (quantidadeProduzida <= 0)
                return (false, "A quantidade produzida deve ser maior que zero.");

            if (!int.TryParse(produtoId, out int prodIdInt))
                return (false, "ID de produto inválido.");

            await _fifoLock.WaitAsync();
            try
            {
                var produto = await _produtoService.GetById(prodIdInt);
                if (produto == null) return (false, "Produto não encontrado.");
                if (!produto.PossuiReceita) return (true, "Produto sem receita. Sem necessidade de baixa de insumos.");

                var todasReceitas = await _receitaService.GetAll();
                var receitas = (todasReceitas ?? Enumerable.Empty<MReceita>())
                    .Where(r => r.ProdutoId == produtoId)
                    .ToList();
                if (!receitas.Any()) return (false, "Nenhuma receita cadastrada para este produto.");

                // Busca todos os lotes uma única vez fora do loop (evita N+1 chamadas HTTP/DB)
                var allLotesInsumo = (await _loteInsumoService.GetAll() ?? Enumerable.Empty<MLoteInsumo>()).ToList();

                decimal custoProducaoTotal = 0m;
                var lotesAtualizados = new List<MLoteInsumo>();
                var snapshotsOriginais = new Dictionary<int, (double QtdAnterior, string? StatusAnterior)>();
                var memoriasCalculo = new List<MMemoriaCalculo>();

                foreach (var rec in receitas)
                {
                    double quantidadeNecessariaTotal = Math.Round(rec.Quantidade * quantidadeProduzida, 6);
                    double quantidadeFaltante = quantidadeNecessariaTotal;

                    if (quantidadeFaltante > 0)
                    {
                        var lotesInsumo = allLotesInsumo
                            .Where(l => l.InsumoId == rec.InsumoId)
                            .ToList();

                        if (!lotesInsumo.Any())
                            return (false, $"Sem estoque para o insumo: {rec.NomeInsumo ?? rec.InsumoId}");

                        var lotesFiltrados = lotesInsumo
                            .Where(l => !string.Equals(l.Status, "Finalizado", StringComparison.OrdinalIgnoreCase))
                            .Where(l => l.QuantidadeDisponivel > 0)
                            .OrderBy(l => l.DataEntrada)
                            .ThenBy(l => l.Id)
                            .ToList();

                        if (!lotesFiltrados.Any())
                            return (false, $"Sem estoque disponível para o insumo: {rec.NomeInsumo ?? rec.InsumoId}");

                        foreach (var lote in lotesFiltrados)
                        {
                            if (quantidadeFaltante <= 0) break;

                            if (!snapshotsOriginais.ContainsKey(lote.Id))
                            {
                                snapshotsOriginais[lote.Id] = (lote.QuantidadeDisponivel, lote.Status);
                            }

                            double quantidadeConsumirDoLote = Math.Min(lote.QuantidadeDisponivel, quantidadeFaltante);

                            lote.QuantidadeDisponivel = Math.Round(lote.QuantidadeDisponivel - quantidadeConsumirDoLote, 6);
                            if (lote.QuantidadeDisponivel <= 0.000001)
                            {
                                lote.QuantidadeDisponivel = 0;
                                lote.Status = "Finalizado";
                            }

                            decimal custoFracaoDec = Math.Round((decimal)quantidadeConsumirDoLote * (decimal)lote.CustoUnitario, 4);
                            custoProducaoTotal += custoFracaoDec;
                            quantidadeFaltante = Math.Round(quantidadeFaltante - quantidadeConsumirDoLote, 6);

                            if (!lotesAtualizados.Any(x => x.Id == lote.Id))
                            {
                                lotesAtualizados.Add(lote);
                            }

                            memoriasCalculo.Add(new MMemoriaCalculo
                            {
                                DataProcessamento = DateTime.UtcNow,
                                LoteInsumoId = lote.Id.ToString(),
                                InsumoId = lote.InsumoId,
                                QuantidadeConsumida = quantidadeConsumirDoLote,
                                CustoUnitarioLote = lote.CustoUnitario,
                                CustoTotalFracao = (double)Math.Round(custoFracaoDec, 2),
                                Descricao = $"Baixa para produção de {quantidadeProduzida}x {produto.Nome}"
                            });
                        }

                        if (quantidadeFaltante > 0.000001)
                        {
                            return (false, $"Estoque insuficiente para o insumo {rec.NomeInsumo ?? rec.InsumoId}. Faltam {quantidadeFaltante} {rec.UnidadeMedida}");
                        }
                    }
                }

                // Persistência com compensação em caso de falha parcial
                var lotesSalvos = new List<MLoteInsumo>();
                try
                {
                    foreach (var lote in lotesAtualizados)
                    {
                        int idSalvo = await _loteInsumoService.SaveAsync(lote);
                        if (idSalvo <= 0)
                        {
                            throw new InvalidOperationException($"Não foi possível atualizar o saldo do lote #{lote.Id}.");
                        }
                        lotesSalvos.Add(lote);
                    }

                    decimal precoVendaTotalLote = (decimal)produto.Preco * (decimal)quantidadeProduzida;
                    decimal margemLucro = precoVendaTotalLote > 0m
                        ? Math.Round(((precoVendaTotalLote - custoProducaoTotal) / precoVendaTotalLote) * 100m, 2)
                        : 0m;

                    double custoFinalArredondado = (double)Math.Round(custoProducaoTotal, 2);

                    var producao = new MProducao
                    {
                        ProdutoId = produtoId,
                        NomeProduto = produto.Nome,
                        DataProducao = DateTime.UtcNow,
                        QuantidadeProduzida = quantidadeProduzida,
                        CustoTotalCalculado = custoFinalArredondado,
                        LucroLote = (double)margemLucro,
                        ResponsavelId = ResponsavelId
                    };

                    int idProd = await _producaoService.SaveAsync(producao);
                    if (idProd > 0 && producao.Id == 0)
                    {
                        producao.Id = idProd;
                    }

                    foreach (var mem in memoriasCalculo)
                    {
                        mem.ReferenciaId = producao.Id.ToString();
                        await _memoriaCalculoService.SaveAsync(mem);
                    }

                    return (true, $"Produção registrada com sucesso. Custo total: R$ {custoFinalArredondado:F2}");
                }
                catch (Exception ex)
                {
                    // Compensação: reverte lotes que já haviam sido alterados antes da falha
                    foreach (var loteRevert in lotesSalvos)
                    {
                        if (snapshotsOriginais.TryGetValue(loteRevert.Id, out var snap))
                        {
                            loteRevert.QuantidadeDisponivel = snap.QtdAnterior;
                            loteRevert.Status = snap.StatusAnterior ?? "Ativo";
                            try { await _loteInsumoService.SaveAsync(loteRevert); } catch { }
                        }
                    }
                    return (false, $"Erro transacional ao registrar produção (estoque revertido): {ex.Message}");
                }
            }
            finally
            {
                _fifoLock.Release();
            }
        }

        public async Task<(bool Sucesso, string Mensagem, double CustoAdicionais)> ProcessarAdicionaisItemAsync(string itemComandaId, string ResponsavelId)
        {
            await _fifoLock.WaitAsync();
            try
            {
                var todosAds = await _adicionaisItemComandaService.GetAll();
                var adicionais = (todosAds ?? Enumerable.Empty<MAdicionaisItemComanda>())
                    .Where(a => a.ItemComandaId.ToString() == itemComandaId)
                    .ToList();
                if (!adicionais.Any())
                    return (true, "Nenhum adicional a processar.", 0);

                decimal custoTotalAdicionais = 0m;
                var memoriasCalculo = new List<MMemoriaCalculo>();
                var lotesAtualizados = new List<MLoteInsumo>();

                var todasReceitas = (await _receitaService.GetAll() ?? Enumerable.Empty<MReceita>()).ToList();
                var todosLotes = (await _loteInsumoService.GetAll() ?? Enumerable.Empty<MLoteInsumo>()).ToList();

                // Cacheia produtos já buscados para evitar chamadas repetidas dentro do loop
                var cacheProdutos = new Dictionary<int, MProduto>();

                foreach (var adicional in adicionais)
                {
                    if (!int.TryParse(adicional.ProdutoId, out int prodAdicId)) continue;

                    if (!cacheProdutos.TryGetValue(prodAdicId, out var produto))
                    {
                        var buscado = await _produtoService.GetById(prodAdicId);
                        if (buscado == null) continue;
                        produto = buscado;
                        cacheProdutos[prodAdicId] = produto;
                    }

                    if (!produto.PossuiReceita) continue;

                    var receitas = todasReceitas
                        .Where(r => r.ProdutoId == adicional.ProdutoId)
                        .ToList();
                    if (!receitas.Any()) continue;

                    foreach (var rec in receitas)
                    {
                        double quantidadeNecessaria = Math.Round(rec.Quantidade * adicional.Quantidade, 6);
                        double quantidadeFaltante = quantidadeNecessaria;

                        var allLotes = todosLotes
                            .Where(l => l.InsumoId == rec.InsumoId)
                            .ToList();

                        if (!allLotes.Any())
                            return (false, $"Sem estoque para o insumo do adicional: {rec.NomeInsumo ?? rec.InsumoId}", 0);

                        var lotesFiltrados = allLotes
                            .Where(l => !string.Equals(l.Status, "Finalizado", StringComparison.OrdinalIgnoreCase))
                            .Where(l => l.QuantidadeDisponivel > 0)
                            .OrderBy(l => l.DataEntrada)
                            .ThenBy(l => l.Id)
                            .ToList();

                        if (!lotesFiltrados.Any())
                            return (false, $"Sem estoque disponível para o insumo do adicional: {rec.NomeInsumo ?? rec.InsumoId}", 0);

                        foreach (var lote in lotesFiltrados)
                        {
                            if (quantidadeFaltante <= 0) break;

                            double qtyConsumo = Math.Min(lote.QuantidadeDisponivel, quantidadeFaltante);

                            lote.QuantidadeDisponivel = Math.Round(lote.QuantidadeDisponivel - qtyConsumo, 6);
                            if (lote.QuantidadeDisponivel <= 0.000001)
                            {
                                lote.QuantidadeDisponivel = 0;
                                lote.Status = "Finalizado";
                            }

                            decimal custoFracaoDec = Math.Round((decimal)qtyConsumo * (decimal)lote.CustoUnitario, 4);
                            custoTotalAdicionais += custoFracaoDec;
                            quantidadeFaltante = Math.Round(quantidadeFaltante - qtyConsumo, 6);

                            if (!lotesAtualizados.Any(x => x.Id == lote.Id))
                            {
                                lotesAtualizados.Add(lote);
                            }

                            memoriasCalculo.Add(new MMemoriaCalculo
                            {
                                DataProcessamento = DateTime.UtcNow,
                                LoteInsumoId = lote.Id.ToString(),
                                InsumoId = lote.InsumoId,
                                QuantidadeConsumida = qtyConsumo,
                                CustoUnitarioLote = lote.CustoUnitario,
                                CustoTotalFracao = (double)Math.Round(custoFracaoDec, 2),
                                Descricao = $"Baixa para adicional {adicional.NomeProduto} (Item {itemComandaId})"
                            });
                        }

                        if (quantidadeFaltante > 0.000001)
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

                return (true, "Adicionais baixados com sucesso.", (double)Math.Round(custoTotalAdicionais, 2));
            }
            finally
            {
                _fifoLock.Release();
            }
        }
    }
}
