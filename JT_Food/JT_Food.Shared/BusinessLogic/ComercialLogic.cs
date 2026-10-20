using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JT_Classes.Restaurante;
using JT_Food.Shared.Domain.Entities;
using JT_Food.Shared.Domain.Enums;
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
            IMesaLayoutService mesaLayoutService)
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

            // 1. Resolver comanda de destino
            ComandaDb? comandaDestino = null;

            if (comandaId.HasValue && comandaId.Value > 0)
            {
                comandaDestino = await _comandaService.GetById(comandaId.Value);
            }
            else if (!string.IsNullOrWhiteSpace(mesaIdentificacao))
            {
                // Modo 1: Pedido direto na mesa - busca comanda aberta da mesa ou abre uma nova
                var comandasDaMesa = await _comandaService.Find($"MesaId = '{mesaIdentificacao}' AND Status = 'Aberta'");
                comandaDestino = comandasDaMesa?.FirstOrDefault();

                if (comandaDestino == null)
                {
                    // Cria uma comanda global para a mesa
                    comandaDestino = new ComandaDb
                    {
                        UserId = "1",
                        MesaId = mesaIdentificacao,
                        NomeCliente = $"Conta Geral {mesaIdentificacao}",
                        DataAbertura = DateTime.UtcNow,
                        Status = "Aberta",
                        GarcomId = garcomId
                    };
                    await _comandaService.SaveAsync(comandaDestino);

                    // Atualiza mesa como Ocupada
                    var mesas = await _mesaService.Find($"Identificacao = '{mesaIdentificacao}'");
                    var mesa = mesas?.FirstOrDefault();
                    if (mesa != null)
                    {
                        mesa.Status = StatusMesaConsts.Ocupada;
                        await _mesaService.SaveAsync(mesa);
                    }
                }
            }

            if (comandaDestino == null)
                return (false, "Nenhuma comanda ou mesa especificada para lançar o pedido.", null);

            // Garante que a mesa vinculada ao pedido ou à comanda passe para status Ocupada
            string? mesaVinculada = !string.IsNullOrWhiteSpace(mesaIdentificacao) ? mesaIdentificacao : comandaDestino.MesaId;
            if (!string.IsNullOrWhiteSpace(mesaVinculada))
            {
                var mesas = await _mesaService.Find($"Identificacao = '{mesaVinculada}'");
                var mesa = mesas?.FirstOrDefault();
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

            // 4. Atualizar o total acumulado na comanda
            comandaDestino.Total += pedido.ValorTotal;
            if (descontoCombo > 0)
            {
                comandaDestino.Desconto += descontoCombo;
            }
            await _comandaService.SaveAsync(comandaDestino);

            // 5. Atualizar estado no Mapa do Salão em tempo real
            if (!string.IsNullOrEmpty(pedido.MesaId))
            {
                var todasComandasMesa = await _comandaService.Find($"MesaId = '{pedido.MesaId}' AND Status = 'Aberta'");
                int totalComandas = todasComandasMesa?.Count ?? 1;
                double subtotalMesa = todasComandasMesa?.Sum(c => c.Total) ?? comandaDestino.Total;

                await _mesaLayoutService.AtualizarStatusOperacionalAsync(
                    pedido.MesaId,
                    StatusMesaConsts.Ocupada,
                    totalComandas,
                    subtotalMesa
                );
            }

            // 6. Roteamento automático para impressoras de rede por setor (Bar, Cozinha, Chapa)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _impressaoService.RoteamentoEImpressaoPedidoAsync(pedido);
                }
                catch
                {
                    // Log silencioso de background
                }
            });

            string msg = $"Pedido lançado com sucesso na Comanda #{comandaDestino.Id}!";
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
