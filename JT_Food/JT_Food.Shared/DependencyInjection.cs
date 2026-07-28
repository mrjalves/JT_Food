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
            services.AddScoped<IMenuProvider, MenuLateral>();
            services.AddScoped<IModuloMenuProvider, MenuLateral>();
            services.AddScoped<IConfiguracaoTabProvider, RestauranteConfigTabProvider>();

            // Registros de Serviços Core (JT_Servicos)
            
            // Registros de Wrappers UI

            return services;
        }
    }
}
