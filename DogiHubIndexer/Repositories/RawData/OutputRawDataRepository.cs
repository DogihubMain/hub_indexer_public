using DogiHubIndexer.Configuration;
using DogiHubIndexer.Entities.RawData;
using DogiHubIndexer.Helpers;
using DogiHubIndexer.Providers.Interfaces;
using DogiHubIndexer.Repositories.RawData.Interfaces;
using NBitcoin;

namespace DogiHubIndexer.Repositories.RawData
{
    public class OutputRawDataRepository : IOutputRawDataRepository
    {
        private readonly IRedisClient _redisClient;
        private readonly IInscriptionRawDataRepository _inscriptionRawDataRepository;

        public OutputRawDataRepository(IRedisClient redisClient, IInscriptionRawDataRepository inscriptionRawDataRepository)
        {
            _redisClient = redisClient;
            _inscriptionRawDataRepository = inscriptionRawDataRepository;
        }

        public async Task AddAsync(string transactionHash, uint index, OutputRawData outputEntity)
        {
            string outputKey = RedisKeys.GetOutputKey(transactionHash, index);
            var serialized = JsonHelper.Serialize(outputEntity);
            await _redisClient.StringSetAsync(outputKey, serialized);
        }

        public async Task DeleteAsync(uint256 transactionHash, uint index)
        {
            string outputKey = RedisKeys.GetOutputKey(transactionHash.ToString(), index);
            await _redisClient.KeyDeleteAsync(outputKey);
        }

        public async Task<OutputRawData?> GetAsync(uint256 transactionHash, uint index)
        {
            string outputKey = RedisKeys.GetOutputKey(transactionHash.ToString(), index);
            var serializedEntity = await _redisClient.StringGetAsync(outputKey);
            if (string.IsNullOrWhiteSpace(serializedEntity))
            {
                return null;
            }
            return JsonHelper.Deserialize<OutputRawData>(serializedEntity!);
        }
    }
}
