using JT_Aplicacao.Classes;
using JT_Aplicacao.Eventos;
using JT_Aplicacao;
using JT_Classes.Acessos;
using JT_Classes.Restaurante;

namespace JT_Food.Shared.Services
{
    public static class ReceitaService
    {
        public static MReceita? GetById(int id)
        {
            return BaseRepository.GetById<MReceita>(id);
        }

        public static List<MReceita> GetAll()
        {
            return BaseRepository.GetAll<MReceita>();
        }

        public static List<MReceita> Find(string condicao)
        {
            return BaseRepository.Find<MReceita>(condicao);
        }

        public static List<MReceita> Find(Dictionary<string, object> filtros)
        {
            return BaseRepository.Find<MReceita>(filtros);
        }

        public static bool Save(MReceita entity)
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
            var result = BaseRepository.Delete<MReceita>(id);
            AcoplamentoRN.Executar(entity, "Excluir", "Depois");

            return result;
        }
    }
}


