using System.Collections.Generic;
using System.Threading.Tasks;
using JT_Food.Shared.Domain.Entities;

namespace JT_Food.Shared.Services.Interfaces
{
    public interface IIAAnalyticsService
    {
        Task<string> GerarDiagnosticoGeralAsync(string promptContexto);
        Task<string> GerarSugestaoPromocoesAsync(string historicoVendasJson);
        Task<string> GerarDiagnosticoCozinhaEComprasAsync(string metricasKdsJson, string statusEstoqueJson);
        Task<(bool Sucesso, string Mensagem, List<GeminiModelInfo> Modelos)> IdentificarModelosDisponiveisAsync(string apiKey);
    }
}

