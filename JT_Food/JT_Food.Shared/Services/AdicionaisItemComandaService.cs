using JT_Aplicacao;
using JT_Classes.Acessos;
using JT_Food.Shared.Models;

namespace JT_Food.Shared.Services
{
    public static class AdicionaisItemComandaService
    {
        public static MAdicionaisItemComanda? GetById(int id)
        {
            return BaseRepository.GetById<MAdicionaisItemComanda>(id);
        }

        public static List<MAdicionaisItemComanda> GetAll()
        {
            return BaseRepository.GetAll<MAdicionaisItemComanda>();
        }

        public static List<MAdicionaisItemComanda> Find(string condicao)
        {
            return BaseRepository.Find<MAdicionaisItemComanda>(condicao);
        }

        public static bool Save(MAdicionaisItemComanda entity)
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
            var result = BaseRepository.Delete<MAdicionaisItemComanda>(id);
            AcoplamentoRN.Executar(entity, "Excluir", "Depois");

            return result;
        }
    }
}
