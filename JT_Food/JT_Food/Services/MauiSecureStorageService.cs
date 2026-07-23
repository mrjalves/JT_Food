using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using JT_Classes.Acessos;

namespace JT_Food.Services
{
    public class MauiSecureStorageService : ISecureStorageService
    {
        public async Task SetTokenAsync(string key, string value)
        {
            await SecureStorage.Default.SetAsync(key, value);
        }

        public async Task<string?> GetTokenAsync(string key)
        {
            return await SecureStorage.Default.GetAsync(key);
        }

        public void RemoveToken(string key)
        {
            SecureStorage.Default.Remove(key);
        }
    }
}
