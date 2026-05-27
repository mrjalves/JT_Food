using JT_Aplicacao;
using JT_Atributos;
using System.ComponentModel.DataAnnotations;

namespace JT_Food.Shared.Models
{
    [NomeTabela("food_compras")]
    public class MCompra : ClasseBase
    {
        [NomeColuna("UserId")]
        public string UserId { get; set; } = string.Empty;

        [NomeColuna("Fornecedor")]
        public string Fornecedor { get; set; } = string.Empty;

        [NomeColuna("DataCompra")]
        public DateTime DataCompra { get; set; } = DateTime.UtcNow;

        [NomeColuna("ValorTotal")]
        public double ValorTotal { get; set; } = 0.0;

        [NomeColuna("Status")]
        public string Status { get; set; } = "Pendente"; // Pendente, Finalizada, Cancelada

        [NomeColuna("Observacao")]
        public string? Observacao { get; set; }

        public override List<Filtro> CarregarFiltros() => [];
    }
}
