using JT_Aplicacao.Classes;
using JT_UserInterface.Shared.Services.Comum;
using JT_Aplicacao;
using JT_Atributos;
using System.ComponentModel.DataAnnotations;

namespace JT_Food.Shared.Models
{
    [NomeTabela("food_estoque")]
    public class MEstoque : ClasseBase
    {
        [NomeColuna("UserId")]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [NomeColuna("InsumoId")]
        public string InsumoId { get; set; } = string.Empty;

        [Required]
        [NomeColuna("QuantidadeAtual")]
        public double QuantidadeAtual { get; set; } = 0.0;

        [NomeColuna("DataUltimaAtualizacao")]
        public DateTime DataUltimaAtualizacao { get; set; } = DateTime.UtcNow;

        [NomeColuna("UsuarioUltimaAtualizacao")]
        public string? UsuarioUltimaAtualizacao { get; set; }

        public override List<Filtro> CarregarFiltros() => [];
    }
}

