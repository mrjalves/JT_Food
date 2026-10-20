using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JT_Food.Shared.BusinessLogic;
using JT_Food.Shared.Domain.Entities;
using JT_Food.Shared.Domain.Enums;
using JT_Food.Shared.Services.Interfaces;

namespace JT_Food.Shared.Services.Implementations
{
    public class CardapioService : ICardapioService
    {
        private readonly PromocaoLogic _promocaoLogic;
        private readonly List<CategoriaMenu> _categorias = new();
        private readonly List<ProdutoCardapio> _produtos = new();
        private readonly List<RegraPromocao> _regrasPromocao = new();

        public CardapioService(PromocaoLogic promocaoLogic)
        {
            _promocaoLogic = promocaoLogic;
            InicializarDadosIniciais();
        }

        private void InicializarDadosIniciais()
        {
            if (_categorias.Any()) return;

            // Categorias
            _categorias.AddRange(new[]
            {
                new CategoriaMenu { Id = 1, Nome = "🔥 Promoções & Happy Hour", Grupo = CategoriaCardapioEnum.Lanches, Icone = "bi-fire", Ordem = 1 },
                new CategoriaMenu { Id = 2, Nome = "🍺 Bebidas & Chopp", Grupo = CategoriaCardapioEnum.Bebidas, Icone = "bi-cup-straw", ImpressoraDestinoId = 2, Ordem = 2 },
                new CategoriaMenu { Id = 3, Nome = "🍟 Porções & Petiscos", Grupo = CategoriaCardapioEnum.Porcoes, Icone = "bi-egg-fried", ImpressoraDestinoId = 1, Ordem = 3 },
                new CategoriaMenu { Id = 4, Nome = "🍔 Hambúrgueres & Lanches", Grupo = CategoriaCardapioEnum.Lanches, Icone = "bi-award", ImpressoraDestinoId = 3, Ordem = 4 },
                new CategoriaMenu { Id = 5, Nome = "🍽️ Pratos Principais", Grupo = CategoriaCardapioEnum.Pratos, Icone = "bi-fork-knife", ImpressoraDestinoId = 1, Ordem = 5 },
                new CategoriaMenu { Id = 6, Nome = "🍰 Sobremesas", Grupo = CategoriaCardapioEnum.Sobremesas, Icone = "bi-cake2", ImpressoraDestinoId = 1, Ordem = 6 },
                new CategoriaMenu { Id = 7, Nome = "🎵 Taxas & Couvert", Grupo = CategoriaCardapioEnum.TaxasCouvert, Icone = "bi-music-note-beamed", Ordem = 7 }
            });

            // Produtos
            _produtos.AddRange(new[]
            {
                new ProdutoCardapio
                {
                    Id = 101,
                    Nome = "Chopp Pilsen Artesanal 500ml",
                    Descricao = "Chopp puro malte geladíssimo com colarinho cremoso.",
                    CategoriaId = 2,
                    Grupo = CategoriaCardapioEnum.Bebidas,
                    PrecoBase = 16.00,
                    PrecoPromocional = 10.90,
                    EmPromocao = true,
                    ImpressoraDestinoId = 2,
                    TempoMedioPreparoMinutos = 3,
                    ObservacoesSugeridas = new() { "Com colarinho", "Sem colarinho", "Copo congelado" }
                },
                new ProdutoCardapio
                {
                    Id = 102,
                    Nome = "Refrigerante Lata 350ml",
                    Descricao = "Coca-Cola, Guaraná Antarctica ou Zero.",
                    CategoriaId = 2,
                    Grupo = CategoriaCardapioEnum.Bebidas,
                    PrecoBase = 7.50,
                    ImpressoraDestinoId = 2,
                    TempoMedioPreparoMinutos = 2,
                    ObservacoesSugeridas = new() { "Gelo e limão", "Sem gelo", "Copos separados" }
                },
                new ProdutoCardapio
                {
                    Id = 103,
                    Nome = "Suco Natural da Fruta 400ml",
                    Descricao = "Laranja, Limonada Suíça, Maracujá ou Morango.",
                    CategoriaId = 2,
                    Grupo = CategoriaCardapioEnum.Bebidas,
                    PrecoBase = 12.00,
                    ImpressoraDestinoId = 2,
                    TempoMedioPreparoMinutos = 5,
                    ObservacoesSugeridas = new() { "Sem açúcar", "Com adoçante", "Muito gelo" }
                },
                new ProdutoCardapio
                {
                    Id = 201,
                    Nome = "Batata Frita Especial com Cheddar e Bacon",
                    Descricao = "500g de batata crocante coberta com creme de cheddar e crispy de bacon.",
                    CategoriaId = 3,
                    Grupo = CategoriaCardapioEnum.Porcoes,
                    PrecoBase = 42.00,
                    ImpressoraDestinoId = 1,
                    TempoMedioPreparoMinutos = 12,
                    ObservacoesSugeridas = new() { "Bacon bem crocante", "Cheddar à parte", "Sem sal" }
                },
                new ProdutoCardapio
                {
                    Id = 202,
                    Nome = "Isca de Picanha Acebolada",
                    Descricao = "Tiras macias de picanha grelhada com cebola roxa e fatias de pão de alho.",
                    CategoriaId = 3,
                    Grupo = CategoriaCardapioEnum.Porcoes,
                    PrecoBase = 68.00,
                    PrecoPromocional = 54.90,
                    EmPromocao = true,
                    ImpressoraDestinoId = 1,
                    TempoMedioPreparoMinutos = 18,
                    ObservacoesSugeridas = new() { "Ao ponto", "Bem passada", "Sem cebola", "Pão bem tostado" }
                },
                new ProdutoCardapio
                {
                    Id = 301,
                    Nome = "Burger Artesanal JT Bacon Blast",
                    Descricao = "Blend bovino 180g, queijo gouda derretido, fatias de bacon rústico e maionese defumada.",
                    CategoriaId = 4,
                    Grupo = CategoriaCardapioEnum.Lanches,
                    PrecoBase = 38.00,
                    ImpressoraDestinoId = 3,
                    TempoMedioPreparoMinutos = 15,
                    ObservacoesSugeridas = new() { "Ponto da carne: ao ponto", "Sem cebola", "Sem maionese", "Carne bem passada" }
                },
                new ProdutoCardapio
                {
                    Id = 302,
                    Nome = "Smash Burger Duplo Cheddar",
                    Descricao = "Dois smashs de 90g ultra prensados, muito cheddar e picles artesanal.",
                    CategoriaId = 4,
                    Grupo = CategoriaCardapioEnum.Lanches,
                    PrecoBase = 32.00,
                    PrecoPromocional = 24.90,
                    EmPromocao = true,
                    ImpressoraDestinoId = 3,
                    TempoMedioPreparoMinutos = 10,
                    ObservacoesSugeridas = new() { "Sem picles", "Molho à parte", "Extra crocante" }
                },
                new ProdutoCardapio
                {
                    Id = 401,
                    Nome = "Filé à Parmegiana Família",
                    Descricao = "Acompanha arroz branco soltinho e batata frita rústica (Serve até 3 pessoas).",
                    CategoriaId = 5,
                    Grupo = CategoriaCardapioEnum.Pratos,
                    PrecoBase = 98.00,
                    ImpressoraDestinoId = 1,
                    TempoMedioPreparoMinutos = 25,
                    ObservacoesSugeridas = new() { "Molho bem quente", "Fritas separadas", "Sem orégano" }
                },
                new ProdutoCardapio
                {
                    Id = 501,
                    Nome = "Petit Gâteau com Sorvete de Creme",
                    Descricao = "Bolinho quente de chocolate belga recheado, servido com sorvete de baunilha e calda de frutas vermelhas.",
                    CategoriaId = 6,
                    Grupo = CategoriaCardapioEnum.Sobremesas,
                    PrecoBase = 26.00,
                    ImpressoraDestinoId = 1,
                    TempoMedioPreparoMinutos = 8,
                    ObservacoesSugeridas = new() { "Sem calda", "Calda à parte" }
                },
                new ProdutoCardapio
                {
                    Id = 601,
                    Nome = "Couvert Artístico ao Vivo",
                    Descricao = "Taxa artística para shows de voz e violão e atrações ao vivo.",
                    CategoriaId = 7,
                    Grupo = CategoriaCardapioEnum.TaxasCouvert,
                    PrecoBase = 15.00,
                    TempoMedioPreparoMinutos = 0,
                    ObservacoesSugeridas = new() { "Cobrança individual", "Isenção aniversariante" }
                }
            });

            // Regras de Promoção
            _regrasPromocao.AddRange(new[]
            {
                new RegraPromocao
                {
                    Id = 1,
                    Titulo = "Happy Hour Semanal de Chopp",
                    Descricao = "Chopp 500ml por R$ 10,90 de Quarta e Quinta das 17h às 20h.",
                    Tipo = TipoRegraPromocao.HappyHour,
                    ProdutoId = 101,
                    NomeProduto = "Chopp Pilsen Artesanal 500ml",
                    PrecoPromocionalFixo = 10.90,
                    DiasSemana = new() { 3, 4 },
                    HoraInicio = new TimeSpan(17, 0, 0),
                    HoraFim = new TimeSpan(20, 0, 0),
                    Ativa = true
                },
                new RegraPromocao
                {
                    Id = 2,
                    Titulo = "Combo Sexta Burguer + Fritas + Chopp",
                    Descricao = "Burger Bacon Blast + Porção Fritas + Chopp com 20% OFF.",
                    Tipo = TipoRegraPromocao.Combo,
                    ProdutosComboIds = new() { 301, 201, 101 },
                    DescontoPercentual = 20,
                    Ativa = true
                }
            });
        }

