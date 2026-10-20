using System;
using System.Collections.Generic;

namespace JT_Food.Shared.Domain.Entities
{
    public class Comanda
    {
        public int Id { get; set; }
        public string? NumeroCartao { get; set; } // ex: "105"
        public string? NomeCliente { get; set; }
        public string? MesaId { get; set; }
        public string Status { get; set; } = "Aberta";
        public DateTime DataAbertura { get; set; } = DateTime.UtcNow;
        public DateTime? DataFechamento { get; set; }
        public double Subtotal { get; set; } = 0.0;
        public double TaxaServico { get; set; } = 0.0;
        public double Desconto { get; set; } = 0.0;
        public bool PossuiCouvert { get; set; } = false;
        public double ValorCouvert { get; set; } = 0.0;
        public int QuantidadePessoas { get; set; } = 1;
        public string? GarcomId { get; set; }
        public double Total => Math.Max(0, Subtotal + TaxaServico + (PossuiCouvert ? (ValorCouvert * Math.Max(1, QuantidadePessoas)) : 0) - Desconto);
        public List<ItemPedido> Itens { get; set; } = new();

        public string DisplayIdentificacao
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(NumeroCartao) && !string.IsNullOrWhiteSpace(NomeCliente))
                    return $"#{NumeroCartao} - {NomeCliente}";
                if (!string.IsNullOrWhiteSpace(NumeroCartao))
                    return $"Comanda #{NumeroCartao}";
                if (!string.IsNullOrWhiteSpace(NomeCliente))
                    return $"{NomeCliente} (s/n)";
                return $"Comanda #{Id}";
            }
        }
    }
}
