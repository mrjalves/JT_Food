using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JT_Classes.IA;
using JT_Food.Shared.Services.Implementations;
using JT_Servicos.IA;
using JT_Servicos.IA.Interfaces;
using Microsoft.Extensions.Configuration;

namespace JT_Food.Shared.Services.Implementations
{
    /// <summary>
    /// Wrapper resiliente de IInstrutorIAService para o JT_Food.
    /// Intercepta chamadas sem chave de API e orienta o usuário de forma amigável sobre como
    /// configurar sua chave do Google Gemini sem causar exceções na interface.
    /// </summary>
    public class FoodInstrutorIAService : IInstrutorIAService
    {
        private readonly InstrutorIAService _innerService;
        private readonly ConfiguracaoIAService _configService;
        private readonly IConfiguration _configuration;

        public FoodInstrutorIAService(
            InstrutorIAService innerService,
            ConfiguracaoIAService configService,
            IConfiguration configuration)
        {
            _innerService = innerService;
            _configService = configService;
            _configuration = configuration;
        }

        public async Task<MensagemChat> EnviarMensagemAsync(
            int idSessao,
            string mensagemUsuario,
            string rotaAtual,
            string contextoTela = "",
            List<DtoAnexo>? anexos = null,
            CancellationToken ct = default)
        {
            var configs = await _configService.GetAll();
            var config = configs.FirstOrDefault();
            string apiKey = config?.ApiKey
                         ?? GeminiRelatorioService.CustomApiKey
                         ?? _configuration["Config:Gemini:ApiKey"]
                         ?? _configuration["Gemini:ApiKey"]
                         ?? _configuration["GeminiApiKey"]
                         ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                         ?? string.Empty;

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return new MensagemChat
                {
                    IdSessao = idSessao,
                    Role = "assistant",
                    EnviadaEm = DateTime.UtcNow,
                    Content = @"### ⚙️ Assistente IA — Configuração Necessária

Olá! Para que eu possa responder suas perguntas e analisar os dados do seu restaurante com inteligência artificial, você precisa informar sua chave de API gratuita do **Google Gemini**.

---

#### 🚀 Como Ativar em 3 Passos:
1. **Obtenha sua chave gratuita:** Acesse [Google AI Studio (aistudio.google.com/app/apikey)](https://aistudio.google.com/app/apikey) e gere sua API Key.
2. **Acesse as Configurações:** No menu lateral do sistema, vá em **Configurações ➔ aba Inteligência Artificial (Gemini)**.
3. **Salve a Chave:** Cole sua chave no campo **Google Gemini API Key**, clique em **Identificar Modelos** e depois no botão verde **Salvar Configurações de IA**.

*(Dica: Você também pode informar a chave no arquivo `appsettings.json` na chave `Config:Gemini:ApiKey`).*"
                };
            }

            // Garante que o config tenha a chave resolvida
            if (config != null && string.IsNullOrWhiteSpace(config.ApiKey))
            {
                config.ApiKey = apiKey;
            }

            return await _innerService.EnviarMensagemAsync(idSessao, mensagemUsuario, rotaAtual, contextoTela, anexos, ct);
        }

        public Task<SessaoInstrucao> IniciarSessaoAsync(string rotaAtual, int idUsuario) =>
            _innerService.IniciarSessaoAsync(rotaAtual, idUsuario);

        public Task EncerrarSessaoAsync(int idSessao) =>
            _innerService.EncerrarSessaoAsync(idSessao);

        public Task EncerrarSessoesOrfasAsync(int idUsuario) =>
            _innerService.EncerrarSessoesOrfasAsync(idUsuario);

        public Task<string> GerarMarkdownSessaoAsync(int idSessao) =>
            _innerService.GerarMarkdownSessaoAsync(idSessao);

        public Task<bool> Delete(int id) =>
            _innerService.Delete(id);

        public Task<List<SessaoInstrucao>> Find(string condicao) =>
            _innerService.Find(condicao);
    }
}
