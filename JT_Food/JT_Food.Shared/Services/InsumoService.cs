using JT_Aplicacao;
using JT_Classes.Acessos;
using JT_Food.Shared.Models;

namespace JT_Food.Shared.Services
{
    public static class InsumoService
    {
        public static MInsumo? GetById(int id)
        {
            return BaseRepository.GetById<MInsumo>(id);
        }

        public static List<MInsumo> GetAll()
        {
            return BaseRepository.GetAll<MInsumo>();
        }

        public static List<MInsumo> Find(string condicao)
        {
            return BaseRepository.Find<MInsumo>(condicao);
        }

        public static bool Save(MInsumo entity)
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
            var result = BaseRepository.Delete<MInsumo>(id);
            AcoplamentoRN.Executar(entity, "Excluir", "Depois");

            return result;
        }
    }
}
