using JT_Aplicacao;
using JT_Atributos;
using System.ComponentModel.DataAnnotations;

namespace JT_Food.Shared.Models
{
    [NomeTabela("food_producao")]
    public class MProducao : ClasseBase
    {
        [NomeColuna("UserId")]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [NomeColuna("ProdutoId")]
        public string ProdutoId { get; set; } = string.Empty;

        [NomeColuna("NomeProduto")]
        public string? NomeProduto { get; set; }

        [Required]
        [NomeColuna("QuantidadeProduzida")]
        public double QuantidadeProduzida { get; set; } = 1.0;

        [NomeColuna("DataProducao")]
        public DateTime DataProducao { get; set; } = DateTime.UtcNow;

        [NomeColuna("ResponsavelId")]
        public string? ResponsavelId { get; set; }

        [NomeColuna("CustoTotalCalculado")]
        public double CustoTotalCalculado { get; set; } = 0.0;

        [NomeColuna("CustoUnitarioSugerido")]
        public double CustoUnitarioSugerido { get; set; } = 0.0;

        [NomeColuna("PrecoVendaTotal")]
        public double PrecoVendaTotal { get; set; } = 0.0;

        [NomeColuna("LucroLote")]
        public double LucroLote { get; set; } = 0.0;

        [NomeColuna("Observacoes")]
        public string? Observacoes { get; set; }

        public override List<Filtro> CarregarFiltros() => [];
    }
}
