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
                    NomeModulo = "JT_Food",
                    Icone = "bi-shop",
                    Modulo = eModuloSistema.Restaurante,
                    Itens = new List<SItemMenu>
                    {
                        new SItemMenu { Titulo = "Home", Texto = "Home", Url = "/", Icone = "bi-house-door", MatchAll = true },
                        new SItemMenu { Titulo = "Mesas", Texto = "Mesas", Url = "/mesas", Icone = "bi-table" },
                        new SItemMenu { Titulo = "Comandas", Texto = "Comandas", Url = "/comandas", Icone = "bi-card-checklist" },
                        new SItemMenu { Titulo = "KDS (Cozinha)", Texto = "KDS", Url = "/kds", Icone = "bi-display" },
                        new SItemMenu { Titulo = "Menu", Texto = "Menu", Url = "/menu", Icone = "bi-journal-text" },
                        new SItemMenu { Titulo = "Receitas", Texto = "Receitas", Url = "/receita", Icone = "bi-book" },
                        new SItemMenu { Titulo = "Estoque", Texto = "Estoque", Url = "/estoque", Icone = "bi-box-seam" },
                        new SItemMenu { Titulo = "Compras", Texto = "Compras", Url = "/compras", Icone = "bi-cart" },
                        new SItemMenu { Titulo = "Produção", Texto = "Produção", Url = "/producao", Icone = "bi-gear-wide-connected" },
                        new SItemMenu { Titulo = "Caixa", Texto = "Caixa", Url = "/caixa", Icone = "bi-cash-coin" }
                    }
                }
            };
        }

        public List<SItemMenu> GetMenu()
            => GetModulos().SelectMany(m => m.Itens).ToList();
    }
}
