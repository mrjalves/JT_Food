using System.Threading.Tasks;

namespace JT_Food.Shared.Services.Interfaces
{
    public interface IIAAnalyticsService
    {
        Task<string> GerarDiagnosticoGeralAsync(string promptContexto);
        Task<string> GerarSugestaoPromocoesAsync(string historicoVendasJson);
        Task<string> GerarDiagnosticoCozinhaEComprasAsync(string metricasKdsJson, string statusEstoqueJson);
    }
}
