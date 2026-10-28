using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JT_Classes.Restaurante;
using JT_Servicos.Restaurante;

namespace JT_Food.Shared.BusinessLogic
{
    public class KDSLogic
    {
        private readonly ItemComandaService _itemComandaService;
        private readonly ProducaoLogic _producaoLogic;

        public event Action? OnKdsAtualizado;

        public static readonly string[] CicloStatus = { "Pendente", "Em Produção", "Saída para Entrega", "Entregue" };

        public KDSLogic(
            ItemComandaService itemComandaService,
            ProducaoLogic producaoLogic)
        {
            _itemComandaService = itemComandaService;
            _producaoLogic = producaoLogic;
        }

        public async Task<List<MItemComanda>> ObterItensAtivosKDSAsync(string setorFiltro = "Todos")
        {
            List<MItemComanda> todosItens;
            try
            {
                todosItens = await _itemComandaService.GetAll() ?? new List<MItemComanda>();
            }
            catch
            {
                todosItens = new List<MItemComanda>();
            }

            var query = todosItens.Where(item =>
                !string.IsNullOrWhiteSpace(item.Status) &&
                (item.Status.Equals("Confirmado", StringComparison.OrdinalIgnoreCase) ||
                 item.Status.Equals("Pendente", StringComparison.OrdinalIgnoreCase) ||
                 item.Status.Equals("Em Produção", StringComparison.OrdinalIgnoreCase) ||
                 item.Status.Equals("Em Preparo", StringComparison.OrdinalIgnoreCase))
            );

            if (!string.IsNullOrWhiteSpace(setorFiltro) && !setorFiltro.Equals("Todos", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(item => IdentificarSetorItem(item.NomeProduto).Equals(setorFiltro, StringComparison.OrdinalIgnoreCase));
            }

            return query.OrderBy(x => x.Id).ToList();
        }

        public static string IdentificarSetorItem(string? nomeProduto)
        {
            if (string.IsNullOrWhiteSpace(nomeProduto)) return "Cozinha";
            string nome = nomeProduto.ToLowerInvariant();

            if (nome.Contains("chopp") || nome.Contains("suco") || nome.Contains("refrigerante") || nome.Contains("cerveja") || nome.Contains("água") || nome.Contains("drink") || nome.Contains("bebida") || nome.Contains("gin") || nome.Contains("vodka") || nome.Contains("caipirinha"))
                return "Bar";

            if (nome.Contains("burger") || nome.Contains("hambúrguer") || nome.Contains("smash") || nome.Contains("misto") || nome.Contains("lanche") || nome.Contains("bacon"))
                return "Chapa";

            return "Cozinha";
        }

        public async Task<(bool Sucesso, string Mensagem, string? NovoStatus)> AvancarStatusItemAsync(int itemId)
        {
            var item = await _itemComandaService.GetById(itemId);

            if (item == null)
                return (false, "Item não encontrado.", null);

            if (item.Status == "Cancelado")
                return (false, "Item cancelado não pode ser avançado.", null);

            if (item.Status == "Entregue")
                return (false, "Item já foi entregue. Ciclo encerrado.", "Entregue");

            int indiceAtual = Array.IndexOf(CicloStatus, item.Status);

            if (item.Status == "Confirmado" || indiceAtual == -1)
            {
                item.Status = "Pendente";
                indiceAtual = 0;
            }

            int proximoIndice = Math.Min(indiceAtual + 1, CicloStatus.Length - 1);
            string novoStatus = CicloStatus[proximoIndice];

            if (novoStatus == "Saída para Entrega" && item.Status == "Em Produção")
            {
                var prodResult = await _producaoLogic.ProcessarProducaoAsync(item.ProdutoId, item.Quantidade, "1");
                if (!prodResult.Sucesso)
                {
                    return (false, $"Erro na produção: {prodResult.Mensagem}", item.Status);
                }

                var adicResult = await _producaoLogic.ProcessarAdicionaisItemAsync(item.Id.ToString(), "1");
                if (!adicResult.Sucesso)
                {
                    return (false, $"Erro na baixa de adicionais: {adicResult.Mensagem}", item.Status);
                }
            }

            string logEntry = $"[{DateTime.UtcNow:dd/MM HH:mm}] {item.Status} → {novoStatus}";
            item.Observacao = string.IsNullOrEmpty(item.Observacao)
                ? logEntry
                : $"{item.Observacao} | {logEntry}";

            item.Status = novoStatus;

            bool salvo = await _itemComandaService.SaveAsync(item) > 0;
            if (salvo)
            {
                OnKdsAtualizado?.Invoke();
            }

            return salvo
                ? (true, $"Status avançado para '{novoStatus}'.", novoStatus)
                : (false, "Erro ao salvar o status do item.", null);
        }

        public static string ObterLabelBotaoAcao(string statusAtual)
        {
            return statusAtual switch
            {
                "Confirmado" => "Iniciar Preparo",
                "Pendente" => "Iniciar Preparo",
                "Em Produção" => "Avisar Garçom (Pronto)",
                "Saída para Entrega" => "Entregar à Mesa",
                "Entregue" => "Finalizado",
                _ => "Avançar"
            };
        }

        public static string ObterClasseBadgeStatus(string status)
        {
            return status switch
            {
                "Confirmado" => "bg-warning text-dark",
                "Pendente" => "bg-warning text-dark",
                "Em Produção" => "bg-primary text-light",
                "Saída para Entrega" => "bg-success text-light",
                "Entregue" => "bg-secondary text-light",
                "Cancelado" => "bg-danger text-light",
                _ => "bg-secondary text-light"
            };
        }
    }
}
