using JT_Aplicacao.Classes;
using JT_Aplicacao.Eventos;
using JT_Classes.Restaurante;
using System.Collections.Generic;

namespace JT_Food.Shared.Services
{
    public static class ComandaService
    {
        public static Comanda? GetById(int id)
        {
            return BaseRepository.GetById<Comanda>(id);
        }

        public static List<Comanda> GetAll()
        {
            return BaseRepository.GetAll<Comanda>();
        }

        public static List<Comanda> Find(string condicao)
        {
            return BaseRepository.Find<Comanda>(condicao);
        }

        public static List<Comanda> Find(Dictionary<string, object> filtros)
        {
            return BaseRepository.Find<Comanda>(filtros);
        }

        public static bool Save(Comanda entity)
        {
            var isNew = entity.Id == 0;
            var acao = isNew ? "Salvar" : "Atualizar";

            AcoplamentoRN.Executar(entity, acao, "Antes");

            bool result;
            if (isNew)
            {
                var newId = BaseRepository.Add(entity);
                entity.Id = newId;
                result = newId > 0;
            }
            else
            {
                result = BaseRepository.Update(entity);
            }

            AcoplamentoRN.Executar(entity, acao, "Depois");

            return result;
        }

        public static bool Delete(int id)
        {
            var entity = GetById(id);
            if (entity == null) return false;

            AcoplamentoRN.Executar(entity, "Excluir", "Antes");
            var result = BaseRepository.Delete<Comanda>(id);
            AcoplamentoRN.Executar(entity, "Excluir", "Depois");

            return result;
        }
    }
}
