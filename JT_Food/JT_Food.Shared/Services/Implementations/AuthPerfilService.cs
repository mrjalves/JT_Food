using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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
        private const int MAX_TENTATIVAS_PIN = 5;
        private static readonly TimeSpan TEMPO_BLOQUEIO_PIN = TimeSpan.FromMinutes(3);

        // Chave efêmera de processo para assinar sessões salvas no navegador e impedir adulteração de Perfil no localStorage
        private static readonly byte[] _sessionSigningKey = RandomNumberGenerator.GetBytes(32);

        private static readonly ConcurrentDictionary<string, (int Tentativas, DateTime? BloqueadoAte)> _controleTentativas = new();

        private UsuarioOperador _usuarioAtual;
        public event Action? OnSessaoAlterada;

        // Operadores com PIN armazenado apenas como hash SHA-256 (salted)
        private readonly List<UsuarioOperador> _usuariosPreConfigurados = new()
        {
            new UsuarioOperador
            {
                Id = "1",
                Nome = "Carlos Gerente",
                PinRapido = HashSecret("1234", "1"),
                Perfil = PerfilAcesso.Gerente,
                Email = "gerente@jtfood.com",
                Ativo = true
            },
            new UsuarioOperador
            {
                Id = "2",
                Nome = "Lucas Garçom",
                PinRapido = HashSecret("2222", "2"),
                Perfil = PerfilAcesso.Garcom,
                Email = "garcom@jtfood.com",
                Ativo = true
            },
            new UsuarioOperador
            {
                Id = "3",
                Nome = "Mariana Caixa",
                PinRapido = HashSecret("3333", "3"),
                Perfil = PerfilAcesso.Caixa,
                Email = "caixa@jtfood.com",
                Ativo = true
            }
        };

        public AuthPerfilService(ISecureStorageService storage)
        {
            _storage = storage;
            // Padrão inicial: Gerente (para navegação completa do menu lateral)
            _usuarioAtual = ClonarSemSegredo(_usuariosPreConfigurados.First());
        }

        private static string HashSecret(string secret, string salt)
        {
            var bytes = Encoding.UTF8.GetBytes($"JT_FOOD_SALT_{salt}:{secret}");
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash);
        }

        private static string ComputeSignature(string payloadJson)
        {
            using var hmac = new HMACSHA256(_sessionSigningKey);
            var sig = hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadJson));
            return Convert.ToBase64String(sig);
        }

        private sealed class SessaoAssinada
        {
            public string Payload { get; set; } = string.Empty;
            public string Assinatura { get; set; } = string.Empty;
        }

        public async Task<UsuarioOperador> ObterUsuarioLogadoAsync()
        {
            try
            {
                var raw = await _storage.GetTokenAsync(STORAGE_KEY);
                if (!string.IsNullOrWhiteSpace(raw))
                {
                    var envelope = JsonSerializer.Deserialize<SessaoAssinada>(raw);
                    if (envelope != null && !string.IsNullOrWhiteSpace(envelope.Payload) && !string.IsNullOrWhiteSpace(envelope.Assinatura))
                    {
                        var expectedSig = ComputeSignature(envelope.Payload);
                        if (CryptographicOperations.FixedTimeEquals(
                            Encoding.UTF8.GetBytes(expectedSig),
                            Encoding.UTF8.GetBytes(envelope.Assinatura)))
                        {
                            var user = JsonSerializer.Deserialize<UsuarioOperador>(envelope.Payload);
                            if (user != null)
                            {
                                _usuarioAtual = user;
                            }
                        }
                        else
                        {
                            // Sessão adulterada ou expirada: limpa token inválido
                            _storage.RemoveToken(STORAGE_KEY);
                        }
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
            const string chaveRateLimit = "PIN_GLOBAL";
            if (_controleTentativas.TryGetValue(chaveRateLimit, out var estado) &&
                estado.BloqueadoAte.HasValue &&
                estado.BloqueadoAte.Value > DateTime.UtcNow)
            {
                var segundosRestantes = Math.Max(1, (int)(estado.BloqueadoAte.Value - DateTime.UtcNow).TotalSeconds);
                return (false, $"Muitas tentativas inválidas. Aguarde {segundosRestantes}s antes de tentar novamente.", null);
            }

            if (string.IsNullOrWhiteSpace(pin) || pin.Length < 4)
            {
                return (false, "Informe o PIN numérico de 4 dígitos.", null);
            }

            var user = _usuariosPreConfigurados.FirstOrDefault(u =>
                u.Ativo &&
                CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(u.PinRapido),
                    Encoding.UTF8.GetBytes(HashSecret(pin.Trim(), u.Id))));

            if (user == null)
            {
                RegistrarFalhaTentativa(chaveRateLimit);
                return (false, "PIN incorreto ou operador inativo.", null);
            }

            _controleTentativas.TryRemove(chaveRateLimit, out _);

            _usuarioAtual = ClonarSemSegredo(user);
            await SalvarSessaoAsync(_usuarioAtual);
            OnSessaoAlterada?.Invoke();

            return (true, $"Bem-vindo, {user.Nome} ({user.Perfil})!", _usuarioAtual);
        }

        public async Task<(bool Sucesso, string Mensagem, UsuarioOperador? Usuario)> LoginCredenciaisAsync(string email, string senha)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
            {
                return (false, "Informe email e senha.", null);
            }

            string chaveRateLimit = $"EMAIL_{email.Trim().ToLowerInvariant()}";
            if (_controleTentativas.TryGetValue(chaveRateLimit, out var estado) &&
                estado.BloqueadoAte.HasValue &&
                estado.BloqueadoAte.Value > DateTime.UtcNow)
            {
                var segundosRestantes = Math.Max(1, (int)(estado.BloqueadoAte.Value - DateTime.UtcNow).TotalSeconds);
                return (false, $"Conta temporariamente bloqueada por excesso de tentativas. Aguarde {segundosRestantes}s.", null);
            }

            var user = _usuariosPreConfigurados.FirstOrDefault(u =>
                u.Ativo && string.Equals(u.Email, email.Trim(), StringComparison.OrdinalIgnoreCase));

            string senhaEsperadaHash = HashSecret("123456", user?.Id ?? "0");
            string senhaInformadaHash = HashSecret(senha, user?.Id ?? "0");

            if (user == null || !CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(senhaEsperadaHash),
                Encoding.UTF8.GetBytes(senhaInformadaHash)))
            {
                RegistrarFalhaTentativa(chaveRateLimit);
                return (false, "Email ou senha incorretos.", null);
            }

            _controleTentativas.TryRemove(chaveRateLimit, out _);

            _usuarioAtual = ClonarSemSegredo(user);
            await SalvarSessaoAsync(_usuarioAtual);
            OnSessaoAlterada?.Invoke();

            return (true, $"Bem-vindo, {user.Nome}!", _usuarioAtual);
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
                PinRapido = string.Empty
            };

            _usuarioAtual = op;
            await SalvarSessaoAsync(op);
            OnSessaoAlterada?.Invoke();
        }

        public async Task LogoutAsync()
        {
            _storage.RemoveToken(STORAGE_KEY);
            _usuarioAtual = ClonarSemSegredo(_usuariosPreConfigurados.First(u => u.Perfil == PerfilAcesso.Garcom));
            OnSessaoAlterada?.Invoke();
            await Task.CompletedTask;
        }

        public PerfilAcesso ObterPerfilAtual() => _usuarioAtual.Perfil;

        public bool IsGerente() => _usuarioAtual.Perfil == PerfilAcesso.Gerente;

        public bool IsGarcom() => _usuarioAtual.Perfil == PerfilAcesso.Garcom;

        public bool IsCaixa() => _usuarioAtual.Perfil == PerfilAcesso.Caixa;

        private static void RegistrarFalhaTentativa(string chave)
        {
            _controleTentativas.AddOrUpdate(
                chave,
                _ => (1, null),
                (_, atual) =>
                {
                    int novas = atual.Tentativas + 1;
                    DateTime? bloqueio = novas >= MAX_TENTATIVAS_PIN
                        ? DateTime.UtcNow.Add(TEMPO_BLOQUEIO_PIN)
                        : null;
                    return (novas >= MAX_TENTATIVAS_PIN ? 0 : novas, bloqueio);
                });
        }

        private static UsuarioOperador ClonarSemSegredo(UsuarioOperador origem)
        {
            return new UsuarioOperador
            {
                Id = origem.Id,
                Nome = origem.Nome,
                Email = origem.Email,
                Perfil = origem.Perfil,
                Ativo = origem.Ativo,
                PinRapido = string.Empty
            };
        }

        private async Task SalvarSessaoAsync(UsuarioOperador user)
        {
            try
            {
                var payloadJson = JsonSerializer.Serialize(ClonarSemSegredo(user));
                var envelope = new SessaoAssinada
                {
                    Payload = payloadJson,
                    Assinatura = ComputeSignature(payloadJson)
                };
                var raw = JsonSerializer.Serialize(envelope);
                await _storage.SetTokenAsync(STORAGE_KEY, raw);
            }
            catch
            {
                // Fallback
            }
        }
    }
}
