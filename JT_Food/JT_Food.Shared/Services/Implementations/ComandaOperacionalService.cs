using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JT_Classes.Restaurante;
using JT_Food.Shared.Domain.Enums;
using JT_Food.Shared.Services.Interfaces;
using JT_Servicos.Restaurante;

namespace JT_Food.Shared.Services.Implementations
{
    public class ComandaOperacionalService : IComandaService
    {
        private readonly ComandaService _comandaService;
        private readonly MesaService _mesaService;
        private readonly ItemComandaService _itemComandaService;
        private readonly IMesaLayoutService _mesaLayoutService;
        private static readonly System.Threading.SemaphoreSlim _aberturaLock = new(1, 1);

        public event Action? OnDataChanged;

        public ComandaOperacionalService(
            ComandaService comandaService,
            MesaService mesaService,
            ItemComandaService itemComandaService,
            IMesaLayoutService mesaLayoutService)
        {
            _comandaService = comandaService;
            _mesaService = mesaService;
            _itemComandaService = itemComandaService;
            _mesaLayoutService = mesaLayoutService;
        }


        public async Task<List<Comanda>> GetAllAsync()
        {
            var lista = await _comandaService.GetAll();
            return lista ?? new List<Comanda>();
        }

        public async Task<List<Comanda>> FindAsync(string condicao)
        {
            var lista = await _comandaService.Find(condicao);
            return lista ?? new List<Comanda>();
        }

        public async Task<List<Comanda>> FindAsync(Dictionary<string, object> filtros)
        {
            var todas = await GetAllAsync();
            var query = todas.AsEnumerable();
            foreach (var kvp in filtros)
            {
                string val = kvp.Value?.ToString() ?? "";
                if (kvp.Key.Equals("Status", StringComparison.OrdinalIgnoreCase))
                    query = query.Where(c => string.Equals(c.Status, val, StringComparison.OrdinalIgnoreCase));
                else if (kvp.Key.Equals("MesaId", StringComparison.OrdinalIgnoreCase))
                {
                    string mNorm = val.Replace("Mesa ", "").Trim();
                    query = query.Where(c => !string.IsNullOrEmpty(c.MesaId) &&
                        (c.MesaId.Equals(val, StringComparison.OrdinalIgnoreCase) ||
                         c.MesaId.Replace("Mesa ", "").Trim().Equals(mNorm, StringComparison.OrdinalIgnoreCase)));
                }
            }
            return query.ToList();
        }

        public async Task<Comanda?> GetByIdAsync(int id)
        {
            return await _comandaService.GetById(id);
        }

        public async Task<bool> SaveAsync(Comanda entity)
        {
            bool ok = await _comandaService.SaveAsync(entity) > 0;
            if (ok) OnDataChanged?.Invoke();
            return ok;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            bool ok = await _comandaService.Delete(id);
            if (ok) OnDataChanged?.Invoke();
            return ok;
        }

