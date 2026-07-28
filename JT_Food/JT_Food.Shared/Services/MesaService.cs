using JT_Aplicacao.Classes;
using JT_Aplicacao.Eventos;
using JT_Aplicacao;
using JT_Classes.Acessos;
using JT_Classes.Restaurante;

namespace JT_Food.Shared.Services
{
    public static class MesaService
    {
        public static Mesa? GetById(int id)
        {
            return BaseRepository.GetById<Mesa>(id);
        }

        public static List<Mesa> GetAll()
        {
            return BaseRepository.GetAll<Mesa>();
        }

        public static List<Mesa> Find(string condicao)
        {
            return BaseRepository.Find<Mesa>(condicao);
        }

        public static List<Mesa> Find(Dictionary<string, object> filtros)
        {
            return BaseRepository.Find<Mesa>(filtros);
        }

        public static bool Save(Mesa entity)
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
            var result = BaseRepository.Delete<Mesa>(id);
            AcoplamentoRN.Executar(entity, "Excluir", "Depois");

            return result;
        }
    }
}



