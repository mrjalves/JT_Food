using System.Collections.Generic;
using JT_Food.Shared.Domain.Enums;

namespace JT_Food.Shared.Domain.Entities
{
    public class ProdutoCardapio
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public int CategoriaId { get; set; }
        public CategoriaCardapioEnum Grupo { get; set; } = CategoriaCardapioEnum.Pratos;
        public double PrecoBase { get; set; } = 0.0;
        public double? PrecoPromocional { get; set; }
        public bool EmPromocao { get; set; } = false;
        public int? ImpressoraDestinoId { get; set; }
        public int? ReceitaId { get; set; }
        public string? FotoUrl { get; set; }
        public bool Disponivel { get; set; } = true;
        public int TempoMedioPreparoMinutos { get; set; } = 15;
        
        public List<string> ObservacoesSugeridas { get; set; } = new()
        {
            "Sem cebola",
            "Bem passado",
            "Ao ponto",
            "Gelo e limão",
            "Copos separados",
            "Sem gelo"
        };
    }
}
