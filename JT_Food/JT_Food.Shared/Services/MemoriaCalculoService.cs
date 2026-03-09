using JT_Aplicacao;
using JT_Classes.Acessos;
using JT_Food.Shared.Models;

namespace JT_Food.Shared.Services
{
    public static class MemoriaCalculoService
    {
        public static MMemoriaCalculo? GetById(int id)
        {
            return BaseRepository.GetById<MMemoriaCalculo>(id);
        }

        public static List<MMemoriaCalculo> GetAll()
        {
            return BaseRepository.GetAll<MMemoriaCalculo>();
        }

        public static List<MMemoriaCalculo> Find(string condicao)
        {
            return BaseRepository.Find<MMemoriaCalculo>(condicao);
        }

        public static bool Save(MMemoriaCalculo entity)
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
            var result = BaseRepository.Delete<MMemoriaCalculo>(id);
            AcoplamentoRN.Executar(entity, "Excluir", "Depois");

            return result;
        }
    }
}
