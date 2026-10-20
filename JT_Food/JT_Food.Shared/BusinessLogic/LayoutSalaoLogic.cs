using System;
using System.Collections.Generic;
using System.Linq;
using JT_Food.Shared.Domain.Entities;
using JT_Food.Shared.Domain.Enums;

namespace JT_Food.Shared.BusinessLogic
{
    public class LayoutSalaoLogic
    {
        public ElementoSalao CriarNovoElemento(int ambienteId, FormatoElementoSalao formato, string identificacao, double posX = 20, double posY = 20)
        {
            var (largura, altura, capacidade) = ObterDimensoesPadrao(formato);

            return new ElementoSalao
            {
                AmbienteId = ambienteId,
                Identificacao = identificacao,
                Formato = formato,
                PosX = Math.Clamp(posX, 0, 90),
                PosY = Math.Clamp(posY, 0, 90),
                Largura = largura,
                Altura = altura,
                Rotacao = 0,
                Capacidade = capacidade,
                Status = StatusMesaConsts.Livre
            };
        }

        public (double Largura, double Altura, int Capacidade) ObterDimensoesPadrao(FormatoElementoSalao formato)
        {
            return formato switch
            {
                FormatoElementoSalao.MesaQuadrada => (11, 14, 4),
                FormatoElementoSalao.MesaRedonda => (13, 17, 6),
                FormatoElementoSalao.MesaRetangular => (18, 14, 8),
                FormatoElementoSalao.BalcaoBar => (22, 40, 8),
                FormatoElementoSalao.PalcoCouvert => (24, 15, 0),
                FormatoElementoSalao.Caixa => (18, 12, 0),
                FormatoElementoSalao.Divisoria => (2, 25, 0),
                _ => (12, 14, 4)
            };
        }

        public void AjustarPosicaoComSnap(ElementoSalao elemento, double novoX, double novoY, double passoSnap = 2.0)
        {
            if (elemento == null) return;

            // Snap to grid
            double xSnap = Math.Round(novoX / passoSnap) * passoSnap;
            double ySnap = Math.Round(novoY / passoSnap) * passoSnap;

            // Clamp dentro dos limites do salão
            elemento.PosX = Math.Clamp(xSnap, 0, 100 - elemento.Largura);
            elemento.PosY = Math.Clamp(ySnap, 0, 100 - elemento.Altura);
        }

        public void RotacionarElemento(ElementoSalao elemento)
        {
            if (elemento == null) return;
            elemento.Rotacao = (elemento.Rotacao + 90) % 360;
            
            // Inverter largura e altura ao girar 90 ou 270 graus se for retangular ou balcão
            if (elemento.Formato == FormatoElementoSalao.MesaRetangular || 
                elemento.Formato == FormatoElementoSalao.BalcaoBar ||
                elemento.Formato == FormatoElementoSalao.Divisoria)
            {
                (elemento.Largura, elemento.Altura) = (elemento.Altura, elemento.Largura);
            }
        }
    }
}