        public Task<List<ProdutoCardapio>> ObterProdutosAsync(CategoriaCardapioEnum? grupo = null, bool apenasDisponiveis = false)
        {
            var query = _produtos.AsEnumerable();
            if (grupo.HasValue)
                query = query.Where(p => p.Grupo == grupo.Value);
            if (apenasDisponiveis)
                query = query.Where(p => p.Disponivel);

            return Task.FromResult(query.OrderBy(p => p.Nome).ToList());
        }

        public Task<ProdutoCardapio?> ObterProdutoPorIdAsync(int id)
        {
            var prod = _produtos.FirstOrDefault(p => p.Id == id);
            return Task.FromResult(prod);
        }

        public Task<bool> SalvarProdutoAsync(ProdutoCardapio produto)
        {
            if (produto.Id == 0)
            {
                produto.Id = _produtos.Any() ? _produtos.Max(p => p.Id) + 1 : 100;
                _produtos.Add(produto);
            }
            else
            {
                var idx = _produtos.FindIndex(p => p.Id == produto.Id);
                if (idx >= 0) _produtos[idx] = produto;
                else _produtos.Add(produto);
            }
            return Task.FromResult(true);
        }

        public Task<bool> ExcluirProdutoAsync(int id)
        {
            var removido = _produtos.RemoveAll(p => p.Id == id) > 0;
            return Task.FromResult(removido);
        }

