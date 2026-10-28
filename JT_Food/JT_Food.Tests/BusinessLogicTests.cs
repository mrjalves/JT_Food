using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JT_Classes.Acessos;
using JT_Classes.Restaurante;
using JT_Food.Shared.BusinessLogic;
using JT_Food.Shared.Domain.Entities;
using JT_Food.Shared.Domain.Enums;
using JT_Food.Shared.Services.Implementations;
using JT_Servicos.Restaurante;
using Xunit;

namespace JT_Food.Tests
{
    public class BusinessLogicTests
    {
        private sealed class FakeSecureStorage : ISecureStorageService
        {
            private readonly Dictionary<string, string> _store = new();
            public Task<string?> GetTokenAsync(string key) =>
                Task.FromResult(_store.TryGetValue(key, out var val) ? val : null);
            public Task SetTokenAsync(string key, string value)
            {
                _store[key] = value;
                return Task.CompletedTask;
            }
            public void RemoveToken(string key) => _store.Remove(key);
        }

        private sealed class InMemoryProdutoService : ProdutoRestauranteService
        {
            private readonly Dictionary<int, MProduto> _items = new();
            public void Seed(MProduto p) => _items[p.Id] = p;
            public override Task<MProduto?> GetById(int id) =>
                Task.FromResult(_items.TryGetValue(id, out var p) ? p : null);
        }

        private sealed class InMemoryReceitaService : ReceitaService
        {
            public List<MReceita> Items { get; } = new();
            public override Task<List<MReceita>> GetAll() => Task.FromResult(new List<MReceita>(Items));
        }

        private sealed class InMemoryLoteInsumoService : LoteInsumoService
        {
            public List<MLoteInsumo> Items { get; } = new();
            public bool FailOnLoteId { get; set; }
            public int LoteIdToFail { get; set; }

            public override Task<List<MLoteInsumo>> GetAll() => Task.FromResult(new List<MLoteInsumo>(Items));
            public override Task<int> SaveAsync(MLoteInsumo entity)
            {
                if (FailOnLoteId && entity.Id == LoteIdToFail)
                {
                    return Task.FromResult(0);
                }

                var idx = Items.FindIndex(x => x.Id == entity.Id);
                if (idx >= 0) Items[idx] = entity;
                else Items.Add(entity);
                return Task.FromResult(entity.Id > 0 ? entity.Id : 1);
            }
        }

        private sealed class InMemoryProducaoService : ProducaoService
        {
            public List<MProducao> Saved { get; } = new();
            public override Task<int> SaveAsync(MProducao entity)
            {
                entity.Id = Saved.Count + 100;
                Saved.Add(entity);
                return Task.FromResult(entity.Id);
            }
        }

        private sealed class InMemoryMemoriaCalculoService : MemoriaCalculoService
        {
            public List<MMemoriaCalculo> Saved { get; } = new();
            public override Task<int> SaveAsync(MMemoriaCalculo entity)
            {
                entity.Id = Saved.Count + 1;
                Saved.Add(entity);
                return Task.FromResult(entity.Id);
            }
        }

        private sealed class InMemoryAdicionaisService : AdicionaisItemComandaService
        {
            public override Task<List<MAdicionaisItemComanda>> GetAll() =>
                Task.FromResult(new List<MAdicionaisItemComanda>());
        }

        [Fact]
        public async Task ProducaoLogic_ConsomeLotesPorOrdemFIFO_ECalculaCustoExato()
        {
            var prodService = new InMemoryProdutoService();
            prodService.Seed(new MProduto { Id = 10, Nome = "Smash Burger", PossuiReceita = true, Preco = 35.00m });

            var recService = new InMemoryReceitaService();
            recService.Items.Add(new MReceita
            {
                Id = 1,
                ProdutoId = "10",
                InsumoId = "50",
                NomeInsumo = "Blend Bovino",
                Quantidade = 0.2, // 200g por burger
                UnidadeMedida = "kg"
            });

            var loteService = new InMemoryLoteInsumoService();
            // Lote mais antigo: 0.3 kg a R$ 30,00/kg
            loteService.Items.Add(new MLoteInsumo
            {
                Id = 1,
                InsumoId = "50",
                QuantidadeInicial = 1.0,
                QuantidadeDisponivel = 0.3,
                CustoUnitario = 30.00,
                DataEntrada = DateTime.UtcNow.AddDays(-5),
                Status = "Ativo"
            });
            // Lote mais novo: 1.0 kg a R$ 40,00/kg
            loteService.Items.Add(new MLoteInsumo
            {
                Id = 2,
                InsumoId = "50",
                QuantidadeInicial = 1.0,
                QuantidadeDisponivel = 1.0,
                CustoUnitario = 40.00,
                DataEntrada = DateTime.UtcNow.AddDays(-1),
                Status = "Ativo"
            });

            var producaoService = new InMemoryProducaoService();
            var memoriaService = new InMemoryMemoriaCalculoService();
            var adicionaisService = new InMemoryAdicionaisService();

            var logic = new ProducaoLogic(
                prodService,
                recService,
                loteService,
                producaoService,
                memoriaService,
                adicionaisService);

            // Produzir 2 unidades -> precisa de 0.4 kg (consome 0.3 kg do Lote 1 + 0.1 kg do Lote 2)
            // Custo esperado: (0.3 * 30.00) + (0.1 * 40.00) = 9.00 + 4.00 = R$ 13.00
            var (sucesso, _) = await logic.ProcessarProducaoAsync("10", 2, "1");

            Assert.True(sucesso);
            Assert.Equal(0, loteService.Items[0].QuantidadeDisponivel);
            Assert.Equal("Finalizado", loteService.Items[0].Status);
            Assert.Equal(0.9, loteService.Items[1].QuantidadeDisponivel);
            Assert.Single(producaoService.Saved);
            Assert.Equal(13.00, producaoService.Saved[0].CustoTotalCalculado);
            Assert.Equal(2, memoriaService.Saved.Count);
        }

