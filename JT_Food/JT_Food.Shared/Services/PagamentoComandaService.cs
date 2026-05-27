using JT_Aplicacao;
using JT_Classes.Acessos;
using JT_Food.Shared.Models;

namespace JT_Food.Shared.Services
{
    public static class PagamentoComandaService
    {
        public static MPagamentoComanda? GetById(int id)
        {
            return BaseRepository.GetById<MPagamentoComanda>(id);
        }

        public static List<MPagamentoComanda> GetAll()
        {
            return BaseRepository.GetAll<MPagamentoComanda>();
        }

        public static List<MPagamentoComanda> Find(string condicao)
        {
            return BaseRepository.Find<MPagamentoComanda>(condicao);
        }

        public static List<MPagamentoComanda> Find(Dictionary<string, object> filtros)
        {
            return BaseRepository.Find<MPagamentoComanda>(filtros);
        }

        public static bool Save(MPagamentoComanda entity)
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
            var result = BaseRepository.Delete<MPagamentoComanda>(id);
            AcoplamentoRN.Executar(entity, "Excluir", "Depois");

            return result;
        }
    }
}
