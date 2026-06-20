using JT_Aplicacao.Classes;
using JT_UserInterface.Shared.Services.Comum;
using JT_Aplicacao;
using JT_Atributos;
using System.ComponentModel.DataAnnotations;

namespace JT_Food.Shared.Models
{
    [NomeTabela("food_produtos")]
    public class MProduto : ClasseBase
    {
        [NomeColuna("UserId")]
        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "O nome é obrigatório.")]
        [NomeColuna("Nome")]
        public string Nome { get; set; } = string.Empty;

        [NomeColuna("Descricao")]
        public string? Descricao { get; set; }

        [NomeColuna("Categoria")]
        public string? Categoria { get; set; }

        [Required]
        [NomeColuna("Preco")]
        public double Preco { get; set; } = 0.0;

        // Tipo: "Prato", "Bebida", "Sobremesa", "Porção"
        [NomeColuna("Tipo")]
        public string Tipo { get; set; } = "Prato";

        [NomeColuna("CustoInformado")]
        public double? CustoInformado { get; set; }
        
        [NomeColuna("PossuiReceita")]
        public bool PossuiReceita { get; set; } = false;

        [NomeColuna("FotoUrl")]
        public string? FotoUrl { get; set; }

        [NomeColuna("DataCadastro")]
        public DateTime DataCadastro { get; set; } = DateTime.UtcNow;

        public override List<Filtro> CarregarFiltros() => [];
    }
}

