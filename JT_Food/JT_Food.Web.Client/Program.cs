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

builder.Services.AddScoped(sp =>
{
    var baseUrl = builder.Configuration["Config:Conexao:BaseApiUrl"]
               ?? builder.Configuration["Config:FirebaseConfig:BaseApiUrl"]
               ?? "https://api.jtecsistemas.com.br";
    var httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };

    var configPayload = new JT_Aplicacao.Config.ClientConfigPayload { ClientId = "JT_Food" };
    if (JT_Aplicacao.Config.Config.Instancia != null)
    {
        JT_Aplicacao.Config.Config.Instancia.ClientId = configPayload.ClientId;
    }

    return httpClient;
});

builder.Services.AddScoped<JT_Classes.Acessos.ISecureStorageService, JT_Food.Shared.Services.WebSecureStorageService>();

builder.Services.AddJTUserInterface();

builder.Services.AddJTFoodServices();
await builder.Build().RunAsync();
