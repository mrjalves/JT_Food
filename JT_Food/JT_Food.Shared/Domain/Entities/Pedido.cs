using System;
using System.Collections.Generic;

namespace JT_Food.Shared.Domain.Entities
{
    public class Pedido
    {
        public int Id { get; set; }
        public int ComandaId { get; set; }
        public string? MesaId { get; set; }
        public string GarcomId { get; set; } = "1";
        public string NomeGarcom { get; set; } = "Garçom";
        public DateTime DataHora { get; set; } = DateTime.UtcNow;
        public string? ObservacaoGeral { get; set; }
        public List<ItemPedido> Itens { get; set; } = new();
        public double ValorTotal { get; set; } = 0.0;
        public string Status { get; set; } = "Enviado";
    }
}
