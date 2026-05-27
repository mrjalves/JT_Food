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
                BaseRepository.Add(config);
            }
            return config;
        }

        public static bool Save(MConfiguracao entity)
        {
            if (entity.Id == 0)
            {
                var newId = BaseRepository.Add(entity);
                entity.Id = newId;
                return newId > 0;
            }
            return BaseRepository.Update(entity);
        }
    }
}