        public async Task<List<Comanda>> BuscarComandasAsync(string? termoPesquisa, string? status = null)
        {
            var todas = await GetAllAsync();
            var query = todas.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(status) && status != "Todas")
            {
                query = query.Where(c => string.Equals(c.Status, status, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(termoPesquisa))
            {
                string termo = termoPesquisa.Trim().ToLowerInvariant();
                query = query.Where(c =>
                    c.Id.ToString().Contains(termo) ||
                    (c.NomeCliente != null && c.NomeCliente.ToLowerInvariant().Contains(termo)) ||
                    (c.MesaId != null && c.MesaId.ToLowerInvariant().Contains(termo))
                );
            }

            return query.OrderByDescending(c => c.DataAbertura).ToList();
        }

        public async Task<List<Comanda>> ObterComandasDaMesaAsync(string mesaId)
        {
            if (string.IsNullOrWhiteSpace(mesaId)) return new List<Comanda>();
            var todas = await GetAllAsync();
            string mNorm = mesaId.Replace("Mesa ", "").Trim();
            return todas
                .Where(c => c.Status != "Fechada" && c.Status != "Cancelada" &&
                            !string.IsNullOrWhiteSpace(c.MesaId) &&
                            (c.MesaId.Equals(mesaId, StringComparison.OrdinalIgnoreCase) ||
                             c.MesaId.Replace("Mesa ", "").Trim().Equals(mNorm, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(c => c.Id)
                .ToList();
        }

        public async Task<Comanda> AbrirComandaFlexivelAsync(
            string? numeroCartao,
            string? nomeCliente,
            string? mesaId,
            int quantidadePessoas = 1,
            bool ativarCouvert = false,
            double valorCouvert = 0)
        {
            await _aberturaLock.WaitAsync();
            try
            {
                var todas = await GetAllAsync();
                string? cartaoLimpo = !string.IsNullOrWhiteSpace(numeroCartao) ? numeroCartao.Trim() : null;
                string? clienteLimpo = !string.IsNullOrWhiteSpace(nomeCliente) ? nomeCliente.Trim() : null;
                string? mNorm = !string.IsNullOrWhiteSpace(mesaId) ? mesaId.Replace("Mesa ", "").Trim() : null;

                // 1. REGRA ANTI-DUPLICAÇÃO: Se já existe comanda aberta com este número de cartão, REUTILIZAR
                if (!string.IsNullOrWhiteSpace(cartaoLimpo))
                {
                    var abertaComCartao = todas.FirstOrDefault(c =>
                        c.Status != "Fechada" && c.Status != "Cancelada" &&
                        (c.NomeCliente != null && (
                            c.NomeCliente.Equals($"Cartão #{cartaoLimpo}", StringComparison.OrdinalIgnoreCase) ||
                            c.NomeCliente.Contains($"[Cartão {cartaoLimpo}]", StringComparison.OrdinalIgnoreCase) ||
                            c.NomeCliente.Equals(cartaoLimpo, StringComparison.OrdinalIgnoreCase)
                        ))
                    );

                    if (abertaComCartao != null)
                    {
                        // Cartão já aberto no salão! Atualiza mesa se fornecida e retorna a comanda existente (NUNCA DUPLICA!)
                        if (!string.IsNullOrWhiteSpace(mesaId) && abertaComCartao.MesaId != mesaId)
                        {
                            abertaComCartao.MesaId = mesaId;
                            await _comandaService.SaveAsync(abertaComCartao);
                        }
                        await GarantirMesaOcupadaAsync(mesaId);
                        return abertaComCartao;
                    }
                }

                // 2. REGRA ANTI-DUPLICAÇÃO: Se já existe comanda aberta para este cliente na mesma mesa
                if (!string.IsNullOrWhiteSpace(mesaId) && !string.IsNullOrWhiteSpace(clienteLimpo))
                {
                    var abertaMesaCliente = todas.FirstOrDefault(c =>
                        c.Status != "Fechada" && c.Status != "Cancelada" &&
                        !string.IsNullOrWhiteSpace(c.MesaId) &&
                        (c.MesaId.Equals(mesaId, StringComparison.OrdinalIgnoreCase) ||
                         (mNorm != null && c.MesaId.Replace("Mesa ", "").Trim().Equals(mNorm, StringComparison.OrdinalIgnoreCase))) &&
                        c.NomeCliente != null && (
                            c.NomeCliente.Equals(clienteLimpo, StringComparison.OrdinalIgnoreCase) ||
                            c.NomeCliente.EndsWith($" {clienteLimpo}", StringComparison.OrdinalIgnoreCase) ||
                            c.NomeCliente.EndsWith($"] {clienteLimpo}", StringComparison.OrdinalIgnoreCase)
                        )
                    );

                    if (abertaMesaCliente != null)
                    {
                        await GarantirMesaOcupadaAsync(mesaId);
                        return abertaMesaCliente;
                    }
                }
                else if (!string.IsNullOrWhiteSpace(mesaId) && string.IsNullOrWhiteSpace(clienteLimpo) && string.IsNullOrWhiteSpace(cartaoLimpo))
                {
                    // Se não tem cartão e não tem cliente específico, verifica se já existe uma Conta Geral para esta mesa
                    var contaGeralMesa = todas.FirstOrDefault(c =>
                        c.Status != "Fechada" && c.Status != "Cancelada" &&
                        !string.IsNullOrWhiteSpace(c.MesaId) &&
                        (c.MesaId.Equals(mesaId, StringComparison.OrdinalIgnoreCase) ||
                         (mNorm != null && c.MesaId.Replace("Mesa ", "").Trim().Equals(mNorm, StringComparison.OrdinalIgnoreCase))) &&
                        (c.NomeCliente == null || c.NomeCliente.StartsWith("Conta Geral")));

                    if (contaGeralMesa != null)
                    {
                        await GarantirMesaOcupadaAsync(mesaId);
                        return contaGeralMesa;
                    }
                }

                // Formatar nome / cartão
                string nomeFormatado = "";
                if (!string.IsNullOrWhiteSpace(cartaoLimpo) && !string.IsNullOrWhiteSpace(clienteLimpo))
                    nomeFormatado = $"[Cartão {cartaoLimpo}] {clienteLimpo}";
                else if (!string.IsNullOrWhiteSpace(cartaoLimpo))
                    nomeFormatado = $"Cartão #{cartaoLimpo}";
                else if (!string.IsNullOrWhiteSpace(clienteLimpo))
                    nomeFormatado = clienteLimpo;
                else
                    nomeFormatado = $"Conta Geral {mesaId}";

                double taxaCouvertCalculada = ativarCouvert ? (valorCouvert * Math.Max(1, quantidadePessoas)) : 0.0;

                var novaComanda = new Comanda
                {
                    UserId = "1",
                    MesaId = mesaId,
                    NomeCliente = nomeFormatado,
                    DataAbertura = DateTime.UtcNow,
                    Status = "Aberta",
                    TaxaServico = 0.0,
                    Desconto = 0.0,
                    Total = taxaCouvertCalculada
                };

                await _comandaService.SaveAsync(novaComanda);

                // Ocupa a mesa tanto na tabela FoodMesas quanto no Layout visual do Salão imediatamente
                if (!string.IsNullOrWhiteSpace(mesaId))
                {
                    await GarantirMesaOcupadaAsync(mesaId, taxaCouvertCalculada);
                }

                OnDataChanged?.Invoke();
                return novaComanda;
            }
            finally
            {
                _aberturaLock.Release();
            }
        }

        private async Task GarantirMesaOcupadaAsync(string? mesaId, double valorInicial = 0)
        {
            if (string.IsNullOrWhiteSpace(mesaId)) return;

            try
            {
                var todasMesas = await _mesaService.GetAll() ?? new List<Mesa>();
                string mNorm = mesaId.Replace("Mesa ", "").Trim();
                var mesa = todasMesas.FirstOrDefault(m =>
                    m.Identificacao != null &&
                    (m.Identificacao.Equals(mesaId, StringComparison.OrdinalIgnoreCase) ||
                     m.Identificacao.Replace("Mesa ", "").Trim().Equals(mNorm, StringComparison.OrdinalIgnoreCase)));

                if (mesa != null && mesa.Status != StatusMesaConsts.Ocupada)
                {
                    mesa.Status = StatusMesaConsts.Ocupada;
                    await _mesaService.SaveAsync(mesa);
                }

                // Atualiza o estado visual no mapa de salão em tempo real
                var todasComandas = await GetAllAsync();
                var comandasMesa = todasComandas.Where(c =>
                    c.Status != "Fechada" && c.Status != "Cancelada" &&
                    !string.IsNullOrWhiteSpace(c.MesaId) &&
                    (c.MesaId.Equals(mesaId, StringComparison.OrdinalIgnoreCase) ||
                     c.MesaId.Replace("Mesa ", "").Trim().Equals(mNorm, StringComparison.OrdinalIgnoreCase))
                ).ToList();

                int totalCmds = Math.Max(1, comandasMesa.Count);
                double totalSub = comandasMesa.Sum(c => c.Total) + valorInicial;

                await _mesaLayoutService.AtualizarStatusOperacionalAsync(
                    mesaId,
                    StatusMesaConsts.Ocupada,
                    totalCmds,
                    totalSub
                );
            }
            catch { }
        }


        public async Task<bool> TransferirMesaAsync(int comandaId, string novaMesaId)
        {
            var comanda = await GetByIdAsync(comandaId);
            if (comanda == null) return false;

            string? mesaAntiga = comanda.MesaId;
            comanda.MesaId = novaMesaId;
            bool ok = await _comandaService.SaveAsync(comanda) > 0;

            if (ok)
            {
                var todasMesas = await _mesaService.GetAll() ?? new List<Mesa>();

                // Se a mesa antiga não tiver mais comandas abertas, liberar a mesa
                if (!string.IsNullOrWhiteSpace(mesaAntiga))
                {
                    var restantes = await ObterComandasDaMesaAsync(mesaAntiga);
                    if (!restantes.Any())
                    {
                        string maNorm = mesaAntiga.Replace("Mesa ", "").Trim();
                        var mA = todasMesas.FirstOrDefault(m =>
                            m.Identificacao != null &&
                            (m.Identificacao.Equals(mesaAntiga, StringComparison.OrdinalIgnoreCase) ||
                             m.Identificacao.Replace("Mesa ", "").Trim().Equals(maNorm, StringComparison.OrdinalIgnoreCase)));
                        if (mA != null)
                        {
                            mA.Status = StatusMesaConsts.Livre;
                            await _mesaService.SaveAsync(mA);
                        }
                    }
                }

                // Ocupar nova mesa
                if (!string.IsNullOrWhiteSpace(novaMesaId))
                {
                    string mnNorm = novaMesaId.Replace("Mesa ", "").Trim();
                    var mN = todasMesas.FirstOrDefault(m =>
                        m.Identificacao != null &&
                        (m.Identificacao.Equals(novaMesaId, StringComparison.OrdinalIgnoreCase) ||
                         m.Identificacao.Replace("Mesa ", "").Trim().Equals(mnNorm, StringComparison.OrdinalIgnoreCase)));
                    if (mN != null)
                    {
                        mN.Status = StatusMesaConsts.Ocupada;
                        await _mesaService.SaveAsync(mN);
                    }
                }

                OnDataChanged?.Invoke();
            }

            return ok;
        }

        public async Task<bool> AtualizarCouvertAsync(int comandaId, bool cobrarCouvert, double valorCouvert, int pessoas = 1)
        {
            var comanda = await GetByIdAsync(comandaId);
            if (comanda == null) return false;

            double taxaCouvert = cobrarCouvert ? (valorCouvert * Math.Max(1, pessoas)) : 0.0;
            comanda.TaxaServico = taxaCouvert; // ou somar/dedicar campo
            bool ok = await _comandaService.SaveAsync(comanda) > 0;
            if (ok) OnDataChanged?.Invoke();
            return ok;
        }

        public async Task<double> ObterTotalConsolidadoMesaAsync(string mesaId)
        {
            var comandas = await ObterComandasDaMesaAsync(mesaId);
            return comandas.Sum(c => c.Total);
        }

        public void NotificarErro(string titulo, string mensagem)
        {
            // Event hook
        }

        public void NotificarSucesso(string titulo, string mensagem)
        {
            // Event hook
        }
    }
}
