using JT_Aplicacao.Classes;
using JT_Aplicacao.Eventos;
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
        Task<Comanda> GetByIdAsync(int id);
        Task<bool> SaveAsync(Comanda entity);
        Task<bool> DeleteAsync(int id);
        
        void NotificarErro(string titulo, string mensagem);
        void NotificarSucesso(string titulo, string mensagem);
        
        event Action OnDataChanged;
    }
}

