using JT_Aplicacao;
using JT_Classes.Acessos;
using JT_Food.Shared.Models;

namespace JT_Food.Shared.Services
{
    public static class UsuarioService
    {
        public static MUsuario? GetById(int id)
        {
            return BaseRepository.GetById<MUsuario>(id);
        }

        public static List<MUsuario> GetAll()
        {
            return BaseRepository.GetAll<MUsuario>();
        }

        public static List<MUsuario> Find(string condicao)
        {
            return BaseRepository.Find<MUsuario>(condicao);
        }

        public static bool Save(MUsuario entity)
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
            var result = BaseRepository.Delete<MUsuario>(id);
            AcoplamentoRN.Executar(entity, "Excluir", "Depois");

            return result;
        }
    }
}
