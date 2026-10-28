using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using JT_Classes.IA;
using JT_Food.Shared.Services.Interfaces;
using Microsoft.Extensions.Configuration;

namespace JT_Food.Shared.Services.Implementations
{
    /// <summary>
    /// Implementação resiliente de ConfiguracaoIAService para o JT_Food.
    /// Garante que o modelo padrão nunca seja 'gpt-4o' vazio, sincroniza com o IIAAnalyticsService,
    /// resolve chaves de appsettings / variáveis de ambiente e previne que o dropdown de modelos fique em branco.
    /// </summary>
    public class FoodConfiguracaoIAService : JT_Servicos.IA.ConfiguracaoIAService
    {
        private readonly IConfiguration _configuration;
        private readonly IIAAnalyticsService? _iaAnalyticsService;
        private readonly ConcurrentDictionary<int, ConfiguracaoIA> _cacheConfig = new();
        private ConfiguracaoIA _configPadrao = new()
        {
            Id = 1,
            Modelo = "gemini-1.5-flash",
            Provedor = "Gemini",
            Temperatura = 0.7m,
            MaxTokens = 2000,
            SystemPromptBase = "Você é o Copiloto Executivo do JT_Food, especialista em gestão de restaurantes, salão, comandas, KDS, estoque e faturamento.",
            HabilitadoParaRoles = "Administrador,Supervisor,Operador,Garcom,Cozinha"
        };

        public FoodConfiguracaoIAService(
            IConfiguration configuration,
            IIAAnalyticsService? iaAnalyticsService = null,
            HttpClient? httpClient = null) : base(httpClient)
        {
            _configuration = configuration;
            _iaAnalyticsService = iaAnalyticsService;
        }

        public override async Task<List<ConfiguracaoIA>> GetAll()
        {
            var resultado = new List<ConfiguracaoIA>();

            try
            {
                var remotos = await base.GetAll();
                if (remotos != null && remotos.Any())
                {
                    resultado.AddRange(remotos);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FoodConfiguracaoIAService] Aviso ao buscar configs remotas: {ex.Message}");
            }

            if (!resultado.Any())
            {
                resultado.Add(_configPadrao);
            }

            foreach (var cfg in resultado)
            {
                EnriquecerConfiguracao(cfg);
                _cacheConfig[cfg.Id] = cfg;
            }

            return resultado;
        }

        public new async Task<ConfiguracaoIA> GetAtivaAsync()
        {
            var todas = await GetAll();
            var ativa = todas.FirstOrDefault() ?? _configPadrao;
            EnriquecerConfiguracao(ativa);
            return ativa;
        }

        public override async Task<ConfiguracaoIA?> GetById(int id)
        {
            if (_cacheConfig.TryGetValue(id, out var emCache))
            {
                EnriquecerConfiguracao(emCache);
                return emCache;
            }

            try
            {
                var remoto = await base.GetById(id);
                if (remoto != null)
                {
                    EnriquecerConfiguracao(remoto);
                    _cacheConfig[id] = remoto;
                    return remoto;
                }
            }
            catch { }

            var padrao = _configPadrao;
            EnriquecerConfiguracao(padrao);
            return padrao;
        }

        public override async Task<bool> Save(ConfiguracaoIA entity)
        {
            var saveId = await SaveAsync(entity);
            return saveId > 0;
        }

        public override async Task<int> SaveAsync(ConfiguracaoIA entity)
        {
            EnriquecerConfiguracao(entity);
            _configPadrao = entity;
            _cacheConfig[entity.Id] = entity;

            if (_iaAnalyticsService != null)
            {
                if (!string.IsNullOrWhiteSpace(entity.ApiKey))
                {
                    _iaAnalyticsService.CustomApiKey = entity.ApiKey;
                }
                if (!string.IsNullOrWhiteSpace(entity.Modelo))
                {
                    _iaAnalyticsService.CustomModel = entity.Modelo;
                }
            }

            try
            {
                var baseId = await base.SaveAsync(entity);
                if (baseId > 0)
                {
                    entity.Id = baseId;
                    _cacheConfig[baseId] = entity;
                    return baseId;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FoodConfiguracaoIAService] Aviso ao persistir via API/banco: {ex.Message}. Mantendo local.");
            }

            return entity.Id > 0 ? entity.Id : 1;
        }

        public new async Task<List<string>> ListarModelosDisponiveisAsync(string? overrideApiKey = null)
        {
            var modelos = new List<string>
            {
                "gemini-1.5-flash",
                "gemini-1.5-pro",
                "gemini-2.0-flash",
                "gemini-2.5-flash"
            };

            string? key = overrideApiKey
                      ?? _iaAnalyticsService?.CustomApiKey
                      ?? ObterApiKeyConfigurada();

            if (!string.IsNullOrWhiteSpace(key))
            {
                try
                {
                    var remotos = await base.ListarModelosDisponiveisAsync(key);
                    if (remotos != null && remotos.Any())
                    {
                        foreach (var r in remotos)
                        {
                            if (!modelos.Contains(r, StringComparer.OrdinalIgnoreCase))
                            {
                                modelos.Add(r);
                            }
                        }
                    }
                }
                catch { }
            }

            return modelos;
        }

        private void EnriquecerConfiguracao(ConfiguracaoIA cfg)
        {
            // 1. ApiKey
            if (string.IsNullOrWhiteSpace(cfg.ApiKey))
            {
                cfg.ApiKey = _iaAnalyticsService?.CustomApiKey ?? ObterApiKeyConfigurada();
            }

            // 2. Modelo: nunca permitir gpt-4o ou vazio para o motor Gemini do Food
            if (string.IsNullOrWhiteSpace(cfg.Modelo) || cfg.Modelo.Equals("gpt-4o", StringComparison.OrdinalIgnoreCase))
            {
                cfg.Modelo = _iaAnalyticsService?.CustomModel ?? "gemini-1.5-flash";
            }

            // 3. Provedor
            if (string.IsNullOrWhiteSpace(cfg.Provedor))
            {
                cfg.Provedor = "Gemini";
            }
        }

        private string? ObterApiKeyConfigurada()
        {
            return _configuration["Config:Gemini:ApiKey"]
                ?? _configuration["Gemini:ApiKey"]
                ?? _configuration["GeminiApiKey"]
                ?? _configuration["AI:ApiKey"]
                ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        }
    }
}