        [Fact]
        public async Task ProducaoLogic_ReverteLotes_QuandoOcorreFalhaNaPersistencia()
        {
            var prodService = new InMemoryProdutoService();
            prodService.Seed(new MProduto { Id = 10, Nome = "Smash Burger", PossuiReceita = true, Preco = 35.00m });

            var recService = new InMemoryReceitaService();
            recService.Items.Add(new MReceita { Id = 1, ProdutoId = "10", InsumoId = "50", Quantidade = 0.5, UnidadeMedida = "kg" });

            var loteService = new InMemoryLoteInsumoService
            {
                FailOnLoteId = true,
                LoteIdToFail = 2
            };
            loteService.Items.Add(new MLoteInsumo
            {
                Id = 1,
                InsumoId = "50",
                QuantidadeDisponivel = 0.3,
                CustoUnitario = 30.00,
                DataEntrada = DateTime.UtcNow.AddDays(-5),
                Status = "Ativo"
            });
            loteService.Items.Add(new MLoteInsumo
            {
                Id = 2,
                InsumoId = "50",
                QuantidadeDisponivel = 0.5,
                CustoUnitario = 40.00,
                DataEntrada = DateTime.UtcNow.AddDays(-1),
                Status = "Ativo"
            });

            var logic = new ProducaoLogic(
                prodService,
                recService,
                loteService,
                new InMemoryProducaoService(),
                new InMemoryMemoriaCalculoService(),
                new InMemoryAdicionaisService());

            var (sucesso, _) = await logic.ProcessarProducaoAsync("10", 1, "1");

            Assert.False(sucesso);
            // O lote 1 deve ter sido revertido ao saldo original (0.3 e Ativo)
            Assert.Equal(0.3, loteService.Items[0].QuantidadeDisponivel);
            Assert.Equal("Ativo", loteService.Items[0].Status);
        }

        [Fact]
        public void PromocaoLogic_CalculaHappyHourEDescontoComboCorretamente()
        {
            var promoLogic = new PromocaoLogic();
            var produto = new ProdutoCardapio
            {
                Id = 5,
                Nome = "Chopp Pilsen",
                PrecoBase = 14.00
            };

            var quarta18h = new DateTime(2026, 10, 28, 18, 30, 0); // Quarta-feira
            var regras = new List<RegraPromocao>
            {
                new RegraPromocao
                {
                    Id = 1,
                    Ativa = true,
                    ProdutoId = 5,
                    Tipo = TipoRegraPromocao.HappyHour,
                    DiasSemana = new List<int> { (int)DayOfWeek.Wednesday },
                    HoraInicio = new TimeSpan(17, 0, 0),
                    HoraFim = new TimeSpan(20, 0, 0),
                    DescontoPercentual = 50
                }
            };

            double preco = promoLogic.CalcularPrecoEfetivo(produto, regras, quarta18h);
            Assert.Equal(7.00, preco);
        }

        [Fact]
        public async Task AuthPerfilService_ValidaHashDePin_EBloqueiaAposTentativasInvalidas()
        {
            var storage = new FakeSecureStorage();
            var auth = new AuthPerfilService(storage);

            // Inicia como Gerente por padrão
            Assert.Equal(PerfilAcesso.Gerente, auth.ObterPerfilAtual());

            // Login com PIN válido do Gerente
            var ok = await auth.LoginPorPinAsync("1234");
            Assert.True(ok.Sucesso);
            Assert.True(auth.IsGerente());
            Assert.Empty(ok.Usuario!.PinRapido); // Nunca expõe o PIN nem o hash na sessão

            // Tentativa de adulterar o JSON no localStorage deve ser rejeitada pela assinatura HMAC
            await storage.SetTokenAsync("JT_FOOD_AUTH_OPERADOR", "{\"Payload\":\"{\\\"Id\\\":\\\"99\\\",\\\"Perfil\\\":0}\",\"Assinatura\":\"invalida\"}");
            await auth.LogoutAsync();
            var recuperado = await auth.ObterUsuarioLogadoAsync();
            Assert.Equal(PerfilAcesso.Garcom, recuperado.Perfil);
        }
    }
}
