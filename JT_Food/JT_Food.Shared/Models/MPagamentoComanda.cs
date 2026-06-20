using JT_Aplicacao.Classes;
using JT_UserInterface.Shared.Services.Comum;
using JT_Aplicacao;
using JT_Atributos;
using System.ComponentModel.DataAnnotations;

namespace JT_Food.Shared.Models
{
    /// <summary>
    /// Representa um pagamento parcial ou total aplicado a uma comanda.
    /// Permite rateio/divisão de conta com múltiplas formas de pagamento.
    /// </summary>
    [NomeTabela("food_pagamentos_comanda")]
    public class MPagamentoComanda : ClasseBase
    {
        [NomeColuna("UserId")]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [NomeColuna("ComandaId")]
        public string ComandaId { get; set; } = string.Empty;

        // FormaPagamento: "Dinheiro", "PIX", "CartaoCredito", "CartaoDebito"
        [Required]
        [NomeColuna("FormaPagamento")]
        public string FormaPagamento { get; set; } = "Dinheiro";

        [Required]
        [NomeColuna("Valor")]
        public double Valor { get; set; } = 0.0;

        [NomeColuna("OperadorId")]
        public string? OperadorId { get; set; }

        [NomeColuna("DataHora")]
        public DateTime DataHora { get; set; } = DateTime.UtcNow;

        [NomeColuna("Observacao")]
        public string? Observacao { get; set; }

        public override List<Filtro> CarregarFiltros() => [];
    }
}

