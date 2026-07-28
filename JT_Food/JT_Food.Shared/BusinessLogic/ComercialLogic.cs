using JT_Classes.Restaurante;
using JT_Food.Shared.Services;

namespace JT_Food.Shared.BusinessLogic
{
    public static class ComercialLogic
    {
        public static (bool Sucesso, string Mensagem) FinalizarCompra(MCompra compra, List<MItemCompra> itens)
        {
            if (itens == null || !itens.Any())
                return (false, "A compra deve ter pelo menos um item.");

            // 1. Salvar a Compra principal
            compra.Status = "Finalizada";
            compra.Valor = (decimal)itens.Sum(x => x.ValorTotal);
            if (!CompraService.Save(compra))
                return (false, "Erro ao salvar o cabeçalho da compra.");

            foreach (var item in itens)
            {
                // 2. Vincular e salvar o item da compra
                item.CompraId = compra.Id.ToString();
                if (!ItemCompraService.Save(item))
                    continue;

                // 3. Gerar Lote de Insumo (Estoque Automático)
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
                    Status = "Aberto"
                };

                LoteInsumoService.Save(novoLote);

                // 4. Atualizar Custo Médio no Insumo (Simplificado)
                AtualizarCustoMedioInsumo(item.InsumoId);
            }

            return (true, "Compra finalizada e estoque atualizado com sucesso!");
        }

        private static void AtualizarCustoMedioInsumo(string insumoId)
        {
            if (int.TryParse(insumoId, out int id))
            {
                var insumo = InsumoService.GetById(id);
                if (insumo != null)
                {
                    var lotes = LoteInsumoService.Find(new Dictionary<string, object> { { "InsumoId", insumoId }, { "Status", "Aberto" } });
                    if (lotes.Any())
                    {
                        double custoTotal = lotes.Sum(x => x.CustoTotalLote);
                        double qtdTotal = lotes.Sum(x => x.QuantidadeDisponivel);
                        if (qtdTotal > 0)
                        {
                            insumo.CustoMedio = custoTotal / qtdTotal;
                            InsumoService.Save(insumo);
                        }
                    }
                }
            }
        }
    }
}



