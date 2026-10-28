using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JT_Classes.Restaurante;
using JT_Food.Shared.Domain.Entities;
using JT_Food.Shared.Domain.Enums;
using JT_Food.Shared.Services.Implementations;
using JT_Food.Shared.Services.Interfaces;
using JT_Servicos.Restaurante;
using ComandaDb = JT_Classes.Restaurante.Comanda;

namespace JT_Food.Shared.BusinessLogic
{
    public class ComercialLogic
    {
        private readonly CompraRestauranteService _compraService;
        private readonly ItemCompraService _itemCompraService;
        private readonly LoteInsumoService _loteInsumoService;
        private readonly InsumoService _insumoService;
        private readonly ComandaService _comandaService;
        private readonly ItemComandaService _itemComandaService;
        private readonly MesaService _mesaService;
        private readonly IImpressaoRedeService _impressaoService;
        private readonly PromocaoLogic _promocaoLogic;
        private readonly ICardapioService _cardapioService;
        private readonly IMesaLayoutService _mesaLayoutService;
        private readonly CaixaLogic _caixaLogic;
        private readonly IComandaService _comandaOperacionalService;

        public ComercialLogic(
            CompraRestauranteService compraService,
            ItemCompraService itemCompraService,
            LoteInsumoService loteInsumoService,
            InsumoService insumoService,
            ComandaService comandaService,
            ItemComandaService itemComandaService,
            MesaService mesaService,
            IImpressaoRedeService impressaoService,
            PromocaoLogic promocaoLogic,
            ICardapioService cardapioService,
            IMesaLayoutService mesaLayoutService,
            CaixaLogic caixaLogic,
            IComandaService comandaOperacionalService)
        {
            _compraService = compraService;
            _itemCompraService = itemCompraService;
            _loteInsumoService = loteInsumoService;
            _insumoService = insumoService;
            _comandaService = comandaService;
            _itemComandaService = itemComandaService;
            _mesaService = mesaService;
            _impressaoService = impressaoService;
            _promocaoLogic = promocaoLogic;
            _cardapioService = cardapioService;
            _mesaLayoutService = mesaLayoutService;
            _caixaLogic = caixaLogic;
            _comandaOperacionalService = comandaOperacionalService;
        }

        /// <summary>
        /// Lança um novo pedido tanto no Modo 1 (direto na conta global da mesa) quanto no Modo 2 (comanda individual da mesa).
        /// Valida preços vigentes/promoções, grava itens no banco, sincroniza com o KDS e despacha para impressão de rede.
        /// </summary>
        public async Task<(bool Sucesso, string Mensagem, Pedido? PedidoCriado)> LancarPedidoAsync(
            string? mesaIdentificacao,
            int? comandaId,
            List<ItemPedido> itens,
            string garcomId = "1",
            string nomeGarcom = "Garçom",
            string? observacaoGeral = null)
        {
            if (itens == null || !itens.Any())
                return (false, "O pedido deve conter ao menos um item.", null);

            var (caixaAberto, _, _) = await _caixaLogic.ObterCaixaAbertoAsync();
            if (!caixaAberto)
            {
                return (false, "Não é possível lançar pedidos com o caixa fechado. Por favor, realize a abertura do caixa no menu Caixa & PDV antes de registrar novos pedidos.", null);
            }

            // 1. Resolver comanda de destino garantindo unicidade
            ComandaDb? comandaDestino = null;

            if (comandaId.HasValue && comandaId.Value > 0)
            {
                comandaDestino = await _comandaOperacionalService.GetByIdAsync(comandaId.Value);
            }
            
            if (comandaDestino == null && !string.IsNullOrWhiteSpace(mesaIdentificacao))
            {
                // Busca comanda aberta existente da mesa ou obtém/cria via serviço operacional com lock anti-duplicação
                var comandasDaMesa = await _comandaOperacionalService.ObterComandasDaMesaAsync(mesaIdentificacao);
                comandaDestino = comandasDaMesa.FirstOrDefault();

                if (comandaDestino == null)
                {
                    comandaDestino = await _comandaOperacionalService.AbrirComandaFlexivelAsync(
                        numeroCartao: null,
                        nomeCliente: null,
                        mesaId: mesaIdentificacao
                    );
                }
            }

            if (comandaDestino == null)
                return (false, "Nenhuma comanda ou mesa especificada para lançar o pedido.", null);

            // Garante que a mesa vinculada ao pedido ou à comanda passe para status Ocupada
            string? mesaVinculada = !string.IsNullOrWhiteSpace(mesaIdentificacao) ? mesaIdentificacao : comandaDestino.MesaId;
            if (!string.IsNullOrWhiteSpace(mesaVinculada))
            {
                var todasMesas = await _mesaService.GetAll() ?? new List<JT_Classes.Restaurante.Mesa>();
                var mesa = todasMesas.FirstOrDefault(m =>
                    !string.IsNullOrWhiteSpace(m.Identificacao) &&
                    ComandaOperacionalService.MesmaMesa(m.Identificacao, mesaVinculada));

                if (mesa != null && mesa.Status != StatusMesaConsts.Ocupada)
                {
                    mesa.Status = StatusMesaConsts.Ocupada;
                    await _mesaService.SaveAsync(mesa);
                }
            }

            // 2. Montar objeto de Pedido e validar promoções/regras
            var regras = await _cardapioService.ObterRegrasPromocaoAsync(apenasAtivas: true);
            var (temCombo, descontoCombo) = _promocaoLogic.CalcularDescontoCombo(itens, regras);

            var pedido = new Pedido
            {
                ComandaId = comandaDestino.Id,
                MesaId = mesaIdentificacao ?? comandaDestino.MesaId,
                GarcomId = garcomId,
                NomeGarcom = nomeGarcom,
                DataHora = DateTime.UtcNow,
                ObservacaoGeral = observacaoGeral,
                Itens = itens,
                ValorTotal = itens.Sum(i => i.TotalItem) - descontoCombo
            };

            // 3. Persistir itens na tabela FoodItensComanda para sincronia com o KDS
            foreach (var item in itens)
            {
                var mItem = new MItemComanda
                {
                    ComandaId = comandaDestino.Id.ToString(),
                    ProdutoId = item.ProdutoId.ToString(),
                    NomeProduto = item.NomeProduto,
                    Quantidade = item.Quantidade,
                    PrecoUnitario = item.PrecoUnitario,
                    TotalItem = item.TotalItem,
                    Status = "Confirmado", // Entra no KDS como Confirmado/Pendente
                    Observacao = item.Observacao
                };

                await _itemComandaService.SaveAsync(mItem);
                item.Id = mItem.Id;
            }

            // 4. Atualizar o total acumulado na comanda de forma consistente
            if (descontoCombo > 0)
            {
                comandaDestino.Desconto = Math.Max(comandaDestino.Desconto, descontoCombo);
                await _comandaService.SaveAsync(comandaDestino);
            }
            await _comandaOperacionalService.RecalcularTotalComandaAsync(comandaDestino.Id);

            // 5. Atualizar estado no Mapa do Salão em tempo real
            if (!string.IsNullOrEmpty(pedido.MesaId))
            {
                var todasComandasMesa = await _comandaOperacionalService.ObterComandasDaMesaAsync(pedido.MesaId);
                int totalComandas = todasComandasMesa.Count > 0 ? todasComandasMesa.Count : 1;
                double subtotalMesa = todasComandasMesa.Count > 0 ? todasComandasMesa.Sum(c => c.Total) : comandaDestino.Total;

                await _mesaLayoutService.AtualizarStatusOperacionalAsync(
                    pedido.MesaId,
                    StatusMesaConsts.Ocupada,
                    totalComandas,
                    subtotalMesa
                );
            }

            // 6. Roteamento automático para impressoras de rede por setor (Bar, Cozinha, Chapa)
            string avisoImpressao = string.Empty;
            try
            {
                var resultadosImp = await _impressaoService.RoteamentoEImpressaoPedidoAsync(pedido);
                var falhas = resultadosImp.Where(r => !r.Sucesso).ToList();
                if (falhas.Any())
                {
                    avisoImpressao = $" [Aviso Impressão: {string.Join("; ", falhas.Select(f => $"{f.Setor}: {f.Mensagem}"))}]";
                }
            }
            catch (Exception exImp)
            {
                avisoImpressao = $" [Aviso Impressão: {exImp.Message}]";
            }

            string msg = $"Pedido lançado com sucesso na Comanda #{comandaDestino.Id}!{avisoImpressao}";
            if (temCombo && descontoCombo > 0)
            {
                msg += $" (Desconto Combo aplicado: -R$ {descontoCombo:F2})";
            }

            return (true, msg, pedido);
        }

