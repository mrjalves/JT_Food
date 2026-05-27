using JT_Aplicacao;
using JT_Classes.Acessos;
using JT_Food.Shared.Models;

namespace JT_Food.Shared.Services
{
    public static class UsuarioService
    {
        public static Usuario? GetById(int id)
        {
            return BaseRepository.GetById<Usuario>(id);
        }

        public static List<Usuario> GetAll()
        {
            return BaseRepository.GetAll<Usuario>();
        }

        public static List<Usuario> Find(string condicao)
        {
            return BaseRepository.Find<Usuario>(condicao);
        }

        public static List<Usuario> Find(Dictionary<string, object> filtros)
        {
            return BaseRepository.Find<Usuario>(filtros);
        }

        public static bool Save(Usuario entity)
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
            var result = BaseRepository.Delete<Usuario>(id);
            AcoplamentoRN.Executar(entity, "Excluir", "Depois");

            return result;
        }
    }
}
