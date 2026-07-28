using JT_Aplicacao.Classes;
using JT_Aplicacao.Eventos;
using JT_Aplicacao;
using JT_Classes.Acessos;
using JT_Classes.Restaurante;

namespace JT_Food.Shared.Services
{
    public static class ItemCompraService
    {
        public static MItemCompra? GetById(int id)
        {
            return BaseRepository.GetById<MItemCompra>(id);
        }

        public static List<MItemCompra> GetAll()
        {
            return BaseRepository.GetAll<MItemCompra>();
        }

        public static List<MItemCompra> Find(string condicao)
        {
            return BaseRepository.Find<MItemCompra>(condicao);
        }

        public static List<MItemCompra> Find(Dictionary<string, object> filtros)
        {
            return BaseRepository.Find<MItemCompra>(filtros);
        }

        public static bool Save(MItemCompra entity)
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
            var result = BaseRepository.Delete<MItemCompra>(id);
            AcoplamentoRN.Executar(entity, "Excluir", "Depois");

            return result;
        }
    }
}


