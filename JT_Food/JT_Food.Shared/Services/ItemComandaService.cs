using JT_Aplicacao;
using JT_Classes.Acessos;
using JT_Food.Shared.Models;

namespace JT_Food.Shared.Services
{
    public static class ItemComandaService
    {
        public static MItemComanda? GetById(int id)
        {
            return BaseRepository.GetById<MItemComanda>(id);
        }

        public static List<MItemComanda> GetAll()
        {
            return BaseRepository.GetAll<MItemComanda>();
        }

        public static List<MItemComanda> Find(string condicao)
        {
            return BaseRepository.Find<MItemComanda>(condicao);
        }

        public static bool Save(MItemComanda entity)
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
            var result = BaseRepository.Delete<MItemComanda>(id);
            AcoplamentoRN.Executar(entity, "Excluir", "Depois");

            return result;
        }
    }
}
