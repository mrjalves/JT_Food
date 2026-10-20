using System;
using System.Collections.Generic;
using JT_Food.Shared.Domain.Enums;

namespace JT_Food.Shared.Domain.Entities
{
    public class Mesa
    {
        public int Id { get; set; }
        public int AmbienteId { get; set; } = 1;
        public string Identificacao { get; set; } = string.Empty;
        public int Capacidade { get; set; } = 4;
        public string Status { get; set; } = StatusMesaConsts.Livre;
        public FormatoElementoSalao Formato { get; set; } = FormatoElementoSalao.MesaQuadrada;
        
        // Posição no salão (0% a 100%)
        public double PosX { get; set; } = 10;
        public double PosY { get; set; } = 10;
        public double Largura { get; set; } = 10;
        public double Altura { get; set; } = 10;
        public int Rotacao { get; set; } = 0;
        
        // Comandas vinculadas à mesa
        public List<Comanda> Comandas { get; set; } = new();
        public double Subtotal => Comandas.Sum(c => c.Total);
        public DateTime? UltimoPedidoEm { get; set; }
        public bool CouvertAtivo { get; set; } = false;
        public double ValorCouvertPorPessoa { get; set; } = 0.0;
        public int QuantidadePessoas { get; set; } = 2;
    }
}
