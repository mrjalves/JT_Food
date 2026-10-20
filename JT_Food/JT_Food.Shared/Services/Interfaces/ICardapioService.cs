using System.Collections.Generic;
using System.Threading.Tasks;
using JT_Food.Shared.Domain.Entities;
using JT_Food.Shared.Domain.Enums;

namespace JT_Food.Shared.Services.Interfaces
{
    public interface ICardapioService
    {
        Task<List<ProdutoCardapio>> ObterProdutosAsync(CategoriaCardapioEnum? grupo = null, bool apenasDisponiveis = false);
        Task<ProdutoCardapio?> ObterProdutoPorIdAsync(int id);
        Task<bool> SalvarProdutoAsync(ProdutoCardapio produto);
        Task<bool> ExcluirProdutoAsync(int id);
        Task<bool> AlternarDisponibilidadeProdutoAsync(int id);
        
        Task<List<CategoriaMenu>> ObterCategoriasAsync();
        Task<bool> SalvarCategoriaAsync(CategoriaMenu categoria);
        Task<bool> ExcluirCategoriaAsync(int id);
        
        Task<List<RegraPromocao>> ObterRegrasPromocaoAsync(bool apenasAtivas = false);
        Task<bool> SalvarRegraPromocaoAsync(RegraPromocao regra);
        Task<bool> ExcluirRegraPromocaoAsync(int id);
        
        Task<double> ObterPrecoVigenteAsync(ProdutoCardapio produto);
    }
}
