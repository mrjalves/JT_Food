using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JT_Food.Shared.Domain.Entities;
using JT_Food.Shared.Domain.Enums;
using JT_Food.Shared.Services.Interfaces;
using JT_Servicos.Restaurante;

namespace JT_Food.Shared.Services.Implementations
{
    public class MesaLayoutService : IMesaLayoutService
    {
        private readonly MesaService _mesaService;
        private readonly List<AmbienteSalao> _ambientes = new();
        private readonly List<ElementoSalao> _elementos = new();

        public MesaLayoutService(MesaService mesaService)
        {
            _mesaService = mesaService;
            InicializarLayoutPadrao();
        }

        private void InicializarLayoutPadrao()
        {
            if (_ambientes.Any()) return;

            _ambientes.AddRange(new[]
            {
                new AmbienteSalao { Id = 1, Nome = "Salão Principal", Ordem = 1, LarguraCanvas = 1000, AlturaCanvas = 650 },
                new AmbienteSalao { Id = 2, Nome = "Varanda & Esplanada", Ordem = 2, LarguraCanvas = 1000, AlturaCanvas = 600 },
                new AmbienteSalao { Id = 3, Nome = "Mezanino / Piso Superior", Ordem = 3, LarguraCanvas = 900, AlturaCanvas = 500 }
            });

            // Elementos do Salão Principal
            _elementos.AddRange(new[]
            {
                // Palco Show / Couvert
                new ElementoSalao
                {
                    Id = 1,
                    AmbienteId = 1,
                    Identificacao = "Palco Acústico (Couvert)",
                    Formato = FormatoElementoSalao.PalcoCouvert,
                    PosX = 4, PosY = 5, Largura = 22, Altura = 14,
                    Capacidade = 0, Status = StatusMesaConsts.Livre
                },
                // Caixa
                new ElementoSalao
                {
                    Id = 2,
                    AmbienteId = 1,
                    Identificacao = "Caixa / Recepção",
                    Formato = FormatoElementoSalao.Caixa,
                    PosX = 75, PosY = 5, Largura = 20, Altura = 12,
                    Capacidade = 0, Status = StatusMesaConsts.Livre
                },
                // Balcão do Bar
                new ElementoSalao
                {
                    Id = 3,
                    AmbienteId = 1,
                    Identificacao = "Balcão Bar & Chopp",
                    Formato = FormatoElementoSalao.BalcaoBar,
                    PosX = 75, PosY = 25, Largura = 20, Altura = 45,
                    Capacidade = 8, Status = StatusMesaConsts.Livre
                },
                // Divisória decorativa
                new ElementoSalao
                {
                    Id = 4,
                    AmbienteId = 1,
                    Identificacao = "Divisória de Vidro",
                    Formato = FormatoElementoSalao.Divisoria,
                    PosX = 32, PosY = 5, Largura = 1, Altura = 25,
                    Capacidade = 0, Status = StatusMesaConsts.Livre
                },
                // Fileira 1 de Mesas
                new ElementoSalao
                {
                    Id = 10,
                    AmbienteId = 1,
                    Identificacao = "Mesa 01",
                    Formato = FormatoElementoSalao.MesaQuadrada,
                    PosX = 8, PosY = 28, Largura = 11, Altura = 14,
                    Capacidade = 4, Status = StatusMesaConsts.Livre
                },
                new ElementoSalao
                {
                    Id = 11,
                    AmbienteId = 1,
                    Identificacao = "Mesa 02",
                    Formato = FormatoElementoSalao.MesaQuadrada,
                    PosX = 24, PosY = 28, Largura = 11, Altura = 14,
                    Capacidade = 4, Status = StatusMesaConsts.Livre
                },
                new ElementoSalao
                {
                    Id = 12,
                    AmbienteId = 1,
                    Identificacao = "Mesa 03",
                    Formato = FormatoElementoSalao.MesaRedonda,
                    PosX = 42, PosY = 26, Largura = 13, Altura = 17,
                    Capacidade = 6, Status = StatusMesaConsts.Livre
                },
                // Fileira 2 de Mesas
                new ElementoSalao
                {
                    Id = 13,
                    AmbienteId = 1,
                    Identificacao = "Mesa 04",
                    Formato = FormatoElementoSalao.MesaQuadrada,
                    PosX = 8, PosY = 50, Largura = 11, Altura = 14,
                    Capacidade = 4, Status = StatusMesaConsts.Livre
                },
                new ElementoSalao
                {
                    Id = 14,
                    AmbienteId = 1,
                    Identificacao = "Mesa 05",
                    Formato = FormatoElementoSalao.MesaRetangular,
                    PosX = 24, PosY = 50, Largura = 15, Altura = 14,
                    Capacidade = 8, Status = StatusMesaConsts.Livre
                },
                new ElementoSalao
                {
                    Id = 15,
                    AmbienteId = 1,
                    Identificacao = "Mesa 06",
                    Formato = FormatoElementoSalao.MesaRedonda,
                    PosX = 44, PosY = 50, Largura = 13, Altura = 17,
                    Capacidade = 6, Status = StatusMesaConsts.Livre
                },
                // Fileira 3 de Mesas
                new ElementoSalao
                {
                    Id = 16,
                    AmbienteId = 1,
                    Identificacao = "Mesa 07",
                    Formato = FormatoElementoSalao.MesaQuadrada,
                    PosX = 8, PosY = 74, Largura = 11, Altura = 14,
                    Capacidade = 4, Status = StatusMesaConsts.Livre
                },
                new ElementoSalao
                {
                    Id = 17,
                    AmbienteId = 1,
                    Identificacao = "Mesa 08",
                    Formato = FormatoElementoSalao.MesaQuadrada,
                    PosX = 24, PosY = 74, Largura = 11, Altura = 14,
                    Capacidade = 4, Status = StatusMesaConsts.Livre
                },
                new ElementoSalao
                {
                    Id = 18,
                    AmbienteId = 1,
                    Identificacao = "Mesa 09",
                    Formato = FormatoElementoSalao.MesaQuadrada,
                    PosX = 42, PosY = 74, Largura = 11, Altura = 14,
                    Capacidade = 4, Status = StatusMesaConsts.Livre
                }
            });

            // Elementos da Varanda
            _elementos.AddRange(new[]
            {
                new ElementoSalao
                {
                    Id = 30,
                    AmbienteId = 2,
                    Identificacao = "Deck 01",
                    Formato = FormatoElementoSalao.MesaRedonda,
                    PosX = 15, PosY = 20, Largura = 14, Altura = 18,
                    Capacidade = 4, Status = StatusMesaConsts.Livre
                },
                new ElementoSalao
                {
                    Id = 31,
                    AmbienteId = 2,
                    Identificacao = "Deck 02",
                    Formato = FormatoElementoSalao.MesaRedonda,
                    PosX = 45, PosY = 20, Largura = 14, Altura = 18,
                    Capacidade = 4, Status = StatusMesaConsts.Livre
                },
                new ElementoSalao
                {
                    Id = 32,
                    AmbienteId = 2,
                    Identificacao = "Deck 03",
                    Formato = FormatoElementoSalao.MesaRetangular,
                    PosX = 25, PosY = 55, Largura = 22, Altura = 18,
                    Capacidade = 10, Status = StatusMesaConsts.Livre
                }
            });
        }

