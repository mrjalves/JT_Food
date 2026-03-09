using JT_Aplicacao;
using JT_Atributos;
using System.ComponentModel.DataAnnotations;

namespace JT_Food.Shared.Models
{
    [NomeTabela("food_memoria_calculo")]
    public class MMemoriaCalculo : ClasseBase
    {
        [NomeColuna("UserId")]
        public string UserId { get; set; } = string.Empty;

        // ComandaId ou ID de Produção
        [NomeColuna("ReferenciaId")]
        public string ReferenciaId { get; set; } = string.Empty;

        // Target produced (e.g. ItemComandaId or ProdutoId)
        [NomeColuna("ItemId")]
        public string ItemId { get; set; } = string.Empty;

        // Consumed Raw Material
        [NomeColuna("LoteInsumoId")]
        public string LoteInsumoId { get; set; } = string.Empty;

        [NomeColuna("InsumoId")]
        public string InsumoId { get; set; } = string.Empty;

        [NomeColuna("QuantidadeConsumida")]
        public double QuantidadeConsumida { get; set; } = 0.0;

        [NomeColuna("CustoUnitarioLote")]
        public double CustoUnitarioLote { get; set; } = 0.0;

        [NomeColuna("CustoTotalFracao")]
        public double CustoTotalFracao { get; set; } = 0.0;

        [NomeColuna("DataProcessamento")]
        public DateTime DataProcessamento { get; set; } = DateTime.UtcNow;

        [NomeColuna("Descricao")]
        public string? Descricao { get; set; }

        public override List<Filtro> CarregarFiltros() => [];
    }
}
