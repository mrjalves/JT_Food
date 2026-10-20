using JT_Classes.Restaurante;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JT_Food.Shared.Services.Interfaces
{
    public interface IComandaService
    {
        Task<List<Comanda>> GetAllAsync();
        Task<List<Comanda>> FindAsync(string condicao);
        Task<List<Comanda>> FindAsync(Dictionary<string, object> filtros);
        Task<Comanda?> GetByIdAsync(int id);
        Task<bool> SaveAsync(Comanda entity);
        Task<bool> DeleteAsync(int id);
        
        // Métodos Operacionais Expandidos
        Task<List<Comanda>> BuscarComandasAsync(string? termoPesquisa, string? status = null);
        Task<List<Comanda>> ObterComandasDaMesaAsync(string mesaId);
        Task<Comanda> AbrirComandaFlexivelAsync(string? numeroCartao, string? nomeCliente, string? mesaId, int quantidadePessoas = 1, bool ativarCouvert = false, double valorCouvert = 0);
        Task<bool> TransferirMesaAsync(int comandaId, string novaMesaId);
        Task<bool> AtualizarCouvertAsync(int comandaId, bool cobrarCouvert, double valorCouvert, int pessoas = 1);
        Task<double> ObterTotalConsolidadoMesaAsync(string mesaId);
        
        void NotificarErro(string titulo, string mensagem);
        void NotificarSucesso(string titulo, string mensagem);
        
        event Action OnDataChanged;
    }
}
