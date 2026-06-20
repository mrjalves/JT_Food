using JT_Aplicacao.Classes;
using JT_UserInterface.Shared.Services.Comum;
using JT_Aplicacao;
using JT_Atributos;

using JT_Classes.Comum;

using System.ComponentModel.DataAnnotations;

namespace JT_Food.Shared.Models
{
    [NomeTabela("food_configuracoes")]
    public class MConfiguracao : Configuracao
    {
        public MConfiguracao()
        {
        }

        [NomeColuna("NomeRestaurante")]
        public string NomeRestaurante { get; set; } = "JT Food";

        [NomeColuna("Endereco")]
        public string? Endereco { get; set; }

        [NomeColuna("Telefone")]
        public string? Telefone { get; set; }

        [NomeColuna("Cnpj")]
        public string? Cnpj { get; set; }

        [NomeColuna("TaxaServicoPadrao")]
        public double TaxaServicoPadrao { get; set; } = 10.0;

        [NomeColuna("PermiteEstoqueNegativo")]
        public bool PermiteEstoqueNegativo { get; set; } = false;

        [NomeColuna("LogoUrl")]
        public string? LogoUrl { get; set; }

        public override List<Filtro> CarregarFiltros() => [];
    }
}

