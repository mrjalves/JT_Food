using System;
using System.Collections.Generic;
using System.Linq;
using JT_Food.Shared.Domain.Entities;
using JT_Food.Shared.Domain.Enums;

namespace JT_Food.Shared.BusinessLogic
{
    public class PromocaoLogic
    {
        public double CalcularPrecoEfetivo(ProdutoCardapio produto, List<RegraPromocao> regras, DateTime? referencia = null)
        {
            if (produto == null) return 0.0;
            var agora = referencia ?? DateTime.Now;

            // 1. Verificar regras ativas para este produto
            var regraProduto = regras
                .Where(r => r.Ativa && r.ProdutoId == produto.Id && r.IsValidaAgora(agora))
                .OrderByDescending(r => r.Tipo == TipoRegraPromocao.HappyHour ? 2 : 1)
                .FirstOrDefault();

            if (regraProduto != null)
            {
                if (regraProduto.PrecoPromocionalFixo.HasValue && regraProduto.PrecoPromocionalFixo.Value > 0)
                {
                    return regraProduto.PrecoPromocionalFixo.Value;
                }

                if (regraProduto.DescontoPercentual.HasValue && regraProduto.DescontoPercentual.Value > 0)
                {
                    double desconto = produto.PrecoBase * (regraProduto.DescontoPercentual.Value / 100.0);
                    return Math.Round(Math.Max(0, produto.PrecoBase - desconto), 2);
                }
            }

            // 2. Preço promocional direto no produto
            if (produto.EmPromocao && produto.PrecoPromocional.HasValue && produto.PrecoPromocional.Value > 0)
            {
                return produto.PrecoPromocional.Value;
            }

            // 3. Preço base padrão
            return produto.PrecoBase;
        }

        public (bool TemDesconto, double DescontoTotal) CalcularDescontoCombo(
            List<ItemPedido> itens,
            List<RegraPromocao> regras,
            DateTime? referencia = null)
        {
            var agora = referencia ?? DateTime.Now;
            var regrasCombo = regras
                .Where(r => r.Ativa && r.Tipo == TipoRegraPromocao.Combo && r.ProdutosComboIds.Any() && r.IsValidaAgora(agora))
                .ToList();

            double descontoTotal = 0;
            bool encontrouCombo = false;

            foreach (var combo in regrasCombo)
            {
                bool comboCompleto = combo.ProdutosComboIds.All(idProd => itens.Any(i => i.ProdutoId == idProd));
                if (comboCompleto)
                {
                    encontrouCombo = true;
                    if (combo.DescontoPercentual.HasValue)
                    {
                        var subtotalItensCombo = itens
                            .Where(i => combo.ProdutosComboIds.Contains(i.ProdutoId))
                            .Sum(i => i.TotalItem);
                        descontoTotal += Math.Round(subtotalItensCombo * (combo.DescontoPercentual.Value / 100.0), 2);
                    }
                    else if (combo.PrecoPromocionalFixo.HasValue)
                    {
                        var subtotalItensCombo = itens
                            .Where(i => combo.ProdutosComboIds.Contains(i.ProdutoId))
                            .Sum(i => i.TotalItem);
                        double diferenca = subtotalItensCombo - combo.PrecoPromocionalFixo.Value;
                        if (diferenca > 0) descontoTotal += diferenca;
                    }
                }
            }

            return (encontrouCombo, descontoTotal);
        }
    }
}
