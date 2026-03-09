using JT_Aplicacao;
using JT_Atributos;
using System.ComponentModel.DataAnnotations;

namespace JT_Food.Shared.Models
{
    [NomeTabela("food_usuarios")]
    public class MUsuario : ClasseBase
    {
        [NomeColuna("UserId")]
        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "O nome é obrigatório.")]
        [NomeColuna("Nome")]
        public string Nome { get; set; } = string.Empty;

        // Tipo: "Garçom", "Caixa", "Admin"
        [Required(ErrorMessage = "O tipo de usuário é obrigatório.")]
        [NomeColuna("TipoUsuario")]
        public string TipoUsuario { get; set; } = "Garçom";

        [NomeColuna("Ativo")]
        public bool Ativo { get; set; } = true;

        [NomeColuna("DataCadastro")]
        public DateTime DataCadastro { get; set; } = DateTime.UtcNow;

        public override List<Filtro> CarregarFiltros() => [];
    }
}
