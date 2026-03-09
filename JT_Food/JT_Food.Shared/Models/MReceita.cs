using JT_Aplicacao;
using JT_Atributos;
using System.ComponentModel.DataAnnotations;

namespace JT_Food.Shared.Models
{
    [NomeTabela("food_receitas")]
    public class MReceita : ClasseBase
    {
        [NomeColuna("UserId")]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [NomeColuna("ProdutoId")]
        public string ProdutoId { get; set; } = string.Empty;

        [Required]
        [NomeColuna("InsumoId")]
        public string InsumoId { get; set; } = string.Empty;
        
        [NomeColuna("NomeInsumo")]
        public string? NomeInsumo { get; set; }

        [Required]
        [NomeColuna("Quantidade")]
        public double Quantidade { get; set; } = 0.0;

        [NomeColuna("UnidadeMedida")]
        public string UnidadeMedida { get; set; } = "kg";

        public override List<Filtro> CarregarFiltros() => [];
    }
}
