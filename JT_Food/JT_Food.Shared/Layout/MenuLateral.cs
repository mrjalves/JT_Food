using JT_Aplicacao.Classes;
using JT_Aplicacao.UserInterface;
using JT_UserInterface.Shared.Services.Interfaces.Comum;
using System.Collections.Generic;
using System.Linq;

namespace JT_Food.Shared.Layout
{
    public class MenuLateral : IModuloMenuProvider, IMenuProvider
    {
        public List<SModuloMenu> GetModulos()
        {
            return new List<SModuloMenu>
            {
                new SModuloMenu
                {
                    NomeModulo = "Restaurante",
                    Icone = "bi-shop",
                    Modulo = eModuloSistema.Restaurante,
                    Itens = new List<SItemMenu>
                    {
                        new SItemMenu { Titulo = "Home", Texto = "Home", Url = "/", Icone = "fas fa-home", MatchAll = true },
                        new SItemMenu { Titulo = "Comandas", Texto = "Comandas", Url = "/comandas", Icone = "fas fa-clipboard-list" },
                        new SItemMenu { Titulo = "KDS (Cozinha)", Texto = "KDS", Url = "/kds", Icone = "fas fa-tv" },
                        new SItemMenu { Titulo = "Menu", Texto = "Menu", Url = "/menu", Icone = "fas fa-utensils" },
                        new SItemMenu { Titulo = "Receitas", Texto = "Receitas", Url = "/receita", Icone = "fas fa-book-open" },
                        new SItemMenu { Titulo = "Estoque", Texto = "Estoque", Url = "/estoque", Icone = "fas fa-boxes" },
                        new SItemMenu { Titulo = "Produção", Texto = "Produção", Url = "/producao", Icone = "fas fa-blender" },
                        new SItemMenu { Titulo = "Caixa", Texto = "Caixa", Url = "/caixa", Icone = "fas fa-cash-register" }
                    }
                }
            };
        }

        public List<SItemMenu> GetMenu()
            => GetModulos().SelectMany(m => m.Itens).ToList();
    }
}
