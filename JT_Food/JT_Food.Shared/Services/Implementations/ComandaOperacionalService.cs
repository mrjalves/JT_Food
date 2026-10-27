using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JT_Classes.Restaurante;
using JT_Food.Shared.BusinessLogic;
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
        private readonly CaixaLogic _caixaLogic;
        private static readonly System.Threading.SemaphoreSlim _aberturaLock = new(1, 1);

        public event Action? OnDataChanged;

        public ComandaOperacionalService(
            ComandaService comandaService,
            MesaService mesaService,
            ItemComandaService itemComandaService,
            IMesaLayoutService mesaLayoutService,
            CaixaLogic caixaLogic)
        {
            _comandaService = comandaService;
            _mesaService = mesaService;
            _itemComandaService = itemComandaService;
            _mesaLayoutService = mesaLayoutService;
            _caixaLogic = caixaLogic;
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

        public async Task<(bool Aberto, string Mensagem)> VerificarCaixaAbertoAsync()
        {
            var (sucesso, _, caixaAtual) = await _caixaLogic.ObterCaixaAbertoAsync();
            if (sucesso && caixaAtual != null)
            {
                return (true, $"Caixa #{caixaAtual.Id} aberto.");
            }
            return (false, "O Caixa está fechado. Abra o caixa no menu Caixa & PDV antes de realizar novos atendimentos.");
        }

        public async Task<bool> SaveAsync(Comanda entity)
        {
            if (entity.Id == 0)
            {
                var (aberto, msg) = await VerificarCaixaAbertoAsync();
                if (!aberto)
                {
                    throw new InvalidOperationException("Não é possível abrir comandas com o caixa fechado. Por favor, realize a abertura do caixa no menu Caixa & PDV antes de iniciar os atendimentos.");
                }
            }
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
                .Where(c => c.Status != "Fechada" && c.Status != "Cancelada" &&
                            !string.IsNullOrWhiteSpace(c.MesaId) &&
                            MesmaMesa(c.MesaId, mesaId))
                .OrderBy(c => c.Id)
                .ToList();
        }

        public async Task<List<Comanda>> ObterComandasIrmasAsync(int comandaId)
        {
            var todas = await GetAllAsync();
            var comandaBase = todas.FirstOrDefault(c => c.Id == comandaId) ?? await GetByIdAsync(comandaId);
            if (comandaBase == null) return new List<Comanda>();

            var abertas = todas.Where(c => c.Status != "Fechada" && c.Status != "Cancelada").ToList();
            string idCartao = ExtrairNumeroCartao(comandaBase.NomeCliente);

            var irmas = abertas.Where(c =>
                c.Id == comandaBase.Id ||
                (!string.IsNullOrEmpty(idCartao) && ComandaPertenceAoCartao(c, idCartao)) ||
                (!string.IsNullOrWhiteSpace(comandaBase.MesaId) &&
                 !comandaBase.MesaId.Contains("Balcão", StringComparison.OrdinalIgnoreCase) &&
                 !comandaBase.MesaId.Contains("Balcao", StringComparison.OrdinalIgnoreCase) &&
                 MesmaMesa(c.MesaId, comandaBase.MesaId))
            ).OrderBy(c => c.Id).ToList();

            if (!irmas.Any(c => c.Id == comandaBase.Id))
            {
                irmas.Insert(0, comandaBase);
            }

            return irmas;
        }

        public static string ExtrairNumeroCartao(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return string.Empty;
            var match = System.Text.RegularExpressions.Regex.Match(valor, @"\d+");
            return match.Success ? match.Value : valor.Trim();
        }

        public static bool ComandaPertenceAoCartao(Comanda comanda, string numeroCartaoOuTexto)
        {
            if (string.IsNullOrWhiteSpace(numeroCartaoOuTexto) || string.IsNullOrWhiteSpace(comanda.NomeCliente)) return false;
            string idBuscado = ExtrairNumeroCartao(numeroCartaoOuTexto);
            if (string.IsNullOrEmpty(idBuscado)) return false;

            string idComanda = ExtrairNumeroCartao(comanda.NomeCliente);
            if (!string.IsNullOrEmpty(idComanda) && idComanda == idBuscado) return true;

            string nome = comanda.NomeCliente;
            return nome.Equals($"Cartão #{idBuscado}", StringComparison.OrdinalIgnoreCase) ||
                   nome.Equals($"Cartão #{numeroCartaoOuTexto.Trim()}", StringComparison.OrdinalIgnoreCase) ||
                   nome.Contains($"[Cartão {idBuscado}]", StringComparison.OrdinalIgnoreCase) ||
                   nome.Contains($"Cartão #{idBuscado}", StringComparison.OrdinalIgnoreCase);
        }

        public static string NormalizarMesaId(string? mesaId)
        {
            if (string.IsNullOrWhiteSpace(mesaId)) return string.Empty;
            string limpo = mesaId.Trim();
            if (limpo.StartsWith("Mesa ", StringComparison.OrdinalIgnoreCase))
                limpo = limpo.Substring(5).Trim();
            if (int.TryParse(limpo, out int num))
                return num.ToString();
            return limpo.ToLowerInvariant();
        }

        public static bool MesmaMesa(string? m1, string? m2)
        {
            if (string.IsNullOrWhiteSpace(m1) || string.IsNullOrWhiteSpace(m2)) return false;
            if (string.Equals(m1.Trim(), m2.Trim(), StringComparison.OrdinalIgnoreCase)) return true;
            return string.Equals(NormalizarMesaId(m1), NormalizarMesaId(m2), StringComparison.OrdinalIgnoreCase);
        }

        public async Task<Comanda> AbrirComandaFlexivelAsync(
            string? numeroCartao,
            string? nomeCliente,
            string? mesaId,
            int quantidadePessoas = 1,
            bool ativarCouvert = false,
            double valorCouvert = 0)
        {
            var (caixaAberto, msgCaixa) = await VerificarCaixaAbertoAsync();
            if (!caixaAberto)
            {
                throw new InvalidOperationException("Não é possível abrir comandas com o caixa fechado. Por favor, realize a abertura do caixa no menu Caixa & PDV antes de iniciar os atendimentos.");
            }

            await _aberturaLock.WaitAsync();
            try
            {
                var todas = await GetAllAsync();
                var abertas = todas.Where(c => c.Status != "Fechada" && c.Status != "Cancelada").ToList();

                string? cartaoLimpo = !string.IsNullOrWhiteSpace(numeroCartao) ? numeroCartao.Trim() : null;
                string? clienteLimpo = !string.IsNullOrWhiteSpace(nomeCliente) ? nomeCliente.Trim() : null;

                // Tentar extrair cartão tanto do campo de número de cartão quanto do nomeCliente digitado
                string? idCartao = !string.IsNullOrWhiteSpace(cartaoLimpo) 
                    ? ExtrairNumeroCartao(cartaoLimpo) 
                    : (!string.IsNullOrWhiteSpace(clienteLimpo) && clienteLimpo.Contains("Cartão", StringComparison.OrdinalIgnoreCase) ? ExtrairNumeroCartao(clienteLimpo) : null);

                // 1. REGRA ANTI-DUPLICAÇÃO: Se já existe comanda aberta com este número de cartão no salão
                if (!string.IsNullOrWhiteSpace(idCartao))
                {
                    var abertaComCartao = abertas.FirstOrDefault(c => ComandaPertenceAoCartao(c, idCartao));
                    if (abertaComCartao != null)
                    {
                        // Cartão já aberto no salão! Atualiza mesa se fornecida e retorna a comanda existente (NUNCA DUPLICA!)
                        bool alterou = false;
                        if (!string.IsNullOrWhiteSpace(mesaId) && !MesmaMesa(abertaComCartao.MesaId, mesaId))
                        {
                            abertaComCartao.MesaId = mesaId;
                            alterou = true;
                        }

                        if (!string.IsNullOrWhiteSpace(clienteLimpo) && 
                            (abertaComCartao.NomeCliente == null || !abertaComCartao.NomeCliente.Contains(clienteLimpo, StringComparison.OrdinalIgnoreCase)))
                        {
                            abertaComCartao.NomeCliente = $"[Cartão {idCartao}] {clienteLimpo}";
                            alterou = true;
                        }

                        if (ativarCouvert)
                        {
                            double taxaAdicional = valorCouvert * Math.Max(1, quantidadePessoas);
                            if (abertaComCartao.TaxaServico != taxaAdicional)
                            {
                                abertaComCartao.TaxaServico = taxaAdicional;
                                alterou = true;
                            }
                        }

                        if (alterou)
                        {
                            await _comandaService.SaveAsync(abertaComCartao);
                            await RecalcularTotalComandaAsync(abertaComCartao.Id);
                        }

                        await GarantirMesaOcupadaAsync(mesaId ?? abertaComCartao.MesaId);
                        return abertaComCartao;
                    }
                }

                // 2. REGRA ANTI-DUPLICAÇÃO PARA A MESA:
                if (!string.IsNullOrWhiteSpace(mesaId))
                {
                    var comandasDaMesa = abertas.Where(c => MesmaMesa(c.MesaId, mesaId)).OrderBy(c => c.Id).ToList();

                    if (comandasDaMesa.Any())
                    {
                        // 2.1 Se informou cliente, verifica se já tem comanda com esse cliente na mesa
                        if (!string.IsNullOrWhiteSpace(clienteLimpo))
                        {
                            var comandaCliente = comandasDaMesa.FirstOrDefault(c =>
                                c.NomeCliente != null && (
                                    c.NomeCliente.Equals(clienteLimpo, StringComparison.OrdinalIgnoreCase) ||
                                    c.NomeCliente.EndsWith($" {clienteLimpo}", StringComparison.OrdinalIgnoreCase) ||
                                    c.NomeCliente.EndsWith($"] {clienteLimpo}", StringComparison.OrdinalIgnoreCase)
                                ));

                            if (comandaCliente != null)
                            {
                                if (!string.IsNullOrWhiteSpace(idCartao) && !ComandaPertenceAoCartao(comandaCliente, idCartao))
                                {
                                    comandaCliente.NomeCliente = $"[Cartão {idCartao}] {clienteLimpo}";
                                    await _comandaService.SaveAsync(comandaCliente);
                                }
                                await GarantirMesaOcupadaAsync(mesaId);
                                return comandaCliente;
                            }
                        }

                        // 2.2 Se a mesa já possui uma "Conta Geral" ou comanda aberta sem cartão específico:
                        // Atualiza a comanda existente para ter o cartão/cliente informado, SEM DUPLICAR
                        var contaGeralOuSemCartao = comandasDaMesa.FirstOrDefault(c =>
                            c.NomeCliente == null ||
                            c.NomeCliente.StartsWith("Conta Geral", StringComparison.OrdinalIgnoreCase) ||
                            string.IsNullOrEmpty(ExtrairNumeroCartao(c.NomeCliente)));

                        if (contaGeralOuSemCartao != null)
                        {
                            string novoNome = "";
                            if (!string.IsNullOrWhiteSpace(idCartao) && !string.IsNullOrWhiteSpace(clienteLimpo) && !clienteLimpo.Equals($"Cartão #{idCartao}", StringComparison.OrdinalIgnoreCase))
                                novoNome = $"[Cartão {idCartao}] {clienteLimpo}";
                            else if (!string.IsNullOrWhiteSpace(idCartao))
                                novoNome = $"Cartão #{idCartao}";
                            else if (!string.IsNullOrWhiteSpace(clienteLimpo))
                                novoNome = clienteLimpo;

                            if (!string.IsNullOrWhiteSpace(novoNome))
                            {
                                contaGeralOuSemCartao.NomeCliente = novoNome;
                            }

                            if (ativarCouvert)
                            {
                                double taxaAdicional = valorCouvert * Math.Max(1, quantidadePessoas);
                                if (contaGeralOuSemCartao.TaxaServico != taxaAdicional)
                                {
                                    contaGeralOuSemCartao.TaxaServico = taxaAdicional;
                                }
                            }

                            await _comandaService.SaveAsync(contaGeralOuSemCartao);
                            await RecalcularTotalComandaAsync(contaGeralOuSemCartao.Id);
                            await GarantirMesaOcupadaAsync(mesaId);
                            return contaGeralOuSemCartao;
                        }

                        // 2.3 Se o usuário NÃO informou cartão nem cliente (ex: lançamento rápido na mesa),
                        // REUTILIZA a primeira comanda aberta da mesa para NUNCA duplicar!
                        if (string.IsNullOrWhiteSpace(idCartao) && string.IsNullOrWhiteSpace(clienteLimpo))
                        {
                            var primeiraAberta = comandasDaMesa.First();
                            await GarantirMesaOcupadaAsync(mesaId);
                            return primeiraAberta;
                        }
                    }
                }

                // 3. Formatar nome / cartão para a nova comanda
                string nomeFormatado = "";
                if (!string.IsNullOrWhiteSpace(idCartao) && !string.IsNullOrWhiteSpace(clienteLimpo) && !clienteLimpo.Equals($"Cartão #{idCartao}", StringComparison.OrdinalIgnoreCase))
                    nomeFormatado = $"[Cartão {idCartao}] {clienteLimpo}";
                else if (!string.IsNullOrWhiteSpace(idCartao))
                    nomeFormatado = $"Cartão #{idCartao}";
                else if (!string.IsNullOrWhiteSpace(clienteLimpo))
                    nomeFormatado = clienteLimpo;
                else
                    nomeFormatado = !string.IsNullOrWhiteSpace(mesaId) ? $"Conta Geral {mesaId}" : "Consumidor Balcão";

                double taxaCouvertCalculada = ativarCouvert ? (valorCouvert * Math.Max(1, quantidadePessoas)) : 0.0;

                var novaComanda = new Comanda
                {
                    UserId = "1",
                    MesaId = mesaId,
                    NomeCliente = nomeFormatado,
                    DataAbertura = DateTime.UtcNow,
                    Status = "Aberta",
                    TaxaServico = taxaCouvertCalculada,
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

        public async Task<double> RecalcularTotalComandaAsync(int comandaId)
        {
            var comanda = await GetByIdAsync(comandaId);
            if (comanda == null) return 0;

            var todosItens = await _itemComandaService.GetAll();
            string cid = comandaId.ToString();
            var itensAtivos = (todosItens ?? Enumerable.Empty<MItemComanda>())
                .Where(i => !string.IsNullOrWhiteSpace(i.ComandaId) &&
                            (i.ComandaId == cid || i.ComandaId.Trim().TrimStart('#') == cid) &&
                            i.Status != "Cancelado")
                .ToList();

            double subtotalItens = itensAtivos.Sum(i => i.TotalItem > 0 ? i.TotalItem : (i.Quantidade * i.PrecoUnitario));
            comanda.Total = Math.Max(0, subtotalItens + comanda.TaxaServico - comanda.Desconto);

            await _comandaService.SaveAsync(comanda);

            if (!string.IsNullOrWhiteSpace(comanda.MesaId))
            {
                await GarantirMesaOcupadaAsync(comanda.MesaId);
            }

            OnDataChanged?.Invoke();
            return comanda.Total;
        }

        private async Task GarantirMesaOcupadaAsync(string? mesaId, double valorInicial = 0)
        {
            if (string.IsNullOrWhiteSpace(mesaId)) return;

            try
            {
                var todasMesas = await _mesaService.GetAll() ?? new List<Mesa>();
                var mesa = todasMesas.FirstOrDefault(m =>
                    !string.IsNullOrWhiteSpace(m.Identificacao) &&
                    MesmaMesa(m.Identificacao, mesaId));

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
                    MesmaMesa(c.MesaId, mesaId)
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
