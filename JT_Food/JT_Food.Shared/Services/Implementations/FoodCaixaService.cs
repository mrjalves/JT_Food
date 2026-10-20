using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Threading.Tasks;
using JT_Classes.Restaurante;

namespace JT_Food.Shared.Services.Implementations
{
    public class FoodCaixaService : JT_Servicos.Restaurante.CaixaService
    {
        private readonly HttpClient _client;
        private static readonly ConcurrentDictionary<int, MCaixa> _cacheCaixas = new();
        private static int _nextCaixaId = 1000;

        public FoodCaixaService(HttpClient? httpClient = null) : base()
        {
            _client = httpClient ?? CriarFallbackHttpClient();

            try
            {
                var field = typeof(JT_Servicos.JTServiceBase<MCaixa>).GetField("_httpClient", BindingFlags.Instance | BindingFlags.NonPublic);
                field?.SetValue(this, _client);
            }
            catch { }
        }

        private static HttpClient CriarFallbackHttpClient()
        {
            string baseAddress = JT_Aplicacao.Config.Config.Instancia?.Conexao?.BaseApiUrl ?? string.Empty;
            if (string.IsNullOrWhiteSpace(baseAddress))
            {
                baseAddress = JT_Aplicacao.Config.Config.Instancia?.FirebaseConfig?.BaseApiUrl 
                           ?? JT_Aplicacao.Config.FirebaseConfig.Instancia?.BaseApiUrl 
                           ?? "https://api.jtecsistemas.com.br";
            }

            var client = new HttpClient();
            if (Uri.TryCreate(baseAddress, UriKind.Absolute, out var uri))
            {
                client.BaseAddress = uri;
            }

            var configPayload = new JT_Aplicacao.Config.ClientConfigPayload { ClientId = "JT_Food" };
            var json = System.Text.Json.JsonSerializer.Serialize(configPayload);
            var encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));
            if (!client.DefaultRequestHeaders.Contains("X-Client-Config"))
            {
                client.DefaultRequestHeaders.Add("X-Client-Config", encoded);
            }

