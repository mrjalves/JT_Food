using System.Collections.Generic;
using System.Linq;
using JT_Classes.Acessos;

namespace JT_Food.Shared.Mocks
{
    /// <summary>
    /// Fornece operadores e usuários simulados para testes de abertura de caixa,
    /// turnos de PDV e rotinas de atendimento.
    /// </summary>
    public static class UsuarioMockProvider
    {
        private static readonly List<Usuario> _usuariosMocks = new()
        {
            new Usuario
            {
                Id = 1,
                Nome = "Operador Geral (PDV 01)",
                Login = "operador01",
                Role = "Operador de Caixa",
                Email = "operador01@jtfood.com",
                Ativo = true
            },
            new Usuario
            {
                Id = 2,
                Nome = "Julio Alves (Supervisor Geral)",
                Login = "supervisor",
                Role = "Supervisor",
                Email = "supervisor@jtfood.com",
                Ativo = true
            },
            new Usuario
            {
                Id = 3,
                Nome = "Mariana Silva (Caixa Manhã)",
                Login = "mariana.caixa",
                Role = "Operador de Caixa",
                Email = "mariana@jtfood.com",
                Ativo = true
            },
            new Usuario
            {
                Id = 4,
                Nome = "Lucas Santos (Caixa Noite)",
                Login = "lucas.caixa",
                Role = "Operador de Caixa",
                Email = "lucas@jtfood.com",
                Ativo = true
            },
            new Usuario
            {
                Id = 5,
                Nome = "Carlos Eduardo (Gerente Operacional)",
                Login = "carlos.gerente",
                Role = "Gerente",
                Email = "gerente@jtfood.com",
                Ativo = true
            },
            new Usuario
            {
                Id = 6,
                Nome = "Ana Paula (Atendente / Balcão)",
                Login = "anapaula",
                Role = "Atendente",
                Email = "anapaula@jtfood.com",
                Ativo = true
            },
            new Usuario
            {
                Id = 7,
                Nome = "Roberto Lima (Fechamento / Auditor)",
                Login = "roberto.auditor",
                Role = "Auditor",
                Email = "auditor@jtfood.com",
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
    }
}
