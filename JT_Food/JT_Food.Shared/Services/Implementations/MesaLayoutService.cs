using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JT_Classes.Restaurante;
using JT_Food.Shared.Domain.Entities;
using JT_Food.Shared.Domain.Enums;
using JT_Food.Shared.Services.Interfaces;
using JT_Servicos.Restaurante;

namespace JT_Food.Shared.Services.Implementations
{
    public class MesaLayoutService : IMesaLayoutService
    {
        private readonly MesaService _mesaService;
        private readonly ComandaService _comandaService;
        private static readonly List<AmbienteSalao> _ambientes = new();
        private static readonly List<ElementoSalao> _elementos = new();
        private static readonly object _syncLock = new();

        public MesaLayoutService(MesaService mesaService, ComandaService comandaService)
        {
            _mesaService = mesaService;
            _comandaService = comandaService;
            InicializarLayoutPadrao();
        }

        private void InicializarLayoutPadrao()
        {
            lock (_syncLock)
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
                    new ElementoSalao
                    {
                        Id = 1,
                        AmbienteId = 1,
                        Identificacao = "Palco Acústico (Couvert)",
                        Formato = FormatoElementoSalao.PalcoCouvert,
                        PosX = 4, PosY = 5, Largura = 22, Altura = 14,
                        Capacidade = 0, Status = StatusMesaConsts.Livre
                    },
                    new ElementoSalao
                    {
                        Id = 2,
                        AmbienteId = 1,
                        Identificacao = "Caixa / Recepção",
                        Formato = FormatoElementoSalao.Caixa,
                        PosX = 75, PosY = 5, Largura = 20, Altura = 12,
                        Capacidade = 0, Status = StatusMesaConsts.Livre
                    },
                    new ElementoSalao
                    {
                        Id = 3,
                        AmbienteId = 1,
                        Identificacao = "Balcão Bar & Chopp",
                        Formato = FormatoElementoSalao.BalcaoBar,
                        PosX = 75, PosY = 25, Largura = 20, Altura = 45,
                        Capacidade = 8, Status = StatusMesaConsts.Livre
                    },
                    new ElementoSalao
                    {
                        Id = 4,
                        AmbienteId = 1,
                        Identificacao = "Divisória de Vidro",
                        Formato = FormatoElementoSalao.Divisoria,
                        PosX = 32, PosY = 5, Largura = 1, Altura = 25,
                        Capacidade = 0, Status = StatusMesaConsts.Livre
                    },
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
        }

        public string FormatarIdentificacaoCanonica(string? identificacao)
        {
            if (string.IsNullOrWhiteSpace(identificacao)) return "Mesa 01";
            string limpo = identificacao.Trim();
            if (int.TryParse(limpo, out int num))
            {
                return $"Mesa {num:00}";
            }
            if (limpo.StartsWith("Mesa ", StringComparison.OrdinalIgnoreCase))
            {
                string sufixo = limpo.Substring(5).Trim();
                if (int.TryParse(sufixo, out int numMesa))
                {
                    return $"Mesa {numMesa:00}";
                }
            }
            return limpo;
        }

