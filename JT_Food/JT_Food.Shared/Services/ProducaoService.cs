using JT_Aplicacao;
using JT_Classes.Acessos;
using JT_Food.Shared.Models;

namespace JT_Food.Shared.Services
{
    public static class ProducaoService
    {
        public static MProducao? GetById(int id)
        {
            return BaseRepository.GetById<MProducao>(id);
        }

        public static List<MProducao> GetAll()
        {
            return BaseRepository.GetAll<MProducao>();
        }

        public static List<MProducao> Find(string condicao)
        {
            return BaseRepository.Find<MProducao>(condicao);
        }

        public static List<MProducao> Find(Dictionary<string, object> filtros)
        {
            return BaseRepository.Find<MProducao>(filtros);
        }

        public static bool Save(MProducao entity)
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
            var result = BaseRepository.Delete<MProducao>(id);
            AcoplamentoRN.Executar(entity, "Excluir", "Depois");

            return result;
        }
    }
}
