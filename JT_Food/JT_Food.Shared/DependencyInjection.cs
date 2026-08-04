using JT_Food.Shared.BusinessLogic;
using JT_Food.Shared.Layout;
using JT_Servicos.Interfaces;
using JT_Servicos.Restaurante;
using JT_UserInterface.Shared.Services.Interfaces.Comum;
using JT_UserInterface.Shared.Services.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace JT_Food.Shared
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddJTFoodServices(this IServiceCollection services)
        {
            services.AddScoped<IMenuProvider, MenuLateral>();
            services.AddScoped<IModuloMenuProvider, MenuLateral>();
            services.AddScoped<IConfiguracaoTabProvider, RestauranteConfigTabProvider>();

            // Serviços de Configuração
            services.AddScoped<MFoodConfiguracaoService>();
            services.AddScoped<IMFoodConfiguracaoService, MFoodConfiguracaoService>();

            // Serviços do Módulo Restaurante
            services.AddScoped<JT_Servicos.Restaurante.MesaService>();
            services.AddScoped<JT_Servicos.Restaurante.ComandaService>();
            services.AddScoped<JT_Servicos.Restaurante.ProdutoRestauranteService>();
            services.AddScoped<JT_Servicos.Restaurante.CompraRestauranteService>();
            services.AddScoped<JT_Servicos.Restaurante.ItemComandaService>();
            services.AddScoped<JT_Servicos.Restaurante.AdicionaisItemComandaService>();
            services.AddScoped<JT_Servicos.Restaurante.PagamentoComandaService>();
            services.AddScoped<JT_Servicos.Restaurante.CaixaService>();
            services.AddScoped<JT_Servicos.Restaurante.MovimentacaoCaixaService>();
            
            // Serviços de Estoque/Producao
            services.AddScoped<JT_Servicos.Restaurante.InsumoService>();
            services.AddScoped<JT_Servicos.Restaurante.LoteInsumoService>();
            services.AddScoped<JT_Servicos.Restaurante.ReceitaService>();
            services.AddScoped<JT_Servicos.Restaurante.ProducaoService>();
            services.AddScoped<JT_Servicos.Restaurante.MemoriaCalculoService>();

            // Logica de Negocios
            services.AddScoped<CaixaLogic>();
            services.AddScoped<ProducaoLogic>();
            services.AddScoped<KDSLogic>();
            services.AddScoped<ComercialLogic>();
            services.AddScoped<JT_Servicos.Restaurante.ItemCompraService>();

            return services;
        }
    }
}
