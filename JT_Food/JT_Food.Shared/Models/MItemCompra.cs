using JT_Aplicacao.Classes;
using JT_UserInterface.Shared.Services.Comum;
using JT_Aplicacao;
using JT_Atributos;
using System.ComponentModel.DataAnnotations;

namespace JT_Food.Shared.Models
{
    [NomeTabela("food_itens_compra")]
    public class MItemCompra : ClasseBase
    {
        [Required]
        [NomeColuna("CompraId")]
        public string CompraId { get; set; } = string.Empty;

        [Required]
        [NomeColuna("InsumoId")]
        public string InsumoId { get; set; } = string.Empty;

        [NomeColuna("NomeInsumo")]
        public string? NomeInsumo { get; set; }

        [Required]
        [NomeColuna("Quantidade")]
        public double Quantidade { get; set; } = 0.0;

        [Required]
        [NomeColuna("ValorUnitario")]
        public double ValorUnitario { get; set; } = 0.0;

        [NomeColuna("ValorTotal")]
        public double ValorTotal { get; set; } = 0.0;

        public override List<Filtro> CarregarFiltros() => [];
    }
}