        public Task<bool> AlternarDisponibilidadeProdutoAsync(int id)
        {
            var prod = _produtos.FirstOrDefault(p => p.Id == id);
            if (prod != null)
            {
                prod.Disponivel = !prod.Disponivel;
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public Task<List<CategoriaMenu>> ObterCategoriasAsync()
        {
            return Task.FromResult(_categorias.OrderBy(c => c.Ordem).ToList());
        }

        public Task<bool> SalvarCategoriaAsync(CategoriaMenu categoria)
        {
            if (categoria.Id == 0)
            {
                categoria.Id = _categorias.Any() ? _categorias.Max(c => c.Id) + 1 : 1;
                _categorias.Add(categoria);
            }
            else
            {
                var idx = _categorias.FindIndex(c => c.Id == categoria.Id);
                if (idx >= 0) _categorias[idx] = categoria;
                else _categorias.Add(categoria);
            }
            return Task.FromResult(true);
        }

        public Task<bool> ExcluirCategoriaAsync(int id)
        {
            var removido = _categorias.RemoveAll(c => c.Id == id) > 0;
            return Task.FromResult(removido);
        }

        public Task<List<RegraPromocao>> ObterRegrasPromocaoAsync(bool apenasAtivas = false)
        {
            var query = _regrasPromocao.AsEnumerable();
            if (apenasAtivas) query = query.Where(r => r.Ativa);
            return Task.FromResult(query.ToList());
        }

        public Task<bool> SalvarRegraPromocaoAsync(RegraPromocao regra)
        {
            if (regra.Id == 0)
            {
                regra.Id = _regrasPromocao.Any() ? _regrasPromocao.Max(r => r.Id) + 1 : 1;
                _regrasPromocao.Add(regra);
            }
            else
            {
                var idx = _regrasPromocao.FindIndex(r => r.Id == regra.Id);
                if (idx >= 0) _regrasPromocao[idx] = regra;
                else _regrasPromocao.Add(regra);
            }
            return Task.FromResult(true);
        }

        public Task<bool> ExcluirRegraPromocaoAsync(int id)
        {
            var removido = _regrasPromocao.RemoveAll(r => r.Id == id) > 0;
            return Task.FromResult(removido);
        }

        public Task<double> ObterPrecoVigenteAsync(ProdutoCardapio produto)
        {
            var preco = _promocaoLogic.CalcularPrecoEfetivo(produto, _regrasPromocao);
            return Task.FromResult(preco);
        }
    }
}
