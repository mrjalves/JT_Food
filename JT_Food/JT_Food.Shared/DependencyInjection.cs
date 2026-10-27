using JT_Food.Shared.BusinessLogic;
using JT_Food.Shared.Layout;
using JT_Food.Shared.Providers;
using JT_Food.Shared.Services.Implementations;
using JT_Food.Shared.Services.Interfaces;
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
            services.AddScoped<ITutorialProvider, FoodTutorialProvider>();

            // Serviços de Configuração
            services.AddScoped<MFoodConfiguracaoService>();
            services.AddScoped<IMFoodConfiguracaoService, MFoodConfiguracaoService>();

            // Serviços do Módulo Restaurante (Banco / API)
            services.AddScoped<JT_Servicos.Restaurante.MesaService>();
            services.AddScoped<FoodComandaService>();
            services.AddScoped<JT_Servicos.Restaurante.ComandaService>(sp => sp.GetRequiredService<FoodComandaService>());
            services.AddScoped<JT_Servicos.Restaurante.ProdutoRestauranteService>();
            services.AddScoped<JT_Servicos.Restaurante.CompraRestauranteService>();
            services.AddScoped<FoodItemComandaService>();
            services.AddScoped<JT_Servicos.Restaurante.ItemComandaService>(sp => sp.GetRequiredService<FoodItemComandaService>());
            services.AddScoped<FoodAdicionaisItemComandaService>();
            services.AddScoped<JT_Servicos.Restaurante.AdicionaisItemComandaService>(sp => sp.GetRequiredService<FoodAdicionaisItemComandaService>());
            services.AddScoped<FoodPagamentoComandaService>();
            services.AddScoped<JT_Servicos.Restaurante.PagamentoComandaService>(sp => sp.GetRequiredService<FoodPagamentoComandaService>());
            services.AddScoped<FoodCaixaService>();
            services.AddScoped<JT_Servicos.Restaurante.CaixaService>(sp => sp.GetRequiredService<FoodCaixaService>());
            services.AddScoped<FoodMovimentacaoCaixaService>();
            services.AddScoped<JT_Servicos.Restaurante.MovimentacaoCaixaService>(sp => sp.GetRequiredService<FoodMovimentacaoCaixaService>());
            
            // Serviços de Estoque/Producao
            services.AddScoped<JT_Servicos.Restaurante.InsumoService>();
            services.AddScoped<JT_Servicos.Restaurante.LoteInsumoService>();
            services.AddScoped<JT_Servicos.Restaurante.ReceitaService>();
            services.AddScoped<JT_Servicos.Restaurante.ProducaoService>();
            services.AddScoped<JT_Servicos.Restaurante.MemoriaCalculoService>();
            services.AddScoped<JT_Servicos.Restaurante.ItemCompraService>();

            // Lógica de Negócios
            services.AddScoped<PromocaoLogic>();
            services.AddScoped<LayoutSalaoLogic>();
            services.AddScoped<CaixaLogic>();
            services.AddScoped<ProducaoLogic>();
            services.AddScoped<KDSLogic>();
            services.AddScoped<ComercialLogic>();
            services.AddScoped<RelatorioIALogic>();

            // Serviços Operacionais JT_Food Core
            services.AddScoped<IComandaService, ComandaOperacionalService>();
            services.AddScoped<ICardapioService, CardapioService>();
            services.AddScoped<IMesaLayoutService, MesaLayoutService>();
            services.AddScoped<IImpressaoRedeService, EscPosPrintService>();
            services.AddScoped<IAuthPerfilService, AuthPerfilService>();
            services.AddScoped<IIAAnalyticsService, GeminiRelatorioService>();

            // Serviços de IA (Copiloto & Assistente Integrado)
            services.AddScoped<FoodConfiguracaoIAService>();
            services.AddScoped<JT_Servicos.IA.ConfiguracaoIAService>(sp => sp.GetRequiredService<FoodConfiguracaoIAService>());
            services.AddScoped<JT_Servicos.IA.InstrutorIAService>();
            services.AddScoped<FoodInstrutorIAService>();
            services.AddScoped<JT_Servicos.IA.Interfaces.IInstrutorIAService>(sp => sp.GetRequiredService<FoodInstrutorIAService>());

            return services;
        }
    }
}
