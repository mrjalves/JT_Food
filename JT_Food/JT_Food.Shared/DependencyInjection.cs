using JT_Aplicacao.Config;
using JT_Classes.Acessos;
using JT_Food.Shared.Layout;
using JT_Food.Shared.Services;
using JT_UserInterface.Shared.Services.Interfaces.Comum;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net.Http;

namespace JT_Food.Shared
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddJTFoodServices(this IServiceCollection services)
        {
            services.AddScoped<SUsuarioRepository>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<FirebaseConfig>>();
                var config = options.Value;
                var userState = sp.GetRequiredService<CurrentUserState>();
                var httpClient = sp.GetRequiredService<HttpClient>();
                var dbUrl = string.IsNullOrEmpty(config.DatabaseUrl) ? "https://config-missing.firebaseio.com" : config.DatabaseUrl;
                return new SUsuarioRepository(dbUrl, userState, httpClient);
            });

            services.AddScoped<IMenuProvider, MenuLateral>();
            services.AddScoped<IModuloMenuProvider, MenuLateral>();
            services.AddScoped<IConfiguracaoTabProvider, RestauranteConfigTabProvider>();

            return services;
        }
    }
}
