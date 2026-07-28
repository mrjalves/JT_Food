using JT_Classes.Restaurante;
using JT_Food.Shared.Services;

namespace JT_Food.Shared.BusinessLogic
{
    /// <summary>
    /// Lógica de negócio do KDS (Kitchen Display System).
    /// Gerencia o ciclo de vida de preparo dos itens da comanda.
    /// Ciclo: Pendente → EmPreparo → Pronto → Entregue
    /// </summary>
    public static class KDSLogic
    {
        // Sequência definida de status de preparo
        public static readonly string[] CicloStatus =
        [
            "Confirmado",
            "Em Produção",
            "Saída para Entrega",
            "Entregue"
        ];

        /// <summary>
        /// Retorna todos os itens ativos no KDS (Pendente e EmPreparo), ordenados por data de lançamento.
        /// </summary>
        public static List<MItemComanda> ObterItensAtivosKDS()
        {
            var pendentes = ItemComandaService.Find(new Dictionary<string, object> { { "Status", "Confirmado" } });
            var emPreparo = ItemComandaService.Find(new Dictionary<string, object> { { "Status", "Em Produção" } });

            var todos = new List<MItemComanda>();
            if (pendentes != null) todos.AddRange(pendentes);
            if (emPreparo != null) todos.AddRange(emPreparo);

            // Ordenação manual em memória já que ORDER BY não é suportado via dicionário no BaseRepository
            return todos.OrderBy(x => x.Id).ToList();
        }

        /// <summary>
        /// Avança o status de preparo do item para o próximo na sequência do ciclo KDS.
        /// </summary>
        public static (bool Sucesso, string Mensagem, string? NovoStatus) AvancarStatusItem(int itemId)
        {
            var item = ItemComandaService.GetById(itemId);
            if (item == null)
                return (false, "Item não encontrado.", null);

            if (item.Status == "Cancelado")
                return (false, "Item cancelado não pode ser avançado.", null);

            if (item.Status == "Entregue")
                return (false, "Item já foi entregue. Ciclo encerrado.", "Entregue");

            int indiceAtual = Array.IndexOf(CicloStatus, item.Status);
            if (indiceAtual < 0)
            {
                // Status desconhecido, reinicia como Pendente
                item.Status = "Pendente";
                indiceAtual = 0;
            }

            int proximoIndice = Math.Min(indiceAtual + 1, CicloStatus.Length - 1);
            string novoStatus = CicloStatus[proximoIndice];

            // 1. Se estiver indo para "Saída para Entrega", finaliza a produção e dá baixa no estoque
            if (novoStatus == "Saída para Entrega" && item.Status == "Em Produção")
            {
                var prodResult = ProducaoLogic.ProcessarProducao(item.ProdutoId, item.Quantidade, "1");
                if (!prodResult.Sucesso)
                {
                    return (false, $"Erro na produção: {prodResult.Mensagem}", item.Status);
                }

                // Também processa adicionais se houverem
                ProducaoLogic.ProcessarAdicionaisItem(item.Id.ToString(), "1");
            }

            // Registrar transição no campo Observacao
            string logEntry = $"[{DateTime.UtcNow:dd/MM HH:mm}] {item.Status} → {novoStatus}";
            item.Observacao = string.IsNullOrEmpty(item.Observacao)
                ? logEntry
                : $"{item.Observacao} | {logEntry}";

            item.Status = novoStatus;

            bool salvo = ItemComandaService.Save(item);
            return salvo
                ? (true, $"Status avançado para '{novoStatus}'.", novoStatus)
                : (false, "Erro ao salvar o status do item.", null);
        }

        /// <summary>
        /// Retorna o nome do botão de ação para o status atual do item.
        /// </summary>
        public static string ObterLabelBotaoAcao(string statusAtual)
        {
            return statusAtual switch
            {
                "Confirmado" => "▶ Iniciar Produção",
                "Em Produção" => "✓ Finalizar Produção",
                "Saída para Entrega" => "🚀 Confirmar Entrega",
                _ => "Avançar"
            };
        }

        public static string ObterClasseBadgeStatus(string status)
        {
            return status switch
            {
                "Confirmado" => "bg-warning text-dark",
                "Em Produção" => "bg-primary",
                "Saída para Entrega" => "bg-success",
                "Entregue" => "bg-secondary",
                "Cancelado" => "bg-danger",
                _ => "bg-light text-dark"
            };
        }
    }
}




