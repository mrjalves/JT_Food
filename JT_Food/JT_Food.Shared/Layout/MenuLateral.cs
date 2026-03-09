using JT_Aplicacao;
using JT_UserInterface.Shared.Services.Interfaces;

namespace JT_Food.Shared.Layout
{
    public class MenuLateral : IMenuProvider
    {
        public List<SItemMenu> GetMenu()
        {
            return new List<SItemMenu>
            {
                new SItemMenu { Titulo = "Home", Texto = "Home", Url = "/", Icone = "fas fa-home", MatchAll = true },
                new SItemMenu { Titulo = "Configurar Restaurante", Texto = "Geral", Url = "/configuracao", Icone = "fas fa-cogs" },
                new SItemMenu { Titulo = "Menu", Texto = "Menu", Url = "/menu", Icone = "fas fa-utensils" },
                new SItemMenu { Titulo = "Receitas", Texto = "Receitas", Url = "/receita", Icone = "fas fa-book-open" },
                new SItemMenu { Titulo = "Estoque", Texto = "Estoque", Url = "/estoque", Icone = "fas fa-boxes" },
                new SItemMenu { Titulo = "Comandas", Texto = "Comandas", Url = "/comandas", Icone = "fas fa-clipboard-list" },
                new SItemMenu { Titulo = "Produção", Texto = "Produção", Url = "/producao", Icone = "fas fa-blender" },
                new SItemMenu { Titulo = "Caixa", Texto = "Caixa", Url = "/caixa", Icone = "fas fa-cash-register" },
                new SItemMenu { Titulo = "KDS (Cozinha)", Texto = "KDS", Url = "/kds", Icone = "fas fa-tv" },
                new SItemMenu { Titulo = "Perfil", Texto = "Perfil", Url = "/perfil", Icone = "bi-person-circle" }
            };
        }
    }
}
