using DogiHubIndexer.Configuration;
using DogiHubIndexer.Entities.RawData;
using DogiHubIndexer.Helpers;
using DogiHubIndexer.Providers.Interfaces;
using DogiHubIndexer.Repositories.RawData.Interfaces;

namespace DogiHubIndexer.Repositories.RawData
{
    public class InscriptionRawDataRepository : IInscriptionRawDataRepository
    {
        private readonly IRedisClient _redisClient;

        public InscriptionRawDataRepository(IRedisClient redisClient)
        {
            _redisClient = redisClient;
        }

        public async Task AddAsync(InscriptionRawData rawData)
        {
            var serialized = JsonHelper.Serialize(rawData);
            string inscriptionKey = RedisKeys.GetInscriptionKey(rawData.Id.ToString());
            await _redisClient.StringSetAsync(inscriptionKey, serialized);
        }

        public async Task UpdateAsync(InscriptionRawData rawData)
        {
            var serialized = JsonHelper.Serialize(rawData);
            string inscriptionKey = RedisKeys.GetInscriptionKey(rawData.Id.ToString());
            await _redisClient.StringSetAsync(inscriptionKey, serialized);
        }

        public async Task<InscriptionRawData?> GetAsync(string inscriptionId)
        {
            string inscriptionKey = RedisKeys.GetInscriptionKey(inscriptionId);
            var json = await _redisClient.StringGetAsync(inscriptionKey);
            return InscriptionRawData.Build(json, inscriptionId);
        }
    }
}
