using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using JT_Aplicacao;
using JT_Aplicacao.Config;
using JT_Classes.Acessos;
using JT_UserInterface.Shared;
using JT_Food.Shared;
var builder = WebAssemblyHostBuilder.CreateDefault(args);

var secaoConfig = builder.Configuration.GetSection("Config");
var dadosConfig = secaoConfig.Get<Config>();
if (dadosConfig != null)
{
    Config.SetInstancia(dadosConfig);
}

var secaoFirebase = builder.Configuration.GetSection("Config:FirebaseConfig");
var dadosFConfig = secaoFirebase.Get<FirebaseConfig>();
if (dadosFConfig != null)
{
    FirebaseConfig.SetInstancia(dadosFConfig);
    builder.Services.AddSingleton(FirebaseConfig.Instancia);
}

builder.Services.AddScoped<JT_Classes.Acessos.ISecureStorageService, JT_Food.Shared.Services.WebSecureStorageService>();

builder.Services.AddJTUserInterface();

builder.Services.AddHttpClient();
builder.Services.AddHttpClient("GeminiCleanClient");

builder.Services.AddScoped(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = factory.CreateClient(string.Empty);

    var baseUrl = builder.Configuration["Config:Conexao:BaseApiUrl"]
               ?? builder.Configuration["Config:FirebaseConfig:BaseApiUrl"]
               ?? "https://api.jtecsistemas.com.br";
    httpClient.BaseAddress = new Uri(baseUrl);

    var configPayload = new JT_Aplicacao.Config.ClientConfigPayload { ClientId = "JT_Food" };
    if (JT_Aplicacao.Config.Config.Instancia != null)
    {
        JT_Aplicacao.Config.Config.Instancia.ClientId = configPayload.ClientId;
    }

    var json = System.Text.Json.JsonSerializer.Serialize(configPayload);
    var encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));
    if (!httpClient.DefaultRequestHeaders.Contains("X-Client-Config"))
    {
        httpClient.DefaultRequestHeaders.Add("X-Client-Config", encoded);
    }

    return httpClient;
});

builder.Services.AddJTFoodServices();
await builder.Build().RunAsync();
