using JT_Aplicacao;

using JT_Classes.Acessos;

using JT_Food.Shared.Layout;
using JT_Food.Shared.Services;

using JT_UserInterface.Shared.Services;
using JT_UserInterface.Shared.Services.Interfaces;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using System.Reflection;

namespace JT_Food
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
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
            // IMPORTANTE: Verifique se o nome do recurso está correto (Namespace.Arquivo)
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

                    // 2. Configuração do Firebase via IOptions

                    if (Config.Instancia.FirebaseConfig != null)
                    {
                        FirebaseConfig.SetInstancia(Config.Instancia.FirebaseConfig);
                        builder.Services.AddSingleton(FirebaseConfig.Instancia);
                        builder.Services.Configure<FirebaseConfig>(configRoot.GetSection("Config:FirebaseConfig"));
                    }
                }

            }
            else
            {
                Console.WriteLine("AVISO: appsettings.json não encontrado.");
            }

            // 5. HttpClient com ajuste para Android Localhost
            builder.Services.AddScoped(sp =>
            {
                var baseAddressUrl = FirebaseConfig.Instancia.BaseApiUrl ?? "https://localhost:7166";

                if (DeviceInfo.Platform == DevicePlatform.Android && baseAddressUrl.Contains("localhost"))
                {
                    baseAddressUrl = baseAddressUrl.Replace("localhost", "10.0.2.2");
                }

                return new HttpClient { BaseAddress = new Uri(baseAddressUrl) };
            });

            // 3. Serviços Singleton
            JT_UserInterface.Shared.DependencyInjection.AddJTUserInterface(builder.Services);

            builder.Services.AddScoped<SUsuarioRepository>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<FirebaseConfig>>();
                var config = options.Value;
                var userState = sp.GetRequiredService<CurrentUserState>();
                var httpClient = sp.GetRequiredService<HttpClient>();
                var dbUrl = string.IsNullOrEmpty(config.DatabaseUrl) ? "https://config-missing.firebaseio.com" : config.DatabaseUrl;
                return new SUsuarioRepository(dbUrl, userState, httpClient);
            });

            // 4. Serviços Scoped
            builder.Services.AddScoped<IMenuProvider, MenuLateral>();
            builder.Services.AddScoped<IConfiguracaoTabProvider, RestauranteConfigTabProvider>();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
