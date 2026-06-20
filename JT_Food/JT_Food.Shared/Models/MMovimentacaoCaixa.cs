using JT_Aplicacao.Classes;
using JT_UserInterface.Shared.Services.Comum;
using JT_Aplicacao;
using JT_Atributos;
using System.ComponentModel.DataAnnotations;

namespace JT_Food.Shared.Models
{
    [NomeTabela("food_movimentacao_caixa")]
    public class MMovimentacaoCaixa : ClasseBase
    {
        [NomeColuna("UserId")]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [NomeColuna("CaixaId")]
        public string CaixaId { get; set; } = string.Empty;

        [Required]
        [NomeColuna("OperadorId")]
        public string OperadorId { get; set; } = string.Empty;

        [Required]
        [NomeColuna("DataHora")]
        public DateTime DataHora { get; set; } = DateTime.UtcNow;

        // "Abertura", "Fechamento", "Sangria", "Reforço", "Venda", "PagamentoForncedor"
        [Required]
        [NomeColuna("TipoMovimentacao")]
        public string TipoMovimentacao { get; set; } = string.Empty;

        // "Entrada" (C), "Saida" (D)
        [NomeColuna("Natureza")]
        public string Natureza { get; set; } = "Entrada";

        [Required]
        [NomeColuna("Valor")]
        public double Valor { get; set; } = 0.0;

        [NomeColuna("FormaPagamento")]
        public string FormaPagamento { get; set; } = "Dinheiro"; // Pix, CartaoCredito, CartaoDebito, Transferencia

        // Link with sale
        [NomeColuna("ComandaId")]
        public string? ComandaId { get; set; }

        [NomeColuna("Descricao")]
        public string? Descricao { get; set; }

        public override List<Filtro> CarregarFiltros() => [];
    }
}

