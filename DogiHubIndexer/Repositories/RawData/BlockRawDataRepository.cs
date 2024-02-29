using DogiHubIndexer.Configuration;
using DogiHubIndexer.Providers.Interfaces;
using DogiHubIndexer.Repositories.RawData.Interfaces;

namespace DogiHubIndexer.Repositories.RawData
{
    public class BlockRawdataRepository : IBlockRawDataRepository
    {
        private readonly IRedisClient _redisClient;

        public BlockRawdataRepository(IRedisClient redisClient)
        {
            _redisClient = redisClient;
        }

        public Task<bool> SetLastInscriptionTransfersBlockSync(ulong blockNumber)
        {
            string lastInscriptionTransfersBlockSyncKey = RedisKeys.GetLastInscriptionTransfersBlockSyncKey();
            return _redisClient.StringSetAsync(lastInscriptionTransfersBlockSyncKey, blockNumber.ToString());
        }

        public Task<bool> SetLastReadModelsBlockSync(ulong blockNumber)
        {
            string lastReadModelsBlockSyncKey = RedisKeys.GetLastReadModelsBlockSyncKey();
            return _redisClient.StringSetAsync(lastReadModelsBlockSyncKey, blockNumber.ToString());
        }

        public async Task<ulong?> GetLastInscriptionTransfersBlockSync()
        {
            string lastInscriptionTransfersBlockSyncKey = RedisKeys.GetLastInscriptionTransfersBlockSyncKey();

            var stringValue = await _redisClient.StringGetAsync(lastInscriptionTransfersBlockSyncKey);

            if (!string.IsNullOrWhiteSpace(stringValue)
                && ulong.TryParse(stringValue, out ulong ulongValue))
            {
                return ulongValue;
            }
            else
            {
                return null;
            }
        }

        public async Task<ulong?> GetLastReadModelsBlockSync()
        {
            string lastReadModelsBlockSyncKey = RedisKeys.GetLastReadModelsBlockSyncKey();

            var stringValue = await _redisClient.StringGetAsync(lastReadModelsBlockSyncKey);

            if (!string.IsNullOrWhiteSpace(stringValue)
                && ulong.TryParse(stringValue, out ulong ulongValue))
            {
                return ulongValue;
            }
            else
            {
                return null;
            }
        }

    }
}
