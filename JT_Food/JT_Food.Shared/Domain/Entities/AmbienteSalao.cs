using System;

namespace JT_Food.Shared.Domain.Entities
{
    public class AmbienteSalao
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public int Ordem { get; set; } = 1;
        public bool Ativo { get; set; } = true;
        public int LarguraCanvas { get; set; } = 1000;
        public int AlturaCanvas { get; set; } = 650;
        public string CorFundo { get; set; } = "#f8f9fc";
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    }
}
