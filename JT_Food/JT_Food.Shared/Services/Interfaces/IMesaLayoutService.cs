using System.Collections.Generic;
using System.Threading.Tasks;
using JT_Food.Shared.Domain.Entities;

namespace JT_Food.Shared.Services.Interfaces
{
    public interface IMesaLayoutService
    {
        Task<List<AmbienteSalao>> ObterAmbientesAsync();
        Task<AmbienteSalao> SalvarAmbienteAsync(AmbienteSalao ambiente);
        Task<bool> ExcluirAmbienteAsync(int id);
        
        Task<List<ElementoSalao>> ObterElementosPorAmbienteAsync(int ambienteId);
        Task<bool> SalvarElementoAsync(ElementoSalao elemento);
        Task<bool> SalvarElementosEmLoteAsync(List<ElementoSalao> elementos);
        Task<bool> ExcluirElementoAsync(int id);
        Task<ElementoSalao?> ObterElementoPorIdentificacaoAsync(string identificacao);
        Task<bool> AtualizarStatusOperacionalAsync(string identificacaoMesa, string novoStatus, int totalComandas, double subtotal);
    }
}
