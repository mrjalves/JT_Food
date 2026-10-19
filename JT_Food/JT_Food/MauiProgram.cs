using System;
using JT_Aplicacao;
using JT_Aplicacao.Config;
using JT_Classes.Acessos;
using JT_Food.Shared;
using JT_Food.Shared.Layout;
using JT_Food.Shared.Services;
using JT_UserInterface.Shared.Services;
using JT_UserInterface.Shared.Services.Interfaces.Comum;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Reflection;
using System.Net.Http;

namespace JT_Food
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            try
            {
                var builder = MauiApp.CreateBuilder();
                builder
                    .UseMauiApp<App>()
                    .ConfigureFonts(fonts =>
                    {
                        fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    });

                builder.Services.AddMauiBlazorWebView();
                
                // 1. Carregamento do appsettings.json
                var assembly = Assembly.GetExecutingAssembly();
                using var stream = assembly.GetManifestResourceStream("JT_Food.appsettings.json");

                if (stream != null)
                {
                    var configRoot = new ConfigurationBuilder().AddJsonStream(stream).Build();
                    builder.Configuration.AddConfiguration(configRoot);

                    var secaoConfig = configRoot.GetSection("Config");
                    var dadosConfig = secaoConfig.Get<Config>();

                    if (dadosConfig != null)
                    {
                        Config.SetInstancia(dadosConfig);
                        builder.Services.AddSingleton(Config.Instancia);
                    }
                }
                else
                {
                    Console.WriteLine("AVISO: appsettings.json não encontrado.");
                }

                // 2. Configuração do Firebase via IOptions
                var secaoFConfig = builder.Configuration.GetSection("Config:FirebaseConfig");
                var dadosFConfig = secaoFConfig.Get<FirebaseConfig>();

                if (dadosFConfig != null)
                {
                    FirebaseConfig.SetInstancia(dadosFConfig);
                    builder.Services.AddSingleton(FirebaseConfig.Instancia);
                }

                // 3. HttpClient com ajuste para Android Localhost
                builder.Services.AddScoped(sp =>
                {
                    var baseAddressUrl = FirebaseConfig.Instancia?.BaseApiUrl ?? "https://api.jtecsistemas.com.br";

                    if (DeviceInfo.Platform == DevicePlatform.Android && baseAddressUrl.Contains("localhost"))
                    {
                        baseAddressUrl = baseAddressUrl.Replace("localhost", "10.0.2.2");
                    }

                    var httpClient = new HttpClient { BaseAddress = new Uri(baseAddressUrl) };
                    
                    var configPayload = new ClientConfigPayload { ClientId = "JT_Food" };
                    if (JT_Aplicacao.Config.Config.Instancia != null)
                    {
                        JT_Aplicacao.Config.Config.Instancia.ClientId = configPayload.ClientId;
                    }
                    var json = System.Text.Json.JsonSerializer.Serialize(configPayload);
                    var encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));
                    httpClient.DefaultRequestHeaders.Add("X-Client-Config", encoded);

                    return httpClient;
                });

                // 4. Serviços Globais
                JT_UserInterface.Shared.DependencyInjection.AddJTUserInterface(builder.Services);
                builder.Services.AddSingleton<ISecureStorageService, Services.MauiSecureStorageService>();

                // 5. Serviços do JT_Food
                builder.Services.AddJTFoodServices();

#if DEBUG
                builder.Services.AddBlazorWebViewDeveloperTools();
                builder.Logging.AddDebug();
#endif

                return builder.Build();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao criar o MauiApp: {ex.Message}");
                throw;
            }
        }
    }
}

