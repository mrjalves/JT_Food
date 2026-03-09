using JT_Food.Shared.Models;
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
            "Pendente",
            "EmPreparo",
            "Pronto",
            "Entregue"
        ];

        /// <summary>
        /// Retorna todos os itens ativos no KDS (Pendente e EmPreparo), ordenados por data de lançamento.
        /// </summary>
        public static List<MItemComanda> ObterItensAtivosKDS()
        {
            var itens = ItemComandaService.Find(
                "Status IN ('Pendente', 'EmPreparo') ORDER BY Id ASC");
            return itens ?? new List<MItemComanda>();
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

            // Registrar transição no campo Observacao (log leve, sem nova coluna)
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
                "Pendente" => "▶ Iniciar Preparo",
                "EmPreparo" => "✓ Marcar como Pronto",
                "Pronto" => "🚀 Confirmar Entrega",
                _ => "Avançar"
            };
        }

        /// <summary>
        /// Retorna a classe CSS Bootstrap para o badge de status do item.
        /// </summary>
        public static string ObterClasseBadgeStatus(string status)
        {
            return status switch
            {
                "Pendente" => "bg-warning text-dark",
                "EmPreparo" => "bg-primary",
                "Pronto" => "bg-success",
                "Entregue" => "bg-secondary",
                "Cancelado" => "bg-danger",
                _ => "bg-light text-dark"
            };
        }
    }
}
