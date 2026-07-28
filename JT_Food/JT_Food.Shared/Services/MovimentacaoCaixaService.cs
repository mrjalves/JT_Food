using JT_Aplicacao.Classes;
using JT_Aplicacao.Eventos;
using JT_Aplicacao;
using JT_Classes.Acessos;
using JT_Classes.Restaurante;

namespace JT_Food.Shared.Services
{
    public static class MovimentacaoCaixaService
    {
        public static MMovimentacaoCaixa? GetById(int id)
        {
            return BaseRepository.GetById<MMovimentacaoCaixa>(id);
        }

        public static List<MMovimentacaoCaixa> GetAll()
        {
            return BaseRepository.GetAll<MMovimentacaoCaixa>();
        }

        public static List<MMovimentacaoCaixa> Find(string condicao)
        {
            return BaseRepository.Find<MMovimentacaoCaixa>(condicao);
        }

        public static List<MMovimentacaoCaixa> Find(Dictionary<string, object> filtros)
        {
            return BaseRepository.Find<MMovimentacaoCaixa>(filtros);
        }

        public static bool Save(MMovimentacaoCaixa entity)
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
            var result = BaseRepository.Delete<MMovimentacaoCaixa>(id);
            AcoplamentoRN.Executar(entity, "Excluir", "Depois");

            return result;
        }
    }
}


