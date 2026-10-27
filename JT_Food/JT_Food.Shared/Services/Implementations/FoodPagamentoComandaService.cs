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
    public class FoodPagamentoComandaService : JT_Servicos.Restaurante.PagamentoComandaService
    {
        private readonly HttpClient _client;
        private static readonly ConcurrentDictionary<int, MPagamentoComanda> _cachePagamentos = new();
        private static int _nextPagamentoId = 5000;

        public FoodPagamentoComandaService(HttpClient? httpClient = null) : base(httpClient)
        {
            _client = httpClient ?? CriarFallbackHttpClient();

            try
            {
                var field = typeof(JT_Servicos.JTServiceBase<MPagamentoComanda>).GetField("_httpClient", BindingFlags.Instance | BindingFlags.NonPublic);
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

        public override async Task<List<MPagamentoComanda>> GetAll()
        {
            var resultado = new Dictionary<int, MPagamentoComanda>();

            if (IsApiMode)
            {
                try
                {
                    var url = ObterUrlEfetiva(GetApiRoute());
                    var response = await _client.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        var remotos = await response.Content.ReadFromJsonAsync<List<MPagamentoComanda>>();
                        if (remotos != null)
                        {
                            foreach (var r in remotos.Where(p => p.Id > 0))
                            {
                                resultado[r.Id] = r;
                                _cachePagamentos[r.Id] = r;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[FoodPagamentoComandaService GetAll Error]: {ex.Message}");
                }
            }
            else
            {
                try
                {
                    var baseList = await base.GetAll();
                    if (baseList != null)
                    {
                        foreach (var b in baseList.Where(p => p.Id > 0))
                        {
                            resultado[b.Id] = b;
                            _cachePagamentos[b.Id] = b;
                        }
                    }
                }
                catch { }
            }

            // Mesclar dados do cache local
            foreach (var kv in _cachePagamentos)
            {
                if (!resultado.ContainsKey(kv.Key))
                {
                    resultado[kv.Key] = kv.Value;
                }
            }

            return resultado.Values.OrderByDescending(p => p.Id).ToList();
        }

        public override async Task<MPagamentoComanda?> GetById(int id)
        {
            if (_cachePagamentos.TryGetValue(id, out var local))
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
                        var remoto = await response.Content.ReadFromJsonAsync<MPagamentoComanda>();
                        if (remoto != null)
                        {
                            _cachePagamentos[id] = remoto;
                            return remoto;
                        }
                    }
                }
                catch { }
                return null;
            }

            return await base.GetById(id);
        }

        public override async Task<bool> Save(MPagamentoComanda entity)
        {
            var id = await SaveAsync(entity);
            return id > 0;
        }

        public override async Task<int> SaveAsync(MPagamentoComanda entity)
        {
            bool isNew = entity.Id <= 0;
            if (isNew)
            {
                entity.Id = System.Threading.Interlocked.Increment(ref _nextPagamentoId);
            }

            _cachePagamentos[entity.Id] = entity;

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
                                var created = await response.Content.ReadFromJsonAsync<MPagamentoComanda>();
                                if (created != null && created.Id > 0)
                                {
                                    if (created.Id != entity.Id)
                                    {
                                        _cachePagamentos.TryRemove(entity.Id, out _);
                                        entity.Id = created.Id;
                                        _cachePagamentos[entity.Id] = entity;
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
                    Console.WriteLine($"[FoodPagamentoComandaService SaveAsync Exception]: {ex.Message}");
                }
            }
            else
            {
                try
                {
                    var baseResult = await base.SaveAsync(entity);
                    if (baseResult > 0)
                    {
                        _cachePagamentos[entity.Id] = entity;
                        return baseResult;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[FoodPagamentoComandaService SaveAsync Base Error]: {ex.Message}");
                }
            }

            return entity.Id;
        }

        public override async Task<bool> Delete(int id)
        {
            _cachePagamentos.TryRemove(id, out _);

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

            return await base.Delete(id);
        }
    }
}
