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
    public class FoodItemComandaService : JT_Servicos.Restaurante.ItemComandaService
    {
        private readonly HttpClient _client;
        private static readonly ConcurrentDictionary<int, MItemComanda> _cacheItens = new();
        private static int _nextLocalId = 1000;

        public FoodItemComandaService(HttpClient? httpClient = null) : base(httpClient)
        {
            _client = httpClient ?? CriarFallbackHttpClient();

            try
            {
                var field = typeof(JT_Servicos.JTServiceBase<MItemComanda>).GetField("_httpClient", BindingFlags.Instance | BindingFlags.NonPublic);
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

        public override async Task<List<MItemComanda>> GetAll()
        {
            var resultado = new Dictionary<int, MItemComanda>();

            // 1. Tentar obter da API
            if (IsApiMode)
            {
                try
                {
                    var url = ObterUrlEfetiva(GetApiRoute());
                    var response = await _client.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        var apiItens = await response.Content.ReadFromJsonAsync<List<MItemComanda>>();
                        if (apiItens != null)
                        {
                            foreach (var item in apiItens)
                            {
                                if (item.Id > 0)
                                {
                                    resultado[item.Id] = item;
                                    _cacheItens[item.Id] = item;
                                }
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[FoodItemComandaService GetAll API Status]: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[FoodItemComandaService GetAll API Error]: {ex.Message}");
                }

                // Fallback para base.GetAll() se a API não retornou itens (ex: 401 sem token ou offline)
                if (resultado.Count == 0)
                {
                    try
                    {
                        var baseItens = await base.GetAll();
                        if (baseItens != null)
                        {
                            foreach (var item in baseItens)
                            {
                                if (item.Id > 0)
                                {
                                    resultado[item.Id] = item;
                                    _cacheItens[item.Id] = item;
                                }
                            }
                        }
                    }
                    catch (Exception exBase)
                    {
                        Console.WriteLine($"[FoodItemComandaService GetAll Base Fallback Error]: {exBase.Message}");
                    }
                }

                // Fallback para banco direto se a API e base não retornaram itens ou falhou (apenas quando não executando no WebAssembly do browser)
                if (!OperatingSystem.IsBrowser() && resultado.Count == 0 && !string.IsNullOrWhiteSpace(JT_Aplicacao.Config.Config.Instancia?.Conexao?.StringConexao))
                {
                    try
                    {
                        var dbItens = JT_Aplicacao.Classes.BaseRepository.GetAll<MItemComanda>();
                        if (dbItens != null)
                        {
                            foreach (var item in dbItens)
                            {
                                if (item.Id > 0)
                                {
                                    resultado[item.Id] = item;
                                    _cacheItens[item.Id] = item;
                                }
                            }
                        }
                    }
                    catch (Exception exDb)
                    {
                        Console.WriteLine($"[FoodItemComandaService GetAll DB Fallback Error]: {exDb.Message}");
                    }
                }
            }
            else
            {
                try
                {
                    var dbItens = await base.GetAll();
                    if (dbItens != null)
                    {
                        foreach (var item in dbItens)
                        {
                            if (item.Id > 0)
                            {
                                resultado[item.Id] = item;
                                _cacheItens[item.Id] = item;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[FoodItemComandaService GetAll Base Error]: {ex.Message}");
                }
            }

            // 2. Mesclar itens do cache local (garante que nenhum item lançado se perca)
            foreach (var kv in _cacheItens)
            {
                if (!resultado.ContainsKey(kv.Key))
                {
                    resultado[kv.Key] = kv.Value;
                }
            }

            return resultado.Values.OrderByDescending(i => i.Id).ToList();
        }

        public override async Task<MItemComanda?> GetById(int id)
        {
            if (_cacheItens.TryGetValue(id, out var emCache))
            {
                return emCache;
            }

            if (IsApiMode)
            {
                try
                {
                    var url = ObterUrlEfetiva($"{GetApiRoute()}/{id}");
                    var response = await _client.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        var item = await response.Content.ReadFromJsonAsync<MItemComanda>();
                        if (item != null)
                        {
                            _cacheItens[item.Id] = item;
                            return item;
                        }
                    }
                }
                catch { }

                if (!OperatingSystem.IsBrowser() && !string.IsNullOrWhiteSpace(JT_Aplicacao.Config.Config.Instancia?.Conexao?.StringConexao))
                {
                    try
                    {
                        var dbItem = JT_Aplicacao.Classes.BaseRepository.GetById<MItemComanda>(id);
                        if (dbItem != null)
                        {
                            _cacheItens[dbItem.Id] = dbItem;
                            return dbItem;
                        }
                    }
                    catch { }
                }
            }

            return await base.GetById(id);
        }

        public override async Task<int> SaveAsync(MItemComanda entity)
        {
            bool isNew = entity.Id <= 0;
            if (isNew)
            {
                entity.Id = System.Threading.Interlocked.Increment(ref _nextLocalId);
            }

            // Armazena no cache local imediatamente
            _cacheItens[entity.Id] = entity;

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
                                var created = await response.Content.ReadFromJsonAsync<MItemComanda>();
                                if (created != null && created.Id > 0)
                                {
                                    if (created.Id != entity.Id)
                                    {
                                        _cacheItens.TryRemove(entity.Id, out _);
                                        entity.Id = created.Id;
                                        _cacheItens[entity.Id] = entity;
                                    }
                                    return created.Id;
                                }
                            }
                            catch { }
                            return entity.Id;
                        }
                        else
                        {
                            Console.WriteLine($"[FoodItemComandaService SaveAsync API Error] Status: {response.StatusCode}");
                        }
                    }
                    else
                    {
                        var url = ObterUrlEfetiva($"{GetApiRoute()}/{entity.Id}");
                        var response = await _client.PutAsJsonAsync(url, entity);
                        if (response.IsSuccessStatusCode) return entity.Id;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[FoodItemComandaService SaveAsync Exception]: {ex.Message}");
                }

                // Fallback para banco direto caso a API falhe (apenas quando não executando no WebAssembly do browser)
                if (!OperatingSystem.IsBrowser() && !string.IsNullOrWhiteSpace(JT_Aplicacao.Config.Config.Instancia?.Conexao?.StringConexao))
                {
                    try
                    {
                        int dbId;
                        if (isNew)
                        {
                            dbId = JT_Aplicacao.Classes.BaseRepository.Add(entity);
                            if (dbId > 0)
                            {
                                if (dbId != entity.Id)
                                {
                                    _cacheItens.TryRemove(entity.Id, out _);
                                    entity.Id = dbId;
                                }
                            }
                        }
                        else
                        {
                            bool ok = JT_Aplicacao.Classes.BaseRepository.Update(entity);
                            dbId = ok ? entity.Id : 0;
                        }

                        if (dbId > 0)
                        {
                            _cacheItens[entity.Id] = entity;
                            return dbId;
                        }
                    }
                    catch (Exception exDb)
                    {
                        Console.WriteLine($"[FoodItemComandaService SaveAsync DB Fallback Error]: {exDb.Message}");
                    }
                }

                return entity.Id;
            }

            try
            {
                return await base.SaveAsync(entity);
            }
            catch
            {
                return entity.Id;
            }
        }

        public override async Task<bool> Save(MItemComanda entity)
        {
            int res = await SaveAsync(entity);
            return res > 0;
        }

        public override async Task<bool> Delete(int id)
        {
            _cacheItens.TryRemove(id, out _);

            if (IsApiMode)
            {
                try
                {
                    var url = ObterUrlEfetiva($"{GetApiRoute()}/{id}");
                    var response = await _client.DeleteAsync(url);
                    return response.IsSuccessStatusCode;
                }
                catch { }
                return true;
            }

            return await base.Delete(id);
        }
    }

    public class FoodAdicionaisItemComandaService : JT_Servicos.Restaurante.AdicionaisItemComandaService
    {
        private readonly HttpClient _client;
        private static readonly ConcurrentDictionary<int, MAdicionaisItemComanda> _cacheAds = new();
        private static int _nextLocalId = 2000;

        public FoodAdicionaisItemComandaService(HttpClient? httpClient = null) : base(httpClient)
        {
            _client = httpClient ?? CriarFallbackHttpClient();

            try
            {
                var field = typeof(JT_Servicos.JTServiceBase<MAdicionaisItemComanda>).GetField("_httpClient", BindingFlags.Instance | BindingFlags.NonPublic);
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

        public override async Task<List<MAdicionaisItemComanda>> GetAll()
        {
            var resultado = new Dictionary<int, MAdicionaisItemComanda>();

            if (IsApiMode)
            {
                try
                {
                    var url = ObterUrlEfetiva(GetApiRoute());
                    var response = await _client.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        var apiAds = await response.Content.ReadFromJsonAsync<List<MAdicionaisItemComanda>>();
                        if (apiAds != null)
                        {
                            foreach (var item in apiAds)
                            {
                                if (item.Id > 0)
                                {
                                    resultado[item.Id] = item;
                                    _cacheAds[item.Id] = item;
                                }
                            }
                        }
                    }
                }
                catch { }

                // Fallback para base.GetAll() se a API não retornou adicionais
                if (resultado.Count == 0)
                {
                    try
                    {
                        var baseAds = await base.GetAll();
                        if (baseAds != null)
                        {
                            foreach (var item in baseAds)
                            {
                                if (item.Id > 0)
                                {
                                    resultado[item.Id] = item;
                                    _cacheAds[item.Id] = item;
                                }
                            }
                        }
                    }
                    catch { }
                }

                // Fallback para banco direto se a API não retornou itens ou falhou (apenas quando não executando no WebAssembly do browser)
                if (!OperatingSystem.IsBrowser() && resultado.Count == 0 && !string.IsNullOrWhiteSpace(JT_Aplicacao.Config.Config.Instancia?.Conexao?.StringConexao))
                {
                    try
                    {
                        var dbAds = JT_Aplicacao.Classes.BaseRepository.GetAll<MAdicionaisItemComanda>();
                        if (dbAds != null)
                        {
                            foreach (var item in dbAds)
                            {
                                if (item.Id > 0)
                                {
                                    resultado[item.Id] = item;
                                    _cacheAds[item.Id] = item;
                                }
                            }
                        }
                    }
                    catch { }
                }
            }
            else
            {
                try
                {
                    var dbAds = await base.GetAll();
                    if (dbAds != null)
                    {
                        foreach (var item in dbAds)
                        {
                            if (item.Id > 0)
                            {
                                resultado[item.Id] = item;
                                _cacheAds[item.Id] = item;
                            }
                        }
                    }
                }
                catch { }
            }

            foreach (var kv in _cacheAds)
            {
                if (!resultado.ContainsKey(kv.Key))
                {
                    resultado[kv.Key] = kv.Value;
                }
            }

            return resultado.Values.OrderByDescending(i => i.Id).ToList();
        }

        public override async Task<int> SaveAsync(MAdicionaisItemComanda entity)
        {
            bool isNew = entity.Id <= 0;
            if (isNew)
            {
                entity.Id = System.Threading.Interlocked.Increment(ref _nextLocalId);
            }

            _cacheAds[entity.Id] = entity;

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
                                var created = await response.Content.ReadFromJsonAsync<MAdicionaisItemComanda>();
                                if (created != null && created.Id > 0)
                                {
                                    if (created.Id != entity.Id)
                                    {
                                        _cacheAds.TryRemove(entity.Id, out _);
                                        entity.Id = created.Id;
                                        _cacheAds[entity.Id] = entity;
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
                        if (response.IsSuccessStatusCode) return entity.Id;
                    }
                }
                catch { }

                // Fallback para banco direto caso a API falhe (apenas quando não executando no WebAssembly do browser)
                if (!OperatingSystem.IsBrowser() && !string.IsNullOrWhiteSpace(JT_Aplicacao.Config.Config.Instancia?.Conexao?.StringConexao))
                {
                    try
                    {
                        int dbId;
                        if (isNew)
                        {
                            dbId = JT_Aplicacao.Classes.BaseRepository.Add(entity);
                            if (dbId > 0)
                            {
                                if (dbId != entity.Id)
                                {
                                    _cacheAds.TryRemove(entity.Id, out _);
                                    entity.Id = dbId;
                                }
                            }
                        }
                        else
                        {
                            bool ok = JT_Aplicacao.Classes.BaseRepository.Update(entity);
                            dbId = ok ? entity.Id : 0;
                        }

                        if (dbId > 0)
                        {
                            _cacheAds[entity.Id] = entity;
                            return dbId;
                        }
                    }
                    catch { }
                }

                return entity.Id;
            }

            try
            {
                return await base.SaveAsync(entity);
            }
            catch
            {
                return entity.Id;
            }
        }
    }
}
