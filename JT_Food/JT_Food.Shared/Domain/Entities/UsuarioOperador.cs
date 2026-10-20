using JT_Food.Shared.Domain.Enums;

namespace JT_Food.Shared.Domain.Entities
{
    public class UsuarioOperador
    {
        public string Id { get; set; } = "1";
        public string Nome { get; set; } = "Operador Padrão";
        public string PinRapido { get; set; } = "1234";
        public PerfilAcesso Perfil { get; set; } = PerfilAcesso.Gerente;
        public string Email { get; set; } = "admin@jtfood.com";
        public bool Ativo { get; set; } = true;
    }
}
