using JT_UserInterface.Shared.Models;
using JT_UserInterface.Shared.Services.Interfaces;
using JT_Food.Shared.Components;
using Microsoft.AspNetCore.Components;

namespace JT_Food.Shared.Services
{
    public class RestauranteConfigTabProvider : IConfiguracaoTabProvider
    {
        public IEnumerable<SConfiguracaoTab> GetTabs()
        {
            yield return new SConfiguracaoTab
            {
                Titulo = "Restaurante",
                Conteudo = builder =>
                {
                    builder.OpenComponent<RestauranteTab>(0);
                    builder.CloseComponent();
                },
                Ordem = 1 // Appear first after shared tabs
            };
        }
    }
}
