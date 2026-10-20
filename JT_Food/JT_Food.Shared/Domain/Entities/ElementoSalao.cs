using System;
using System.Collections.Generic;
using JT_Food.Shared.Domain.Enums;

namespace JT_Food.Shared.Domain.Entities
{
    public class ElementoSalao
    {
        public int Id { get; set; }
        public int AmbienteId { get; set; } = 1;
        public int? MesaDbId { get; set; }
        public string Identificacao { get; set; } = "Mesa";
        public FormatoElementoSalao Formato { get; set; } = FormatoElementoSalao.MesaQuadrada;
        
        // Coordenadas percentuais (0% a 100%)
        public double PosX { get; set; } = 10;
        public double PosY { get; set; } = 10;
        public double Largura { get; set; } = 10;
        public double Altura { get; set; } = 10;
        public int Rotacao { get; set; } = 0;
        
        public int Capacidade { get; set; } = 4;
        public string Status { get; set; } = StatusMesaConsts.Livre;
        
        // Informações em tempo real
        public int TotalComandasAtivas { get; set; } = 0;
        public double SubtotalAcumulado { get; set; } = 0.0;
        public DateTime? UltimoPedidoEm { get; set; }
        public int QuantidadePessoas { get; set; } = 0;
        public bool PossuiCouvert { get; set; } = false;
        
        public bool IsAlertaInativo(int minutosLimite = 20)
        {
            if (Status != StatusMesaConsts.Ocupada || UltimoPedidoEm == null)
                return false;

            return (DateTime.UtcNow - UltimoPedidoEm.Value).TotalMinutes >= minutosLimite;
        }
    }
}