        public async Task<(bool Sucesso, string Mensagem)> FinalizarCompraAsync(MCompra compra, List<MItemCompra> itens)
        {
            if (itens == null || !itens.Any())
                return (false, "A compra deve ter pelo menos um item.");

            compra.Valor = itens.Sum(x => Math.Round((decimal)x.ValorTotal, 2));
            
            bool salvouCompra = await _compraService.SaveAsync(compra) > 0;
            if (!salvouCompra)
                return (false, "Erro ao salvar o cabeçalho da compra.");

            var todosLotesCache = (await _loteInsumoService.GetAll() ?? Enumerable.Empty<MLoteInsumo>()).ToList();

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
                        Status = "Ativo"
                    };

                    await _loteInsumoService.SaveAsync(novoLote);
                    todosLotesCache.Add(novoLote);
                    await AtualizarCustoMedioInsumoAsync(item.InsumoId, todosLotesCache);
                }
            }

            return (true, "Compra finalizada com sucesso. Estoque atualizado.");
        }

        private async Task AtualizarCustoMedioInsumoAsync(string? insumoId, List<MLoteInsumo>? lotesPreCarregados = null)
        {
            if (string.IsNullOrEmpty(insumoId)) return;

            if (int.TryParse(insumoId, out int id))
            {
                var insumo = await _insumoService.GetById(id);
                if (insumo != null)
                {
                    var todosLotes = lotesPreCarregados ?? (await _loteInsumoService.GetAll() ?? Enumerable.Empty<MLoteInsumo>()).ToList();
                    var lotesAtivos = todosLotes
                        .Where(x => x.InsumoId == insumoId &&
                                    x.QuantidadeDisponivel > 0 &&
                                    !string.Equals(x.Status, "Finalizado", StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    if (lotesAtivos.Any())
                    {
                        // Custo médio ponderado real do saldo em estoque: Soma(QuantidadeDisponivel * CustoUnitario) / Soma(QuantidadeDisponivel)
                        decimal valorEstoqueAtual = lotesAtivos.Sum(x => (decimal)x.QuantidadeDisponivel * (decimal)x.CustoUnitario);
                        decimal qtdTotalDisponivel = lotesAtivos.Sum(x => (decimal)x.QuantidadeDisponivel);

                        if (qtdTotalDisponivel > 0m)
                        {
                            insumo.CustoMedio = (double)Math.Round(valorEstoqueAtual / qtdTotalDisponivel, 4);
                            await _insumoService.SaveAsync(insumo);
                        }
                    }
                }
            }
        }
    }
}
