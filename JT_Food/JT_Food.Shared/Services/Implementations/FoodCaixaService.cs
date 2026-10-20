using System;
using System.Collections.Generic;
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
            if (IsApiMode)
            {
                try
                {
                    var url = ObterUrlEfetiva(GetApiRoute());
                    var response = await _client.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        return await response.Content.ReadFromJsonAsync<List<MCaixa>>() ?? new List<MCaixa>();
                    }
                    return new List<MCaixa>();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[FoodCaixaService GetAll Error]: {ex.Message}");
                    return new List<MCaixa>();
                }
            }

            return await base.GetAll();
        }

        public override async Task<MCaixa?> GetById(int id)
        {
            if (IsApiMode)
            {
                try
                {
                    var url = ObterUrlEfetiva($"{GetApiRoute()}/{id}");
                    var response = await _client.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        return await response.Content.ReadFromJsonAsync<MCaixa>();
                    }
                    return null;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[FoodCaixaService GetById Error]: {ex.Message}");
                    return null;
                }
            }

            return await base.GetById(id);
        }

        public override async Task<bool> Save(MCaixa entity)
        {
            if (IsApiMode)
            {
                var id = entity.Id;
                bool isNew = id == 0;
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
                                    entity.Id = created.Id;
                                }
                            }
                            catch { }
                            return true;
                        }
                        return false;
                    }
                    else
                    {
                        var url = ObterUrlEfetiva($"{GetApiRoute()}/{id}");
                        var response = await _client.PutAsJsonAsync(url, entity);
                        return response.IsSuccessStatusCode;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[FoodCaixaService Save Error]: {ex.Message}");
                    return false;
                }
            }

            return await base.Save(entity);
        }

        public override async Task<int> SaveAsync(MCaixa entity)
        {
            if (IsApiMode)
            {
                var id = entity.Id;
                bool isNew = id == 0;
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
                                    entity.Id = created.Id;
                                    return created.Id;
                                }
                            }
                            catch { }
                            return 1;
                        }
                        return 0;
                    }
                    else
                    {
                        var url = ObterUrlEfetiva($"{GetApiRoute()}/{id}");
                        var response = await _client.PutAsJsonAsync(url, entity);
                        return response.IsSuccessStatusCode ? id : 0;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[FoodCaixaService SaveAsync Error]: {ex.Message}");
                    return 0;
                }
            }

            return await base.SaveAsync(entity);
        }

        public override async Task<bool> Delete(int id)
        {
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
                    return false;
                }
            }

            return await base.Delete(id);
        }
    }

    public class FoodMovimentacaoCaixaService : JT_Servicos.Restaurante.MovimentacaoCaixaService
    {
        private readonly HttpClient _client;

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
            if (IsApiMode)
            {
                try
                {
                    var url = ObterUrlEfetiva(GetApiRoute());
                    var response = await _client.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        return await response.Content.ReadFromJsonAsync<List<MMovimentacaoCaixa>>() ?? new List<MMovimentacaoCaixa>();
                    }
                    return new List<MMovimentacaoCaixa>();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[FoodMovimentacaoCaixaService GetAll Error]: {ex.Message}");
                    return new List<MMovimentacaoCaixa>();
                }
            }

            return await base.GetAll();
        }

        public override async Task<MMovimentacaoCaixa?> GetById(int id)
        {
            if (IsApiMode)
            {
                try
                {
                    var url = ObterUrlEfetiva($"{GetApiRoute()}/{id}");
                    var response = await _client.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        return await response.Content.ReadFromJsonAsync<MMovimentacaoCaixa>();
                    }
                    return null;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[FoodMovimentacaoCaixaService GetById Error]: {ex.Message}");
                    return null;
                }
            }

            return await base.GetById(id);
        }

        public override async Task<bool> Save(MMovimentacaoCaixa entity)
        {
            if (IsApiMode)
            {
                var id = entity.Id;
                bool isNew = id == 0;
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
                                    entity.Id = created.Id;
                                }
                            }
                            catch { }
                            return true;
                        }
                        return false;
                    }
                    else
                    {
                        var url = ObterUrlEfetiva($"{GetApiRoute()}/{id}");
                        var response = await _client.PutAsJsonAsync(url, entity);
                        return response.IsSuccessStatusCode;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[FoodMovimentacaoCaixaService Save Error]: {ex.Message}");
                    return false;
                }
            }

            return await base.Save(entity);
        }

        public override async Task<int> SaveAsync(MMovimentacaoCaixa entity)
        {
            if (IsApiMode)
            {
                var id = entity.Id;
                bool isNew = id == 0;
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
                                    entity.Id = created.Id;
                                    return created.Id;
                                }
                            }
                            catch { }
                            return 1;
                        }
                        return 0;
                    }
                    else
                    {
                        var url = ObterUrlEfetiva($"{GetApiRoute()}/{id}");
                        var response = await _client.PutAsJsonAsync(url, entity);
                        return response.IsSuccessStatusCode ? id : 0;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[FoodMovimentacaoCaixaService SaveAsync Error]: {ex.Message}");
                    return 0;
                }
            }

            return await base.SaveAsync(entity);
        }

        public override async Task<bool> Delete(int id)
        {
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
                    return false;
                }
            }

            return await base.Delete(id);
        }
    }
}
