using JT_Aplicacao;
using JT_Food.Shared.Models;

namespace JT_Food.Shared.Services
{
    public static class ConfiguracaoService
    {
        public static MConfiguracao Get()
        {
            var config = BaseRepository.GetAll<MConfiguracao>().FirstOrDefault();
            if (config == null)
            {
                config = new MConfiguracao();
            }
            return config;
        }

        public static bool Save(MConfiguracao entity)
        {
            var configBaseService = new JT_Servicos.Comum.ConfiguracaoService();
            JT_Classes.Comum.Configuracao configBase = new JT_Classes.Comum.Configuracao
            {
                Id = entity.Id,
                IdUsuario = entity.IdUsuario,
                Tema = entity.Tema,
                ItensPorPagina = entity.ItensPorPagina,
                NotificacoesEmail = entity.NotificacoesEmail,
                Status = entity.Status,
                Ativo = entity.Ativo
            };

            if (entity.Id == 0)
            {
                // Synchronous save since configBaseService.Save in JTServiceBase doesn't block properly if used synchronously, but wait, JTServiceBase has `Save` which returns `Task<bool>`.
                // Wait, here it's static and synchronous. BaseRepository.Add is synchronous.
                var newId = JT_Aplicacao.Classes.BaseRepository.Add(configBase);
                if (newId > 0)
                {
                    entity.Id = newId;
                    JT_Aplicacao.Classes.BaseRepository.Add(entity);
                    return true;
                }
                return false;
            }
            else
            {
                JT_Aplicacao.Classes.BaseRepository.Update(configBase);
            }
            return BaseRepository.Update(entity);
        }
    }
}
