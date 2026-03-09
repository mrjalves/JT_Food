using JT_Aplicacao;
using JT_Atributos;
using System.ComponentModel.DataAnnotations;

namespace JT_Food.Shared.Models
{
    [NomeTabela("food_caixa")]
    public class MCaixa : ClasseBase
    {
        [NomeColuna("UserId")]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [NomeColuna("OperadorAberturaId")]
        public string OperadorAberturaId { get; set; } = string.Empty;

        [NomeColuna("OperadorFechamentoId")]
        public string? OperadorFechamentoId { get; set; }

        [Required]
        [NomeColuna("DataAbertura")]
        public DateTime DataAbertura { get; set; } = DateTime.UtcNow;

        [NomeColuna("DataFechamento")]
        public DateTime? DataFechamento { get; set; }

        [NomeColuna("SaldoInicial")]
        public double SaldoInicial { get; set; } = 0.0;

        [NomeColuna("SaldoFinalInformado")]
        public double SaldoFinalInformado { get; set; } = 0.0;

        [NomeColuna("SaldoFinalCalculado")]
        public double SaldoFinalCalculado { get; set; } = 0.0;

        // "Aberto", "Fechado"
        [NomeColuna("Status")]
        public string Status { get; set; } = "Aberto";

        [NomeColuna("Observacao")]
        public string? Observacao { get; set; }

        public override List<Filtro> CarregarFiltros() => [];
    }
}
