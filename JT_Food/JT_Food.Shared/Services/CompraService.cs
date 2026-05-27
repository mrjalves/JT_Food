using JT_Aplicacao;
using JT_Classes.Acessos;
using JT_Food.Shared.Models;

namespace JT_Food.Shared.Services
{
    public static class CompraService
    {
        public static MCompra? GetById(int id)
        {
            return BaseRepository.GetById<MCompra>(id);
        }

        public static List<MCompra> GetAll()
        {
            return BaseRepository.GetAll<MCompra>();
        }

        public static List<MCompra> Find(string condicao)
        {
            return BaseRepository.Find<MCompra>(condicao);
        }

        public static List<MCompra> Find(Dictionary<string, object> filtros)
        {
            return BaseRepository.Find<MCompra>(filtros);
        }

        public static bool Save(MCompra entity)
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
            var result = BaseRepository.Delete<MCompra>(id);
            AcoplamentoRN.Executar(entity, "Excluir", "Depois");

            return result;
        }
    }
}
