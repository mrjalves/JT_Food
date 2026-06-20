using JT_Aplicacao.Classes;
using JT_UserInterface.Shared.Services.Comum;
using JT_Aplicacao;
using JT_Atributos;
using System.ComponentModel.DataAnnotations;

namespace JT_Food.Shared.Models
{
    [NomeTabela("food_insumos")]
    public class MInsumo : ClasseBase
    {
        [NomeColuna("UserId")]
        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "O nome é obrigatório.")]
        [NomeColuna("Nome")]
        public string Nome { get; set; } = string.Empty;

        [NomeColuna("UnidadeMedida")]
        public string UnidadeMedida { get; set; } = "kg";

        [NomeColuna("Descricao")]
        public string? Descricao { get; set; }

        [NomeColuna("EstoqueMinimo")]
        public double EstoqueMinimo { get; set; } = 0.0;
        
        [NomeColuna("CustoMedio")]
        public double CustoMedio { get; set; } = 0.0;

        [NomeColuna("DataCadastro")]
        public DateTime DataCadastro { get; set; } = DateTime.UtcNow;

        public override List<Filtro> CarregarFiltros() => [];
    }
}

