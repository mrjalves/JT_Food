using System.Collections.Generic;
using System.Linq;
using JT_Aplicacao.Classes;
using JT_Aplicacao.UserInterface;
using JT_Food.Shared.Domain.Enums;
using JT_Food.Shared.Services.Interfaces;
using JT_UserInterface.Shared.Services.Interfaces.Comum;

namespace JT_Food.Shared.Layout
{
    public class MenuLateral : IModuloMenuProvider, IMenuProvider
    {
        private readonly IAuthPerfilService _authService;

        public MenuLateral(IAuthPerfilService authService)
        {
            _authService = authService;
        }

        public List<SModuloMenu> GetModulos()
        {
            var perfil = _authService.ObterPerfilAtual();
            var itens = new List<SItemMenu>();

            if (perfil == PerfilAcesso.Garcom)
            {
                // Interface do Garçom: Acessos focados no atendimento rápido móvel + Caixa
                itens.Add(new SItemMenu { Titulo = "Mesas", Texto = "Mesas", Url = "/mesas", Icone = "bi-table" });
                itens.Add(new SItemMenu { Titulo = "Comandas", Texto = "Comandas", Url = "/comandas", Icone = "bi-card-checklist" });
                itens.Add(new SItemMenu { Titulo = "KDS (Cozinha)", Texto = "KDS", Url = "/kds", Icone = "bi-display" });
                itens.Add(new SItemMenu { Titulo = "Caixa", Texto = "Caixa", Url = "/caixa", Icone = "bi-cash-coin" });
            }
            else if (perfil == PerfilAcesso.Caixa)
            {
                // Interface do Caixa: Fechamentos, mesas e comandas
                itens.Add(new SItemMenu { Titulo = "Home", Texto = "Home", Url = "/", Icone = "bi-house-door", MatchAll = true });
                itens.Add(new SItemMenu { Titulo = "Mesas", Texto = "Mesas", Url = "/mesas", Icone = "bi-table" });
                itens.Add(new SItemMenu { Titulo = "Comandas", Texto = "Comandas", Url = "/comandas", Icone = "bi-card-checklist" });
                itens.Add(new SItemMenu { Titulo = "Caixa", Texto = "Caixa", Url = "/caixa", Icone = "bi-cash-coin" });
            }
            else
            {
                // Interface do Gerente: Painel administrativo e operacional
                itens.Add(new SItemMenu { Titulo = "Dashboard IA", Texto = "Home", Url = "/", Icone = "bi-house-door", MatchAll = true });
                itens.Add(new SItemMenu { Titulo = "Mesas & Salão", Texto = "Mesas", Url = "/mesas", Icone = "bi-table" });
                itens.Add(new SItemMenu { Titulo = "Comandas", Texto = "Comandas", Url = "/comandas", Icone = "bi-card-checklist" });
                itens.Add(new SItemMenu { Titulo = "KDS (Cozinha)", Texto = "KDS", Url = "/kds", Icone = "bi-display" });
                itens.Add(new SItemMenu { Titulo = "Cardápio & Promoções", Texto = "Menu", Url = "/menu", Icone = "bi-journal-text" });
                itens.Add(new SItemMenu { Titulo = "Caixa", Texto = "Caixa", Url = "/caixa", Icone = "bi-cash-coin" });
            }

            return new List<SModuloMenu>
            {
                new SModuloMenu
                {
                    NomeModulo = "JT_Food",
                    Icone = "bi-shop",
                    Modulo = eModuloSistema.Restaurante,
                    Itens = itens
                }
            };
        }

        public List<SItemMenu> GetMenu()
            => GetModulos().SelectMany(m => m.Itens).ToList();
    }
}
