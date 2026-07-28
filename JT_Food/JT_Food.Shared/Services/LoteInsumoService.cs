using JT_Aplicacao.Classes;
using JT_Aplicacao.Eventos;
using JT_Aplicacao;
using JT_Classes.Acessos;
using JT_Classes.Restaurante;

namespace JT_Food.Shared.Services
{
    public static class LoteInsumoService
    {
        public static MLoteInsumo? GetById(int id)
        {
            return BaseRepository.GetById<MLoteInsumo>(id);
        }

        public static List<MLoteInsumo> GetAll()
        {
            return BaseRepository.GetAll<MLoteInsumo>();
        }

        public static List<MLoteInsumo> Find(string condicao)
        {
            return BaseRepository.Find<MLoteInsumo>(condicao);
        }

        public static List<MLoteInsumo> Find(Dictionary<string, object> filtros)
        {
            return BaseRepository.Find<MLoteInsumo>(filtros);
        }

        public static bool Save(MLoteInsumo entity)
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
            var result = BaseRepository.Delete<MLoteInsumo>(id);
            AcoplamentoRN.Executar(entity, "Excluir", "Depois");

            return result;
        }
    }
}


