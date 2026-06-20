using JT_Aplicacao.Classes;
using JT_UserInterface.Shared.Services.Comum;
using JT_Aplicacao;
using JT_Atributos;
using System.ComponentModel.DataAnnotations;

namespace JT_Food.Shared.Models
{
    [NomeTabela("food_mesas")]
    public class MMesa : ClasseBase
    {
        [NomeColuna("UserId")]
        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "O número ou nome da mesa é obrigatório.")]
        [NomeColuna("Identificacao")]
        public string Identificacao { get; set; } = string.Empty;

        [NomeColuna("Capacidade")]
        public int Capacidade { get; set; } = 4;

        // Status: use StatusMesa constants below
        [NomeColuna("Status")]
        public string Status { get; set; } = StatusMesa.Livre;

        [NomeColuna("DataCadastro")]
        public DateTime DataCadastro { get; set; } = DateTime.UtcNow;

        public override List<Filtro> CarregarFiltros() => [];
    }

    /// <summary>
    /// Constantes de status para MMesa. Mudanças automáticas:
    /// Livre → Ocupada (ao abrir comanda) → AguardandoPagamento (ao solicitar fechamento) → Livre (quitação).
    /// </summary>
    public static class StatusMesa
    {
        public const string Livre = "Livre";
        public const string Ocupada = "Ocupada";
        public const string AguardandoPagamento = "AguardandoPagamento";
        public const string Reservada = "Reservada";
    }
}

