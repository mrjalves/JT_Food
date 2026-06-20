using JT_Aplicacao.Classes;
using JT_UserInterface.Shared.Services.Comum;
using JT_Aplicacao;
using JT_Atributos;
using System.ComponentModel.DataAnnotations;

namespace JT_Food.Shared.Models
{
    /// <summary>
    /// Representa um adicional vinculado a um item da comanda.
    /// Ex: "Hambúrguer Extra", "Adicional de Bacon", "Queijo Extra".
    /// </summary>
    [NomeTabela("food_adicionais_item_comanda")]
    public class MAdicionaisItemComanda : ClasseBase
    {
        [NomeColuna("UserId")]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [NomeColuna("ItemComandaId")]
        public string ItemComandaId { get; set; } = string.Empty;

        [Required]
        [NomeColuna("ProdutoId")]
        public string ProdutoId { get; set; } = string.Empty;

        [NomeColuna("NomeProduto")]
        public string? NomeProduto { get; set; }

        [Required]
        [NomeColuna("Quantidade")]
        public double Quantidade { get; set; } = 1;

        [Required]
        [NomeColuna("PrecoUnitario")]
        public double PrecoUnitario { get; set; } = 0.0;

        [NomeColuna("TotalAdicional")]
        public double TotalAdicional { get; set; } = 0.0;

        [NomeColuna("DataLancamento")]
        public DateTime DataLancamento { get; set; } = DateTime.UtcNow;

        public override List<Filtro> CarregarFiltros() => [];
    }
}

