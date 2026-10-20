using JT_Food.Shared.Domain.Enums;

namespace JT_Food.Shared.Domain.Entities
{
    public class CategoriaMenu
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public CategoriaCardapioEnum Grupo { get; set; } = CategoriaCardapioEnum.Pratos;
        public string Icone { get; set; } = "bi-tag";
        public int? ImpressoraDestinoId { get; set; }
        public int Ordem { get; set; } = 1;
        public bool Ativo { get; set; } = true;
    }
}
