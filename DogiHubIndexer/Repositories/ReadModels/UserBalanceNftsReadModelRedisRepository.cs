using DogiHubIndexer.Configuration;
using DogiHubIndexer.Entities.RawData;
using DogiHubIndexer.Entities.ReadModels;
using DogiHubIndexer.Helpers;
using DogiHubIndexer.Providers.Interfaces;
using DogiHubIndexer.Repositories.ReadModels.Interfaces;
using StackExchange.Redis;

namespace DogiHubIndexer.Repositories.ReadModels
{
    public class UserBalanceNftsReadModelRedisRepository : IUserBalanceNftsReadModelRepository
    {
        private readonly IRedisClient _redisClient;

        public UserBalanceNftsReadModelRedisRepository(IRedisClient redisClient)
        {
            _redisClient = redisClient;
        }

        public async Task AddAsync(UserBalanceNftReadModel userBalanceNftReadModel)
        {
            var json = JsonHelper.Serialize(userBalanceNftReadModel);
            string userBalanceNftsKey = RedisKeys.GetUserBalanceKey(InscriptionTypeEnum.Nft, userBalanceNftReadModel.Address);
            await _redisClient.HashSetAsync(userBalanceNftsKey, new HashEntry[] { new HashEntry(userBalanceNftReadModel.InscriptionId, json) });
        }

        public async Task DeleteAsync(string inscriptionId, string address)
        {
            string userBalanceNftsKey = RedisKeys.GetUserBalanceKey(InscriptionTypeEnum.Nft, address);
            await _redisClient.HashDeleteAsync(userBalanceNftsKey, inscriptionId);
        }

        public async Task<UserBalanceNftReadModel?> GetAsync(string inscriptionId, string address)
        {
            string userBalanceNftsKey = RedisKeys.GetUserBalanceKey(InscriptionTypeEnum.Nft, address);
            var json = await _redisClient.HashGetAsync(userBalanceNftsKey, inscriptionId);
            return UserBalanceNftReadModel.Build(json, inscriptionId, address);
        }

        public async Task UpdateAsync(UserBalanceNftReadModel userBalanceReadModel)
        {
            var json = JsonHelper.Serialize(userBalanceReadModel);
            string userBalanceNftsKey = RedisKeys.GetUserBalanceKey(InscriptionTypeEnum.Nft, userBalanceReadModel.Address);
            await _redisClient.HashSetAsync(userBalanceNftsKey, new HashEntry[] { new HashEntry(userBalanceReadModel.InscriptionId, json) });
        }
    }
}
