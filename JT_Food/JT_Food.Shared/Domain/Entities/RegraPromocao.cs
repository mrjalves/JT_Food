using System;
using System.Collections.Generic;
using JT_Food.Shared.Domain.Enums;

namespace JT_Food.Shared.Domain.Entities
{
    public class RegraPromocao
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public TipoRegraPromocao Tipo { get; set; } = TipoRegraPromocao.DescontoDireto;
        
        // Produto individual afetado (se aplicável)
        public int? ProdutoId { get; set; }
        public string? NomeProduto { get; set; }
        
        // Produtos no caso de Combo
        public List<int> ProdutosComboIds { get; set; } = new();
        
        // Preço fixo ou % de desconto
        public double? PrecoPromocionalFixo { get; set; }
        public double? DescontoPercentual { get; set; }
        
        // Configuração de Happy Hour
        public List<int> DiasSemana { get; set; } = new() { 3, 4 }; // Quarta (3) e Quinta (4)
        public TimeSpan? HoraInicio { get; set; } = new TimeSpan(17, 0, 0); // 17h
        public TimeSpan? HoraFim { get; set; } = new TimeSpan(20, 0, 0);    // 20h
        
        public bool Ativa { get; set; } = true;

        public bool IsValidaAgora(DateTime? dataHoraReferencia = null)
        {
            if (!Ativa) return false;
            
            var agora = dataHoraReferencia ?? DateTime.Now;
            
            if (Tipo == TipoRegraPromocao.HappyHour)
            {
                int diaDaSemana = (int)agora.DayOfWeek;
                if (!DiasSemana.Contains(diaDaSemana)) return false;
                
                var horaAtual = agora.TimeOfDay;
                if (HoraInicio.HasValue && horaAtual < HoraInicio.Value) return false;
                if (HoraFim.HasValue && horaAtual > HoraFim.Value) return false;
            }

            return true;
        }
    }
}