            return client;
        }

        private string ObterUrlEfetiva(string relativePath)
        {
            if (relativePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                relativePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return relativePath;
            }

            if (!relativePath.StartsWith("/"))
                relativePath = "/" + relativePath;

            if (_client.BaseAddress != null)
            {
                return $"{_client.BaseAddress.ToString().TrimEnd('/')}{relativePath}";
            }

            string baseAddress = JT_Aplicacao.Config.Config.Instancia?.Conexao?.BaseApiUrl ?? string.Empty;
            if (string.IsNullOrWhiteSpace(baseAddress))
            {
                baseAddress = JT_Aplicacao.Config.Config.Instancia?.FirebaseConfig?.BaseApiUrl 
                           ?? JT_Aplicacao.Config.FirebaseConfig.Instancia?.BaseApiUrl 
                           ?? "https://api.jtecsistemas.com.br";
            }

            return $"{baseAddress.TrimEnd('/')}{relativePath}";
        }

        public override async Task<List<MCaixa>> GetAll()
        {
            var resultado = new List<MCaixa>();

            if (IsApiMode)
            {
                try
                {
                    var url = ObterUrlEfetiva(GetApiRoute());
                    var response = await _client.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        var remotos = await response.Content.ReadFromJsonAsync<List<MCaixa>>();
                        if (remotos != null && remotos.Any())
                        {
                            resultado.AddRange(remotos);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[FoodCaixaService GetAll Error]: {ex.Message}");
                }
            }
            else
            {
                try
                {
                    var baseList = await base.GetAll();
                    if (baseList != null && baseList.Any())
                    {
                        resultado.AddRange(baseList);
                    }
                }
                catch { }
            }

            // Mesclar dados do cache local (fallback em memória)
            foreach (var local in _cacheCaixas.Values)
            {
                var index = resultado.FindIndex(c => c.Id == local.Id);
                if (index >= 0)
                {
                    resultado[index] = local;
                }
                else
                {
                    resultado.Add(local);
                }
            }

            return resultado;
        }

        public override async Task<MCaixa?> GetById(int id)
        {
            if (_cacheCaixas.TryGetValue(id, out var local))
            {
                return local;
            }

            if (IsApiMode)
            {
                try
                {
                    var url = ObterUrlEfetiva($"{GetApiRoute()}/{id}");
                    var response = await _client.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        var remoto = await response.Content.ReadFromJsonAsync<MCaixa>();
                        if (remoto != null)
                        {
                            _cacheCaixas[id] = remoto;
                            return remoto;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[FoodCaixaService GetById Error]: {ex.Message}");
                }
                return null;
            }

            return await base.GetById(id);
        }

        public override async Task<bool> Save(MCaixa entity)
        {
            var saveId = await SaveAsync(entity);
            return saveId > 0;
        }

        public override async Task<int> SaveAsync(MCaixa entity)
        {
            var id = entity.Id;
            bool isNew = id <= 0;

            if (isNew)
            {
                entity.Id = System.Threading.Interlocked.Increment(ref _nextCaixaId);
            }
            _cacheCaixas[entity.Id] = entity;

            if (IsApiMode)
            {
                try
                {
                    if (isNew)
                    {
                        var url = ObterUrlEfetiva(GetApiRoute());
                        var response = await _client.PostAsJsonAsync(url, entity);
                        if (response.IsSuccessStatusCode)
                        {
                            try
                            {
                                var created = await response.Content.ReadFromJsonAsync<MCaixa>();
                                if (created != null && created.Id > 0)
                                {
                                    if (created.Id != entity.Id)
                                    {
                                        _cacheCaixas.TryRemove(entity.Id, out _);
                                        entity.Id = created.Id;
                                        _cacheCaixas[entity.Id] = entity;
                                    }
                                    return created.Id;
                                }
                            }
                            catch { }
                            return entity.Id;
                        }
                    }
                    else
                    {
                        var url = ObterUrlEfetiva($"{GetApiRoute()}/{entity.Id}");
                        var response = await _client.PutAsJsonAsync(url, entity);
                        if (response.IsSuccessStatusCode)
                        {
                            _cacheCaixas[entity.Id] = entity;
                            return entity.Id;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[FoodCaixaService SaveAsync Error]: {ex.Message}");
                }
            }
            else
            {
                try
                {
                    var baseResult = await base.SaveAsync(entity);
                    if (baseResult > 0)
                    {
                        _cacheCaixas[entity.Id] = entity;
                        return baseResult;
                    }
                }
                catch { }
            }

            return entity.Id;
        }

        public override async Task<bool> Delete(int id)
        {
            _cacheCaixas.TryRemove(id, out _);

            if (IsApiMode)
            {
                try
                {
                    var url = ObterUrlEfetiva($"{GetApiRoute()}/{id}");
                    var response = await _client.DeleteAsync(url);
                    return response.IsSuccessStatusCode;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[FoodCaixaService Delete Error]: {ex.Message}");
                    return true;
                }
            }

            return await base.Delete(id);
        }
    }

    public class FoodMovimentacaoCaixaService : JT_Servicos.Restaurante.MovimentacaoCaixaService
    {
        private readonly HttpClient _client;
        private static readonly ConcurrentDictionary<int, MMovimentacaoCaixa> _cacheMovimentacoes = new();
        private static int _nextMovId = 2000;

        public FoodMovimentacaoCaixaService(HttpClient? httpClient = null) : base()
        {
            _client = httpClient ?? CriarFallbackHttpClient();

            try
            {
                var field = typeof(JT_Servicos.JTServiceBase<MMovimentacaoCaixa>).GetField("_httpClient", BindingFlags.Instance | BindingFlags.NonPublic);
                field?.SetValue(this, _client);
            }
            catch { }
        }

        private static HttpClient CriarFallbackHttpClient()
        {
            string baseAddress = JT_Aplicacao.Config.Config.Instancia?.Conexao?.BaseApiUrl ?? string.Empty;
            if (string.IsNullOrWhiteSpace(baseAddress))
            {
                baseAddress = JT_Aplicacao.Config.Config.Instancia?.FirebaseConfig?.BaseApiUrl 
                           ?? JT_Aplicacao.Config.FirebaseConfig.Instancia?.BaseApiUrl 
                           ?? "https://api.jtecsistemas.com.br";
            }

            var client = new HttpClient();
            if (Uri.TryCreate(baseAddress, UriKind.Absolute, out var uri))
            {
                client.BaseAddress = uri;
            }

            var configPayload = new JT_Aplicacao.Config.ClientConfigPayload { ClientId = "JT_Food" };
            var json = System.Text.Json.JsonSerializer.Serialize(configPayload);
            var encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));
            if (!client.DefaultRequestHeaders.Contains("X-Client-Config"))
            {
                client.DefaultRequestHeaders.Add("X-Client-Config", encoded);
            }

            return client;
        }

        private string ObterUrlEfetiva(string relativePath)
        {
            if (relativePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                relativePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return relativePath;
            }

            if (!relativePath.StartsWith("/"))
                relativePath = "/" + relativePath;

            if (_client.BaseAddress != null)
            {
                return $"{_client.BaseAddress.ToString().TrimEnd('/')}{relativePath}";
            }

            string baseAddress = JT_Aplicacao.Config.Config.Instancia?.Conexao?.BaseApiUrl ?? string.Empty;
            if (string.IsNullOrWhiteSpace(baseAddress))
            {
                baseAddress = JT_Aplicacao.Config.Config.Instancia?.FirebaseConfig?.BaseApiUrl 
                           ?? JT_Aplicacao.Config.FirebaseConfig.Instancia?.BaseApiUrl 
                           ?? "https://api.jtecsistemas.com.br";
            }

            return $"{baseAddress.TrimEnd('/')}{relativePath}";
        }

        public override async Task<List<MMovimentacaoCaixa>> GetAll()
        {
            var resultado = new List<MMovimentacaoCaixa>();

            if (IsApiMode)
            {
                try
                {
                    var url = ObterUrlEfetiva(GetApiRoute());
                    var response = await _client.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        var remotos = await response.Content.ReadFromJsonAsync<List<MMovimentacaoCaixa>>();
                        if (remotos != null && remotos.Any())
                        {
                            resultado.AddRange(remotos);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[FoodMovimentacaoCaixaService GetAll Error]: {ex.Message}");
                }
            }
            else
            {
                try
                {
                    var baseList = await base.GetAll();
                    if (baseList != null && baseList.Any())
                    {
                        resultado.AddRange(baseList);
                    }
                }
                catch { }
            }

            // Mesclar dados locais
            foreach (var local in _cacheMovimentacoes.Values)
            {
                var index = resultado.FindIndex(m => m.Id == local.Id);
                if (index >= 0)
                {
                    resultado[index] = local;
                }
                else
                {
                    resultado.Add(local);
                }
            }

            return resultado;
        }

        public override async Task<MMovimentacaoCaixa?> GetById(int id)
        {
            if (_cacheMovimentacoes.TryGetValue(id, out var local))
            {
                return local;
            }

            if (IsApiMode)
            {
                try
                {
                    var url = ObterUrlEfetiva($"{GetApiRoute()}/{id}");
                    var response = await _client.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        var remoto = await response.Content.ReadFromJsonAsync<MMovimentacaoCaixa>();
                        if (remoto != null)
                        {
                            _cacheMovimentacoes[id] = remoto;
                            return remoto;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[FoodMovimentacaoCaixaService GetById Error]: {ex.Message}");
                }
                return null;
            }

            return await base.GetById(id);
        }

        public override async Task<bool> Save(MMovimentacaoCaixa entity)
        {
            var saveId = await SaveAsync(entity);
            return saveId > 0;
        }

        public override async Task<int> SaveAsync(MMovimentacaoCaixa entity)
        {
            var id = entity.Id;
            bool isNew = id <= 0;

            if (isNew)
            {
                entity.Id = System.Threading.Interlocked.Increment(ref _nextMovId);
            }
            _cacheMovimentacoes[entity.Id] = entity;

            if (IsApiMode)
            {
                try
                {
                    if (isNew)
                    {
                        var url = ObterUrlEfetiva(GetApiRoute());
                        var response = await _client.PostAsJsonAsync(url, entity);
                        if (response.IsSuccessStatusCode)
                        {
                            try
                            {
                                var created = await response.Content.ReadFromJsonAsync<MMovimentacaoCaixa>();
                                if (created != null && created.Id > 0)
                                {
                                    if (created.Id != entity.Id)
                                    {
                                        _cacheMovimentacoes.TryRemove(entity.Id, out _);
                                        entity.Id = created.Id;
                                        _cacheMovimentacoes[entity.Id] = entity;
                                    }
                                    return created.Id;
                                }
                            }
                            catch { }
                            return entity.Id;
                        }
                    }
                    else
                    {
                        var url = ObterUrlEfetiva($"{GetApiRoute()}/{entity.Id}");
                        var response = await _client.PutAsJsonAsync(url, entity);
                        if (response.IsSuccessStatusCode)
                        {
                            _cacheMovimentacoes[entity.Id] = entity;
                            return entity.Id;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[FoodMovimentacaoCaixaService SaveAsync Error]: {ex.Message}");
                }
            }
            else
            {
                try
                {
                    var baseResult = await base.SaveAsync(entity);
                    if (baseResult > 0)
                    {
                        _cacheMovimentacoes[entity.Id] = entity;
                        return baseResult;
                    }
                }
                catch { }
            }

            return entity.Id;
        }

        public override async Task<bool> Delete(int id)
        {
            _cacheMovimentacoes.TryRemove(id, out _);

            if (IsApiMode)
            {
                try
                {
                    var url = ObterUrlEfetiva($"{GetApiRoute()}/{id}");
                    var response = await _client.DeleteAsync(url);
                    return response.IsSuccessStatusCode;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[FoodMovimentacaoCaixaService Delete Error]: {ex.Message}");
                    return true;
                }
            }

            return await base.Delete(id);
        }
    }
}
