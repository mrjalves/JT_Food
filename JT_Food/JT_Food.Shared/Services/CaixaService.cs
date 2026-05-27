using JT_Aplicacao;
using JT_Classes.Acessos;
using JT_Food.Shared.Models;

namespace JT_Food.Shared.Services
{
    public static class CaixaService
    {
        public static MCaixa? GetById(int id)
        {
            return BaseRepository.GetById<MCaixa>(id);
        }

        public static List<MCaixa> GetAll()
        {
            return BaseRepository.GetAll<MCaixa>();
        }

        public static List<MCaixa> Find(string condicao)
        {
            return BaseRepository.Find<MCaixa>(condicao);
        }

        public static List<MCaixa> Find(Dictionary<string, object> filtros)
        {
            return BaseRepository.Find<MCaixa>(filtros);
        }

        public static bool Save(MCaixa entity)
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
            var result = BaseRepository.Delete<MCaixa>(id);
            AcoplamentoRN.Executar(entity, "Excluir", "Depois");

            return result;
        }
    }
}
