using JT_Classes.Restaurante;
using JT_Servicos.Restaurante;

namespace JT_Food.Shared.BusinessLogic
{
    public class ComercialLogic
    {
        private readonly CompraRestauranteService _compraService;
        private readonly ItemCompraService _itemCompraService;
        private readonly LoteInsumoService _loteInsumoService;
        private readonly InsumoService _insumoService;

        public ComercialLogic(
            CompraRestauranteService compraService,
            ItemCompraService itemCompraService,
            LoteInsumoService loteInsumoService,
            InsumoService insumoService)
        {
            _compraService = compraService;
            _itemCompraService = itemCompraService;
            _loteInsumoService = loteInsumoService;
            _insumoService = insumoService;
        }

        public async Task<(bool Sucesso, string Mensagem)> FinalizarCompraAsync(MCompra compra, List<MItemCompra> itens)
        {
            if (itens == null || !itens.Any())
                return (false, "A compra deve ter pelo menos um item.");

            
            compra.Valor = (decimal)itens.Sum(x => x.ValorTotal);
            
            bool salvouCompra = await _compraService.SaveAsync(compra) > 0;
            if (!salvouCompra)
                return (false, "Erro ao salvar o cabeçalho da compra.");

            foreach (var item in itens)
            {
                item.CompraId = compra.Id.ToString();
                
                if (await _itemCompraService.SaveAsync(item) > 0)
                {
                    var novoLote = new MLoteInsumo
                    {
                        UserId = compra.UserId,
                        InsumoId = item.InsumoId,
                        NomeInsumo = item.NomeInsumo,
                        QuantidadeInicial = item.Quantidade,
                        QuantidadeDisponivel = item.Quantidade,
                        CustoTotalLote = item.ValorTotal,
                        CustoUnitario = item.ValorUnitario,
                        DataEntrada = DateTime.UtcNow,
                        
                    };

                    await _loteInsumoService.SaveAsync(novoLote);
                    await AtualizarCustoMedioInsumoAsync(item.InsumoId);
                }
            }

            return (true, "Compra finalizada com sucesso. Estoque atualizado.");
        }

        private async Task AtualizarCustoMedioInsumoAsync(string? insumoId)
        {
            if (string.IsNullOrEmpty(insumoId)) return;

            if (int.TryParse(insumoId, out int id))
            {
                var insumo = await _insumoService.GetById(id);
                if (insumo != null)
                {
                    var lotes = await _loteInsumoService.Find($"InsumoId = '{insumoId}' ");
                    if (lotes != null && lotes.Any())
                    {
                        double custoTotal = lotes.Sum(x => x.CustoTotalLote);
                        double qtdTotal = lotes.Sum(x => x.QuantidadeDisponivel);

                        if (qtdTotal > 0)
                        {
                            insumo.CustoMedio = custoTotal / qtdTotal;
                            await _insumoService.SaveAsync(insumo);
                        }
                    }
                }
            }
        }
    }
}

