using JT_Aplicacao.Classes;
using JT_UserInterface.Shared.Services.Comum;
using JT_Aplicacao;
using JT_Atributos;
using System.ComponentModel.DataAnnotations;

namespace JT_Food.Shared.Models
{
    [NomeTabela("food_comandas")]
    public class MComanda : ClasseBase
    {
        [NomeColuna("UserId")]
        public string UserId { get; set; } = string.Empty;

        [NomeColuna("MesaId")]
        public string? MesaId { get; set; }

        [NomeColuna("NomeCliente")]
        public string? NomeCliente { get; set; }

        [Required]
        [NomeColuna("DataAbertura")]
        public DateTime DataAbertura { get; set; } = DateTime.UtcNow;

        [NomeColuna("DataFechamento")]
        public DateTime? DataFechamento { get; set; }

        // Status: "Aberta", "Fechada", "Cancelada"
        [NomeColuna("Status")]
        public string Status { get; set; } = "Aberta";

        [NomeColuna("Total")]
        public double Total { get; set; } = 0.0;
        
        [NomeColuna("TaxaServico")]
        public double TaxaServico { get; set; } = 0.0;
        
        [NomeColuna("Desconto")]
        public double Desconto { get; set; } = 0.0;

        [NomeColuna("GarcomId")]
        public string? GarcomId { get; set; }

        public override List<Filtro> CarregarFiltros() => [];
    }
}

