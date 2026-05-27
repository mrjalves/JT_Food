using JT_Aplicacao;
using JT_Atributos;
using System.ComponentModel.DataAnnotations;

namespace JT_Food.Shared.Models
{
    [NomeTabela("food_itens_comanda")]
    public class MItemComanda : ClasseBase
    {


        [Required]
        [NomeColuna("ComandaId")]
        public string ComandaId { get; set; } = string.Empty;

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

        [NomeColuna("TotalItem")]
        public double TotalItem { get; set; } = 0.0;
        
        // Status: "Confirmado", "Em Produção", "Pronto", "Saída para Entrega", "Entregue", "Cancelado"
        [NomeColuna("Status")]
        public string Status { get; set; } = "Confirmado";

        [NomeColuna("Observacao")]
        public string? Observacao { get; set; }

        public override List<Filtro> CarregarFiltros() => [];
    }
}
