using System;
using System.Threading.Tasks;
using JT_Food.Shared.Domain.Entities;
using JT_Food.Shared.Domain.Enums;

namespace JT_Food.Shared.Services.Interfaces
{
    public interface IAuthPerfilService
    {
        Task<UsuarioOperador> ObterUsuarioLogadoAsync();
        Task<(bool Sucesso, string Mensagem, UsuarioOperador? Usuario)> LoginPorPinAsync(string pin);
        Task<(bool Sucesso, string Mensagem, UsuarioOperador? Usuario)> LoginCredenciaisAsync(string email, string senha);
        Task LogoutAsync();
        PerfilAcesso ObterPerfilAtual();
        bool IsGerente();
        bool IsGarcom();
        bool IsCaixa();
        
        event Action OnSessaoAlterada;
    }
}
