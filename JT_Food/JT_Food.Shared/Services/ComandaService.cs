using JT_Aplicacao;
using JT_Classes.Acessos;
using JT_Food.Shared.Models;

namespace JT_Food.Shared.Services
{
    public static class ComandaService
    {
        public static MComanda? GetById(int id)
        {
            return BaseRepository.GetById<MComanda>(id);
        }

        public static List<MComanda> GetAll()
        {
            return BaseRepository.GetAll<MComanda>();
        }

        public static List<MComanda> Find(string condicao)
        {
            return BaseRepository.Find<MComanda>(condicao);
        }

        public static List<MComanda> Find(Dictionary<string, object> filtros)
        {
            return BaseRepository.Find<MComanda>(filtros);
        }

        public static bool Save(MComanda entity)
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

                // Regra de negócio: ao abrir uma comanda vinculada a uma mesa,
                // atualizar automaticamente o status da mesa para "Ocupada".
                if (result && !string.IsNullOrEmpty(entity.MesaId))
                {
                    var mesas = MesaService.Find($"Identificacao = '{entity.MesaId}'");
                    var mesa = mesas?.FirstOrDefault();
                    if (mesa != null && mesa.Status == StatusMesa.Livre)
                    {
                        mesa.Status = StatusMesa.Ocupada;
                        MesaService.Save(mesa);
                    }
                }
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
            var result = BaseRepository.Delete<MComanda>(id);
            AcoplamentoRN.Executar(entity, "Excluir", "Depois");

            return result;
        }
    }
}
