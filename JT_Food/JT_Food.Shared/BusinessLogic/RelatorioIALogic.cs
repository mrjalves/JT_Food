using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using JT_Classes.Restaurante;
using JT_Food.Shared.Services.Interfaces;
using JT_Servicos.Restaurante;

namespace JT_Food.Shared.BusinessLogic
{
    public class RelatorioIALogic
    {
        private readonly IIAAnalyticsService _iaService;
        private readonly ComandaService _comandaService;
        private readonly ItemComandaService _itemComandaService;
        private readonly CaixaService _caixaService;
        private readonly InsumoService _insumoService;

        public RelatorioIALogic(
            IIAAnalyticsService iaService,
            ComandaService comandaService,
            ItemComandaService itemComandaService,
            CaixaService caixaService,
            InsumoService insumoService)
        {
            _iaService = iaService;
            _comandaService = comandaService;
            _itemComandaService = itemComandaService;
            _caixaService = caixaService;
            _insumoService = insumoService;
        }

        public async Task<string> GerarRelatorioInteligenteAsync()
        {
            var comandas = await _comandaService.GetAll() ?? new List<Comanda>();
            var itens = await _itemComandaService.GetAll() ?? new List<MItemComanda>();
            var caixas = await _caixaService.GetAll() ?? new List<MCaixa>();
            var insumos = await _insumoService.GetAll() ?? new List<MInsumo>();

            double faturamentoTotal = comandas.Where(c => c.Status == "Fechada").Sum(c => c.Total);
            int totalComandasFechadas = comandas.Count(c => c.Status == "Fechada");
            double ticketMedio = totalComandasFechadas > 0 ? faturamentoTotal / totalComandasFechadas : 0;
            int comandasAbertas = comandas.Count(c => c.Status == "Aberta" || c.Status == "AguardandoPagamento");

            var produtosMaisVendidos = itens
                .GroupBy(i => i.NomeProduto ?? "Produto")
                .Select(g => new { Produto = g.Key, Quantidade = g.Sum(x => x.Quantidade), Total = g.Sum(x => x.TotalItem) })
                .OrderByDescending(x => x.Quantidade)
                .Take(5)
                .ToList();

            var insumosCriticos = insumos
                .Where(ins => ins.EstoqueMinimo > 0)
                .Select(ins => new { ins.Nome, ins.CustoMedio })
                .Take(5)
                .ToList();

            var payload = new
            {
                DataAnalise = DateTime.UtcNow,
                FaturamentoTotal = faturamentoTotal,
                TotalComandasFechadas = totalComandasFechadas,
                TicketMedio = ticketMedio,
                ComandasAbertasAtuais = comandasAbertas,
                TopProdutosVendidos = produtosMaisVendidos,
                InsumosMonitorados = insumosCriticos
            };

            string contextoJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
            return await _iaService.GerarDiagnosticoGeralAsync(contextoJson);
        }
    }
}
