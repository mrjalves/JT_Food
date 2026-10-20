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

        public event Action? OnDataChanged;

        public ComandaOperacionalService(
            ComandaService comandaService,
            MesaService mesaService,
            ItemComandaService itemComandaService)
        {
            _comandaService = comandaService;
            _mesaService = mesaService;
            _itemComandaService = itemComandaService;
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
            var condicoes = filtros.Select(f => $"{f.Key} = '{f.Value}'");
            string condicao = string.Join(" AND ", condicoes);
            return await FindAsync(condicao);
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
            return todas
                .Where(c => c.MesaId == mesaId && c.Status != "Fechada" && c.Status != "Cancelada")
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
            // Formatar nome / cartão
            string nomeFormatado = "";
            if (!string.IsNullOrWhiteSpace(numeroCartao) && !string.IsNullOrWhiteSpace(nomeCliente))
                nomeFormatado = $"[Cartão {numeroCartao}] {nomeCliente}";
            else if (!string.IsNullOrWhiteSpace(numeroCartao))
                nomeFormatado = $"Cartão #{numeroCartao}";
            else if (!string.IsNullOrWhiteSpace(nomeCliente))
                nomeFormatado = nomeCliente;
            else
                nomeFormatado = "Consumidor";

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

            // Se associada a mesa, ocupar mesa
            if (!string.IsNullOrWhiteSpace(mesaId))
            {
                var mesas = await _mesaService.Find($"Identificacao = '{mesaId}'");
                var mesa = mesas?.FirstOrDefault();
                if (mesa != null && mesa.Status == StatusMesaConsts.Livre)
                {
                    mesa.Status = StatusMesaConsts.Ocupada;
                    await _mesaService.SaveAsync(mesa);
                }
            }

            OnDataChanged?.Invoke();
            return novaComanda;
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
                // Se a mesa antiga não tiver mais comandas abertas, liberar a mesa
                if (!string.IsNullOrWhiteSpace(mesaAntiga))
                {
                    var restantes = await ObterComandasDaMesaAsync(mesaAntiga);
                    if (!restantes.Any())
                    {
                        var mesasAntigas = await _mesaService.Find($"Identificacao = '{mesaAntiga}'");
                        var mA = mesasAntigas?.FirstOrDefault();
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
                    var novasMesas = await _mesaService.Find($"Identificacao = '{novaMesaId}'");
                    var mN = novasMesas?.FirstOrDefault();
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
