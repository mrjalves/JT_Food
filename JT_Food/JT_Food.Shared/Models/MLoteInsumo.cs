using JT_Aplicacao;
using JT_Atributos;
using System.ComponentModel.DataAnnotations;

namespace JT_Food.Shared.Models
{
    [NomeTabela("food_lote_insumo")]
    public class MLoteInsumo : ClasseBase
    {
        [NomeColuna("UserId")]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [NomeColuna("InsumoId")]
        public string InsumoId { get; set; } = string.Empty;

        [NomeColuna("NomeInsumo")]
        public string? NomeInsumo { get; set; }

        [Required]
        [NomeColuna("QuantidadeInicial")]
        public double QuantidadeInicial { get; set; } = 0.0;

        [Required]
        [NomeColuna("QuantidadeDisponivel")]
        public double QuantidadeDisponivel { get; set; } = 0.0;

        [Required]
        [NomeColuna("CustoTotalLote")]
        public double CustoTotalLote { get; set; } = 0.0;

        [NomeColuna("CustoUnitario")]
        public double CustoUnitario { get; set; } = 0.0;

        [NomeColuna("DataEntrada")]
        public DateTime DataEntrada { get; set; } = DateTime.UtcNow;

        [NomeColuna("DataValidade")]
        public DateTime? DataValidade { get; set; }

        // Aberto, Finalizado
        [NomeColuna("Status")]
        public string Status { get; set; } = "Aberto";

        public override List<Filtro> CarregarFiltros() => [];
    }
}