        private async Task<List<JT_Classes.Restaurante.Mesa>> SincronizarMesasEComandasAsync()
        {
            List<JT_Classes.Restaurante.Mesa> mesasDb = new();
            List<JT_Classes.Restaurante.Comanda> comandasDb = new();

            try { mesasDb = await _mesaService.GetAll() ?? new(); } catch { }
            try { comandasDb = await _comandaService.GetAll() ?? new(); } catch { }

            var comandasAbertas = comandasDb
                .Where(c => !string.Equals(c.Status, "Fechada", StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(c.Status, "Cancelada", StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Deduplica as mesas do banco por chave normalizada (ex: "01" e "Mesa 01" são a mesma mesa)
            var gruposMesas = mesasDb
                .Where(m => !string.IsNullOrWhiteSpace(m.Identificacao))
                .GroupBy(m => ComandaOperacionalService.NormalizarMesaId(m.Identificacao), StringComparer.OrdinalIgnoreCase)
                .ToList();

            var mesasUnicas = new List<JT_Classes.Restaurante.Mesa>();

            foreach (var grupo in gruposMesas)
            {
                var ordenadas = grupo.OrderBy(m => m.Id).ToList();
                var principal = ordenadas.First();

                // Remove registros duplicados da mesma mesa no banco de dados
                foreach (var duplicada in ordenadas.Skip(1))
                {
                    if (duplicada.Id > 0 && duplicada.Id != principal.Id)
                    {
                        try { await _mesaService.Delete(duplicada.Id); } catch { }
                    }
                }

                // Reconcilia status real da mesa com as comandas abertas no salão
                var cmdsDaMesa = comandasAbertas
                    .Where(c => !string.IsNullOrWhiteSpace(c.MesaId) &&
                                ComandaOperacionalService.MesmaMesa(c.MesaId, principal.Identificacao))
                    .ToList();

                string statusCorreto;
                if (cmdsDaMesa.Any())
                {
                    bool aguardando = cmdsDaMesa.Any(c => string.Equals(c.Status, "AguardandoPagamento", StringComparison.OrdinalIgnoreCase));
                    statusCorreto = aguardando ? StatusMesaConsts.AguardandoPagamento : StatusMesaConsts.Ocupada;
                }
                else if (string.Equals(principal.Status, StatusMesaConsts.Reservada, StringComparison.OrdinalIgnoreCase))
                {
                    statusCorreto = StatusMesaConsts.Reservada;
                }
                else
                {
                    statusCorreto = StatusMesaConsts.Livre;
                }

                string nomeCanonico = FormatarIdentificacaoCanonica(principal.Identificacao);
                if (!string.Equals(principal.Status, statusCorreto, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(principal.Identificacao, nomeCanonico, StringComparison.Ordinal))
                {
                    principal.Status = statusCorreto;
                    principal.Identificacao = nomeCanonico;
                    if (principal.Id > 0)
                    {
                        try { await _mesaService.SaveAsync(principal); } catch { }
                    }
                }

                mesasUnicas.Add(principal);
            }

            lock (_syncLock)
            {
                // 1. Garantir que toda mesa cadastrada no banco/modal exista como elemento visual no Mapa Operacional
                foreach (var m in mesasUnicas)
                {
                    var existente = _elementos.FirstOrDefault(e =>
                        e.Capacidade > 0 &&
                        ComandaOperacionalService.MesmaMesa(e.Identificacao, m.Identificacao));

                    if (existente == null)
                    {
                        CriarElementoPosicionadoInterno(m, ambientePadraoId: 1);
                    }
                    else
                    {
                        if (m.Capacidade > 0) existente.Capacidade = m.Capacidade;
                    }
                }

                // 2. Atualizar status real de cada elemento com base nas comandas realmente abertas
                foreach (var elem in _elementos.Where(e => e.Capacidade > 0))
                {
                    var cmdsDaMesa = comandasAbertas
                        .Where(c => !string.IsNullOrWhiteSpace(c.MesaId) &&
                                    ComandaOperacionalService.MesmaMesa(c.MesaId, elem.Identificacao))
                        .ToList();

                    if (cmdsDaMesa.Any())
                    {
                        bool aguardando = cmdsDaMesa.Any(c => string.Equals(c.Status, "AguardandoPagamento", StringComparison.OrdinalIgnoreCase));
                        elem.Status = aguardando ? StatusMesaConsts.AguardandoPagamento : StatusMesaConsts.Ocupada;
                        elem.TotalComandasAtivas = cmdsDaMesa.Count;
                        elem.SubtotalAcumulado = cmdsDaMesa.Sum(c => c.Total);
                    }
                    else
                    {
                        var mesaCorrespondente = mesasUnicas.FirstOrDefault(m =>
                            ComandaOperacionalService.MesmaMesa(m.Identificacao, elem.Identificacao));

                        // Se a mesa estava Reservada manualmente, preserva Reservada; caso contrário, sem comanda aberta = Livre
                        if (mesaCorrespondente != null && string.Equals(mesaCorrespondente.Status, StatusMesaConsts.Reservada, StringComparison.OrdinalIgnoreCase))
                        {
                            elem.Status = StatusMesaConsts.Reservada;
                        }
                        else
                        {
                            elem.Status = StatusMesaConsts.Livre;
                        }
                        elem.TotalComandasAtivas = 0;
                        elem.SubtotalAcumulado = 0;
                    }

                    // Garantir que elementos do mapa com capacidade também constem na lista consolidada de mesas
                    if (!mesasUnicas.Any(m => ComandaOperacionalService.MesmaMesa(m.Identificacao, elem.Identificacao)))
                    {
                        mesasUnicas.Add(new JT_Classes.Restaurante.Mesa
                        {
                            Id = 0,
                            Identificacao = elem.Identificacao,
                            Capacidade = elem.Capacidade,
                            Status = elem.Status
                        });
                    }
                }
            }

            return mesasUnicas
                .OrderBy(m => m.Identificacao, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public async Task<List<JT_Classes.Restaurante.Mesa>> ObterMesasSincronizadasAsync()
        {
            return await SincronizarMesasEComandasAsync();
        }

        private ElementoSalao CriarElementoPosicionadoInterno(JT_Classes.Restaurante.Mesa mesa, int ambientePadraoId = 1)
        {
            string nomeCanonico = FormatarIdentificacaoCanonica(mesa.Identificacao);
            int cap = mesa.Capacidade > 0 ? mesa.Capacidade : 4;

            FormatoElementoSalao formato = cap >= 8
                ? FormatoElementoSalao.MesaRetangular
                : cap >= 6
                    ? FormatoElementoSalao.MesaRedonda
                    : FormatoElementoSalao.MesaQuadrada;

            double larg = formato == FormatoElementoSalao.MesaRetangular ? 15 : (formato == FormatoElementoSalao.MesaRedonda ? 13 : 11);
            double alt = formato == FormatoElementoSalao.MesaRedonda ? 17 : 14;

            var mesasNoAmbiente = _elementos.Where(e => e.AmbienteId == ambientePadraoId && e.Capacidade > 0 && e.Formato != FormatoElementoSalao.BalcaoBar).ToList();
            int idx = mesasNoAmbiente.Count;

            // Posiciona automaticamente numa grade livre do salão
            int col = idx % 4;
            int row = idx / 4;
            double posX = Math.Clamp(8 + (col * 16), 4, 84);
            double posY = Math.Clamp(28 + (row * 22), 18, 82);

            int novoId = _elementos.Any() ? _elementos.Max(e => e.Id) + 1 : 1;
            var novoElem = new ElementoSalao
            {
                Id = novoId,
                AmbienteId = ambientePadraoId,
                Identificacao = nomeCanonico,
                Formato = formato,
                PosX = posX,
                PosY = posY,
                Largura = larg,
                Altura = alt,
                Capacidade = cap,
                Status = string.IsNullOrWhiteSpace(mesa.Status) ? StatusMesaConsts.Livre : mesa.Status
            };

            _elementos.Add(novoElem);
            return novoElem;
        }

        public async Task<ElementoSalao> GarantirElementoParaMesaAsync(JT_Classes.Restaurante.Mesa mesa, int ambientePadraoId = 1)
        {
            await Task.CompletedTask;
            lock (_syncLock)
            {
                var existente = _elementos.FirstOrDefault(e =>
                    e.Capacidade > 0 &&
                    ComandaOperacionalService.MesmaMesa(e.Identificacao, mesa.Identificacao));

                if (existente != null)
                {
                    existente.Identificacao = FormatarIdentificacaoCanonica(mesa.Identificacao);
                    if (ambientePadraoId > 0) existente.AmbienteId = ambientePadraoId;
                    if (mesa.Capacidade > 0) existente.Capacidade = mesa.Capacidade;
                    if (!string.IsNullOrWhiteSpace(mesa.Status)) existente.Status = mesa.Status;
                    return existente;
                }

                return CriarElementoPosicionadoInterno(mesa, ambientePadraoId);
            }
        }

        public Task<bool> RemoverElementoPorIdentificacaoAsync(string identificacaoMesa)
        {
            if (string.IsNullOrWhiteSpace(identificacaoMesa)) return Task.FromResult(false);
            lock (_syncLock)
            {
                int removidos = _elementos.RemoveAll(e =>
                    e.Capacidade > 0 &&
                    ComandaOperacionalService.MesmaMesa(e.Identificacao, identificacaoMesa));
                return Task.FromResult(removidos > 0);
            }
        }

        public Task<List<AmbienteSalao>> ObterAmbientesAsync()
        {
            lock (_syncLock)
            {
                return Task.FromResult(_ambientes.OrderBy(a => a.Ordem).ToList());
            }
        }

        public Task<AmbienteSalao> SalvarAmbienteAsync(AmbienteSalao ambiente)
        {
            lock (_syncLock)
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
        }

        public Task<bool> ExcluirAmbienteAsync(int id)
        {
            lock (_syncLock)
            {
                var removido = _ambientes.RemoveAll(a => a.Id == id) > 0;
                _elementos.RemoveAll(e => e.AmbienteId == id);
                return Task.FromResult(removido);
            }
        }

        public async Task<List<ElementoSalao>> ObterElementosPorAmbienteAsync(int ambienteId)
        {
            await SincronizarMesasEComandasAsync();
            lock (_syncLock)
            {
                return _elementos.Where(e => e.AmbienteId == ambienteId).ToList();
            }
        }

        public Task<bool> SalvarElementoAsync(ElementoSalao elemento)
        {
            lock (_syncLock)
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
        }

        public Task<bool> SalvarElementosEmLoteAsync(List<ElementoSalao> elementos)
        {
            lock (_syncLock)
            {
                var ambientesAfetados = elementos.Select(e => e.AmbienteId).Distinct().ToList();
                foreach (var ambId in ambientesAfetados)
                {
                    var idsMantidos = elementos.Where(e => e.AmbienteId == ambId).Select(e => e.Id).ToHashSet();
                    _elementos.RemoveAll(e => e.AmbienteId == ambId && !idsMantidos.Contains(e.Id));
                }

                foreach (var elemento in elementos)
                {
                    var idx = _elementos.FindIndex(e => e.Id == elemento.Id);
                    if (idx >= 0) _elementos[idx] = elemento;
                    else _elementos.Add(elemento);
                }
                return Task.FromResult(true);
            }
        }

        public Task<bool> ExcluirElementoAsync(int id)
        {
            lock (_syncLock)
            {
                var removido = _elementos.RemoveAll(e => e.Id == id) > 0;
                return Task.FromResult(removido);
            }
        }

        public Task<ElementoSalao?> ObterElementoPorIdentificacaoAsync(string identificacao)
        {
            lock (_syncLock)
            {
                var elem = _elementos.FirstOrDefault(e => ComandaOperacionalService.MesmaMesa(e.Identificacao, identificacao));
                return Task.FromResult(elem);
            }
        }

        public Task<bool> AtualizarStatusOperacionalAsync(string identificacaoMesa, string novoStatus, int totalComandas, double subtotal)
        {
            if (string.IsNullOrWhiteSpace(identificacaoMesa)) return Task.FromResult(false);

            lock (_syncLock)
            {
                var elem = _elementos.FirstOrDefault(e =>
                    ComandaOperacionalService.MesmaMesa(e.Identificacao, identificacaoMesa)
                );

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
}
