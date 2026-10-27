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
    public class FoodComandaService : JT_Servicos.Restaurante.ComandaService
    {
        private readonly HttpClient _client;
        private static readonly ConcurrentDictionary<int, Comanda> _cacheComandas = new();
        private static int _nextComandaId = 500;

        public FoodComandaService(HttpClient? httpClient = null) : base(httpClient)
        {
            _client = httpClient ?? CriarFallbackHttpClient();

            try
            {
                var field = typeof(JT_Servicos.JTServiceBase<Comanda>).GetField("_httpClient", BindingFlags.Instance | BindingFlags.NonPublic);
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

        public override async Task<List<Comanda>> GetAll()
        {
            var resultado = new Dictionary<int, Comanda>();

            if (IsApiMode)
            {
                try
                {
                    var url = ObterUrlEfetiva(GetApiRoute());
                    var response = await _client.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        var remotos = await response.Content.ReadFromJsonAsync<List<Comanda>>();
                        if (remotos != null)
                        {
                            foreach (var r in remotos.Where(c => c.Id > 0))
                            {
                                resultado[r.Id] = r;
                                _cacheComandas[r.Id] = r;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[FoodComandaService GetAll Error]: {ex.Message}");
                }
            }
            else
            {
                try
                {
                    var baseList = await base.GetAll();
                    if (baseList != null)
                    {
                        foreach (var b in baseList.Where(c => c.Id > 0))
                        {
                            resultado[b.Id] = b;
                            _cacheComandas[b.Id] = b;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[FoodComandaService GetAll Base Exception]: {ex.Message}");
                }
            }

            // Mesclar dados do cache local (sempre preserva os dados mais recentes em memória)
            foreach (var kv in _cacheComandas)
            {
                resultado[kv.Key] = kv.Value;
            }

            return resultado.Values.OrderByDescending(c => c.DataAbertura).ToList();
        }

        public override async Task<Comanda?> GetById(int id)
        {
            if (_cacheComandas.TryGetValue(id, out var local))
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
                        var remoto = await response.Content.ReadFromJsonAsync<Comanda>();
                        if (remoto != null)
                        {
                            _cacheComandas[id] = remoto;
                            return remoto;
                        }
                    }
                }
                catch { }
                return null;
            }

            try
            {
                var baseItem = await base.GetById(id);
                if (baseItem != null)
                {
                    _cacheComandas[id] = baseItem;
                    return baseItem;
                }
            }
            catch { }

            return null;
        }

        public override async Task<bool> Save(Comanda entity)
        {
            var id = await SaveAsync(entity);
            return id > 0;
        }

        public override async Task<int> SaveAsync(Comanda entity)
        {
            bool isNew = entity.Id <= 0;
            if (isNew)
            {
                // Se o cache já possui registros, garante um id único sequencial
                int maxId = _cacheComandas.Any() ? _cacheComandas.Keys.Max() : _nextComandaId;
                if (maxId >= _nextComandaId) _nextComandaId = maxId + 1;
                entity.Id = System.Threading.Interlocked.Increment(ref _nextComandaId);
            }

            _cacheComandas[entity.Id] = entity;

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
                                var created = await response.Content.ReadFromJsonAsync<Comanda>();
                                if (created != null && created.Id > 0)
                                {
                                    if (created.Id != entity.Id)
                                    {
                                        _cacheComandas.TryRemove(entity.Id, out _);
                                        entity.Id = created.Id;
                                        _cacheComandas[entity.Id] = entity;
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
                catch (Exception ex)
                {
                    Console.WriteLine($"[FoodComandaService SaveAsync Exception]: {ex.Message}");
                }
            }
            else
            {
                try
                {
                    var baseResult = await base.SaveAsync(entity);
                    if (baseResult > 0)
                    {
                        _cacheComandas[entity.Id] = entity;
                        return baseResult;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[FoodComandaService SaveAsync Base Error]: {ex.Message}");
                }
            }

            return entity.Id;
        }

        public override async Task<bool> Delete(int id)
        {
            _cacheComandas.TryRemove(id, out _);

            if (IsApiMode)
            {
                try
                {
                    var url = ObterUrlEfetiva($"{GetApiRoute()}/{id}");
                    var response = await _client.DeleteAsync(url);
                    return response.IsSuccessStatusCode;
                }
                catch
                {
                    return true;
                }
            }

            try
            {
                return await base.Delete(id);
            }
            catch
            {
                return true;
            }
        }
    }
}
