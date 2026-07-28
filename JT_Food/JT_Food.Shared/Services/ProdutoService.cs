using JT_Aplicacao.Classes;
using JT_Aplicacao.Eventos;
using JT_Aplicacao;
using JT_Classes.Acessos;
using JT_Classes.Restaurante;

namespace JT_Food.Shared.Services
{
    public static class ProdutoService
    {
        public static MProduto? GetById(int id)
        {
            return BaseRepository.GetById<MProduto>(id);
        }

        public static List<MProduto> GetAll()
        {
            return BaseRepository.GetAll<MProduto>();
        }

        public static List<MProduto> Find(string condicao)
        {
            return BaseRepository.Find<MProduto>(condicao);
        }

        public static List<MProduto> Find(Dictionary<string, object> filtros)
        {
            return BaseRepository.Find<MProduto>(filtros);
        }

        public static bool Save(MProduto entity)
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
            var result = BaseRepository.Delete<MProduto>(id);
            AcoplamentoRN.Executar(entity, "Excluir", "Depois");

            return result;
        }
    }
}