        public Task<List<AmbienteSalao>> ObterAmbientesAsync()
        {
            return Task.FromResult(_ambientes.OrderBy(a => a.Ordem).ToList());
        }

        public Task<AmbienteSalao> SalvarAmbienteAsync(AmbienteSalao ambiente)
        {
            if (ambiente.Id == 0)
            {
                ambiente.Id = _ambientes.Any() ? _ambientes.Max(a => a.Id) + 1 : 1;
                _ambientes.Add(ambiente);
            }
            else
            {
                var idx = _ambientes.FindIndex(a => a.Id == ambiente.Id);
                if (idx >= 0) _ambientes[idx] = ambiente;
                else _ambientes.Add(ambiente);
            }
            return Task.FromResult(ambiente);
        }

        public Task<bool> ExcluirAmbienteAsync(int id)
        {
            var removido = _ambientes.RemoveAll(a => a.Id == id) > 0;
            _elementos.RemoveAll(e => e.AmbienteId == id);
            return Task.FromResult(removido);
        }

        public Task<List<ElementoSalao>> ObterElementosPorAmbienteAsync(int ambienteId)
        {
            var elementos = _elementos.Where(e => e.AmbienteId == ambienteId).ToList();
            return Task.FromResult(elementos);
        }

        public Task<bool> SalvarElementoAsync(ElementoSalao elemento)
        {
            if (elemento.Id == 0)
            {
                elemento.Id = _elementos.Any() ? _elementos.Max(e => e.Id) + 1 : 1;
                _elementos.Add(elemento);
            }
            else
            {
                var idx = _elementos.FindIndex(e => e.Id == elemento.Id);
                if (idx >= 0) _elementos[idx] = elemento;
                else _elementos.Add(elemento);
            }
            return Task.FromResult(true);
        }

        public Task<bool> SalvarElementosEmLoteAsync(List<ElementoSalao> elementos)
        {
            foreach (var elemento in elementos)
            {
                var idx = _elementos.FindIndex(e => e.Id == elemento.Id);
                if (idx >= 0) _elementos[idx] = elemento;
                else _elementos.Add(elemento);
            }
            return Task.FromResult(true);
        }

        public Task<bool> ExcluirElementoAsync(int id)
        {
            var removido = _elementos.RemoveAll(e => e.Id == id) > 0;
            return Task.FromResult(removido);
        }

        public Task<ElementoSalao?> ObterElementoPorIdentificacaoAsync(string identificacao)
        {
            var elem = _elementos.FirstOrDefault(e => string.Equals(e.Identificacao, identificacao, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(elem);
        }

        public Task<bool> AtualizarStatusOperacionalAsync(string identificacaoMesa, string novoStatus, int totalComandas, double subtotal)
        {
            var elem = _elementos.FirstOrDefault(e => string.Equals(e.Identificacao, identificacaoMesa, StringComparison.OrdinalIgnoreCase));
            if (elem != null)
            {
                elem.Status = novoStatus;
                elem.TotalComandasAtivas = totalComandas;
                elem.SubtotalAcumulado = subtotal;
                if (novoStatus == StatusMesaConsts.Ocupada && totalComandas > 0)
                {
                    elem.UltimoPedidoEm = DateTime.UtcNow;
                }
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }
    }
}
