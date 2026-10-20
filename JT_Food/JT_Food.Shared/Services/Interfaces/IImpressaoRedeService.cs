using System.Collections.Generic;
using System.Threading.Tasks;
using JT_Food.Shared.Domain.Entities;

namespace JT_Food.Shared.Services.Interfaces
{
    public interface IImpressaoRedeService
    {
        Task<List<ImpressoraRede>> ObterImpressorasAsync();
        Task<bool> SalvarImpressoraAsync(ImpressoraRede impressora);
        Task<bool> ExcluirImpressoraAsync(int id);
        Task<(bool Sucesso, string Mensagem)> TestarConexaoAsync(ImpressoraRede impressora);
        Task<List<(string Setor, bool Sucesso, string Mensagem)>> RoteamentoEImpressaoPedidoAsync(Pedido pedido);
        Task<(bool Sucesso, string Mensagem)> ImprimirContaAsync(Comanda comanda);
    }
}
