using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using JT_Classes.Acessos;
using JT_UserInterface.Shared.Services.Interfaces.Acessos;

namespace JT_Food.Shared.Mocks
{
    /// <summary>
    /// Fornece operadores e usuários simulados para testes de abertura de caixa,
    /// turnos de PDV e rotinas de atendimento, com suporte a criação automática e autenticação.
    /// </summary>
    public static class UsuarioMockProvider
    {
        public const string SenhaPadrao = "123456";

        private static readonly List<Usuario> _usuariosMocks = new()
        {
            new Usuario
            {
                Id = 1,
                Nome = "Operador Geral (PDV 01)",
                Login = "operador01",
                Role = "Operador de Caixa",
                Email = "operador01@jtfood.com",
                Senha = SenhaPadrao,
                Ativo = true
            },
            new Usuario
            {
                Id = 2,
                Nome = "Julio Alves (Supervisor Geral)",
                Login = "supervisor",
                Role = "Supervisor",
                Email = "supervisor@jtfood.com",
                Senha = SenhaPadrao,
                Ativo = true
            },
            new Usuario
            {
                Id = 3,
                Nome = "Mariana Silva (Caixa Manhã)",
                Login = "mariana.caixa",
                Role = "Operador de Caixa",
                Email = "mariana@jtfood.com",
                Senha = SenhaPadrao,
                Ativo = true
            },
            new Usuario
            {
                Id = 4,
                Nome = "Lucas Santos (Caixa Noite)",
                Login = "lucas.caixa",
                Role = "Operador de Caixa",
                Email = "lucas@jtfood.com",
                Senha = SenhaPadrao,
                Ativo = true
            },
            new Usuario
            {
                Id = 5,
                Nome = "Carlos Eduardo (Gerente Operacional)",
                Login = "carlos.gerente",
                Role = "Gerente",
                Email = "gerente@jtfood.com",
                Senha = SenhaPadrao,
                Ativo = true
            },
            new Usuario
            {
                Id = 6,
                Nome = "Ana Paula (Atendente / Balcão)",
                Login = "anapaula",
                Role = "Atendente",
                Email = "anapaula@jtfood.com",
                Senha = SenhaPadrao,
                Ativo = true
            },
            new Usuario
            {
                Id = 7,
                Nome = "Roberto Lima (Fechamento / Auditor)",
                Login = "roberto.auditor",
                Role = "Auditor",
                Email = "auditor@jtfood.com",
                Senha = SenhaPadrao,
                Ativo = true
            }
        };

        /// <summary>
        /// Retorna a lista completa de usuários simulados para testes.
        /// </summary>
        public static List<Usuario> ObterUsuariosMock()
        {
            return _usuariosMocks.Select(u => new Usuario
            {
                Id = u.Id,
                Nome = u.Nome,
                Login = u.Login,
                Role = u.Role,
                Email = u.Email,
                Senha = u.Senha,
                Ativo = u.Ativo
            }).ToList();
        }

        /// <summary>
        /// Obtém o usuário padrão de abertura de turno.
        /// </summary>
        public static Usuario ObterPadrao() => ObterUsuariosMock().First();

        /// <summary>
        /// Obtém um usuário simulado pelo ID.
        /// </summary>
        public static Usuario? ObterPorId(int id)
        {
            return ObterUsuariosMock().FirstOrDefault(u => u.Id == id);
        }

        /// <summary>
        /// Valida se a senha digitada confere com a senha do usuário ou com a senha padrão (123456).
        /// </summary>
        public static bool ValidarSenha(Usuario? usuario, string senhaInformada)
        {
            if (string.IsNullOrWhiteSpace(senhaInformada))
                return false;

            if (senhaInformada == SenhaPadrao)
                return true;

            if (usuario == null || string.IsNullOrWhiteSpace(usuario.Senha))
                return senhaInformada == SenhaPadrao;

            if (string.Equals(usuario.Senha, senhaInformada, StringComparison.Ordinal))
                return true;

            try
            {
                using var sha256 = SHA256.Create();
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(senhaInformada));
                var hash = Convert.ToBase64String(bytes);
                if (string.Equals(usuario.Senha, hash, StringComparison.Ordinal))
                    return true;
            }
            catch
            {
                // Fallback silencioso
            }

            return false;
        }

        /// <summary>
        /// Garante que o usuário existe no sistema/banco; caso não exista, cria-o automaticamente.
        /// </summary>
        public static async Task<Usuario> GarantirUsuarioCriadoAsync(Usuario usuario, string senha = SenhaPadrao, IUsuarioService? usuarioService = null)
        {
            if (usuario == null)
            {
                usuario = ObterPadrao();
            }

            if (usuarioService != null)
            {
                try
                {
                    var existente = await usuarioService.ObterUsuarioPorLoginAsync(usuario.Login);
                    if (existente != null)
                    {
                        if (string.IsNullOrWhiteSpace(existente.Token))
                        {
                            existente.Token = $"session_token_{existente.Id}_{Guid.NewGuid():N}";
                        }
                        return existente;
                    }

                    // Se não encontrado no banco/API, persiste automaticamente
                    var novo = new Usuario
                    {
                        Nome = usuario.Nome,
                        Login = usuario.Login,
                        Role = string.IsNullOrWhiteSpace(usuario.Role) ? "Operador de Caixa" : usuario.Role,
                        Email = string.IsNullOrWhiteSpace(usuario.Email) ? $"{usuario.Login}@jtfood.com" : usuario.Email,
                        Ativo = true,
                        DataCadastro = DateTime.Now
                    };

                    bool salvo = await usuarioService.SalvarUsuarioAsync(novo, senha);
                    if (salvo)
                    {
                        var recarregado = await usuarioService.ObterUsuarioPorLoginAsync(novo.Login);
                        if (recarregado != null)
                        {
                            recarregado.Token = $"session_token_{recarregado.Id}_{Guid.NewGuid():N}";
                            return recarregado;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[UsuarioMockProvider] Aviso ao criar usuário automaticamente: {ex.Message}");
                }
            }

            // Fallback mock em memória
            if (string.IsNullOrWhiteSpace(usuario.Token))
            {
                usuario.Token = $"mock_token_{usuario.Id}_{Guid.NewGuid():N}";
            }
            usuario.Senha = senha;

            return usuario;
        }

        /// <summary>
        /// Garante que todos os operadores simulados estejam cadastrados no banco caso IUsuarioService esteja ativo.
        /// </summary>
        public static async Task GarantirTodosMocksCriadosAsync(IUsuarioService? usuarioService)
        {
            if (usuarioService == null) return;

            try
            {
                var todos = await usuarioService.ObterTodosUsuariosAsync();
                var loginsExistentes = new HashSet<string>(
                    (todos ?? Enumerable.Empty<Usuario>()).Select(u => u.Login?.ToLowerInvariant() ?? string.Empty),
                    StringComparer.OrdinalIgnoreCase
                );

                foreach (var mock in _usuariosMocks)
                {
                    if (!loginsExistentes.Contains(mock.Login.ToLowerInvariant()))
                    {
                        var novo = new Usuario
                        {
                            Nome = mock.Nome,
                            Login = mock.Login,
                            Role = mock.Role,
                            Email = mock.Email,
                            Ativo = true,
                            DataCadastro = DateTime.Now
                        };
                        await usuarioService.SalvarUsuarioAsync(novo, SenhaPadrao);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UsuarioMockProvider] Sincronização de mocks: {ex.Message}");
            }
        }
    }
}
