using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using JT_Classes.Acessos;
using JT_Food.Shared.Domain.Entities;
using JT_Food.Shared.Domain.Enums;
using JT_Food.Shared.Services.Interfaces;

namespace JT_Food.Shared.Services.Implementations
{
    public class AuthPerfilService : IAuthPerfilService
    {
        private readonly ISecureStorageService _storage;
        private const string STORAGE_KEY = "JT_FOOD_AUTH_OPERADOR";

        private UsuarioOperador _usuarioAtual;
        public event Action? OnSessaoAlterada;

        private readonly List<UsuarioOperador> _usuariosPreConfigurados = new()
        {
            new UsuarioOperador
            {
                Id = "1",
                Nome = "Carlos Gerente",
                PinRapido = "1234",
                Perfil = PerfilAcesso.Gerente,
                Email = "gerente@jtfood.com",
                Ativo = true
            },
            new UsuarioOperador
            {
                Id = "2",
                Nome = "Lucas Garçom",
                PinRapido = "2222",
                Perfil = PerfilAcesso.Garcom,
                Email = "garcom@jtfood.com",
                Ativo = true
            },
            new UsuarioOperador
            {
                Id = "3",
                Nome = "Mariana Caixa",
                PinRapido = "3333",
                Perfil = PerfilAcesso.Caixa,
                Email = "caixa@jtfood.com",
                Ativo = true
            }
        };

        public AuthPerfilService(ISecureStorageService storage)
        {
            _storage = storage;
            // Padrão inicial: Gerente (para facilidade de navegação inicial)
            _usuarioAtual = _usuariosPreConfigurados.First();
        }

        public async Task<UsuarioOperador> ObterUsuarioLogadoAsync()
        {
            try
            {
                var json = await _storage.GetTokenAsync(STORAGE_KEY);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    var user = JsonSerializer.Deserialize<UsuarioOperador>(json);
                    if (user != null)
                    {
                        _usuarioAtual = user;
                    }
                }
            }
            catch
            {
                // Fallback silencioso
            }

            return _usuarioAtual;
        }

        public async Task<(bool Sucesso, string Mensagem, UsuarioOperador? Usuario)> LoginPorPinAsync(string pin)
        {
            if (string.IsNullOrWhiteSpace(pin) || pin.Length < 4)
            {
                return (false, "Informe o PIN numérico de 4 dígitos.", null);
            }

            var user = _usuariosPreConfigurados.FirstOrDefault(u => u.PinRapido == pin && u.Ativo);
            if (user == null)
            {
                return (false, "PIN incorreto ou operador inativo.", null);
            }

            _usuarioAtual = user;
            await SalvarSessaoAsync(user);
            OnSessaoAlterada?.Invoke();

            return (true, $"Bem-vindo, {user.Nome} ({user.Perfil})!", user);
        }

        public async Task<(bool Sucesso, string Mensagem, UsuarioOperador? Usuario)> LoginCredenciaisAsync(string email, string senha)
        {
            var user = _usuariosPreConfigurados.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
            if (user == null || senha != "123456")
            {
                return (false, "Email ou senha incorretos.", null);
            }

            _usuarioAtual = user;
            await SalvarSessaoAsync(user);
            OnSessaoAlterada?.Invoke();

            return (true, $"Bem-vindo, {user.Nome}!", user);
        }

        public async Task SetOperadorLogadoAsync(Usuario usuario, string? senha = null)
        {
            if (usuario == null) return;

            PerfilAcesso perfil = PerfilAcesso.Caixa;
            var roleLower = (usuario.Role ?? string.Empty).ToLowerInvariant();
            if (roleLower.Contains("gerente") || roleLower.Contains("supervisor") || roleLower.Contains("admin"))
            {
                perfil = PerfilAcesso.Gerente;
            }
            else if (roleLower.Contains("garcom") || roleLower.Contains("garçom") || roleLower.Contains("atendente"))
            {
                perfil = PerfilAcesso.Garcom;
            }
            else if (roleLower.Contains("cozinha") || roleLower.Contains("producao") || roleLower.Contains("produção"))
            {
                perfil = PerfilAcesso.Cozinha;
            }

            var op = new UsuarioOperador
            {
                Id = usuario.Id.ToString(),
                Nome = !string.IsNullOrWhiteSpace(usuario.Nome) ? usuario.Nome : usuario.Login,
                Email = !string.IsNullOrWhiteSpace(usuario.Email) ? usuario.Email : $"{usuario.Login}@jtfood.com",
                Perfil = perfil,
                Ativo = usuario.Ativo,
                PinRapido = "1234"
            };

            _usuarioAtual = op;
            await SalvarSessaoAsync(op);
            OnSessaoAlterada?.Invoke();
        }

        public async Task LogoutAsync()
        {
            // Retorna para Garçom padrão ou desloga
            _storage.RemoveToken(STORAGE_KEY);
            _usuarioAtual = _usuariosPreConfigurados.First(u => u.Perfil == PerfilAcesso.Garcom);
            OnSessaoAlterada?.Invoke();
            await Task.CompletedTask;
        }

        public PerfilAcesso ObterPerfilAtual() => _usuarioAtual.Perfil;

        public bool IsGerente() => _usuarioAtual.Perfil == PerfilAcesso.Gerente;

        public bool IsGarcom() => _usuarioAtual.Perfil == PerfilAcesso.Garcom;

        public bool IsCaixa() => _usuarioAtual.Perfil == PerfilAcesso.Caixa;

        private async Task SalvarSessaoAsync(UsuarioOperador user)
        {
            try
            {
                var json = JsonSerializer.Serialize(user);
                await _storage.SetTokenAsync(STORAGE_KEY, json);
            }
            catch
            {
                // Fallback
            }
        }
    }
}
