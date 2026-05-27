using JT_Aplicacao;
using JT_Classes.Acessos;
using JT_Food.Shared.Models;

namespace JT_Food.Shared.Services
{
    public static class EstoqueService
    {
        public static MEstoque? GetById(int id)
        {
            return BaseRepository.GetById<MEstoque>(id);
        }

        public static List<MEstoque> GetAll()
        {
            return BaseRepository.GetAll<MEstoque>();
        }

        public static List<MEstoque> Find(string condicao)
        {
            return BaseRepository.Find<MEstoque>(condicao);
        }

        public static List<MEstoque> Find(Dictionary<string, object> filtros)
        {
            return BaseRepository.Find<MEstoque>(filtros);
        }

        public static bool Save(MEstoque entity)
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
            var result = BaseRepository.Delete<MEstoque>(id);
            AcoplamentoRN.Executar(entity, "Excluir", "Depois");

            return result;
        }
    }
}
