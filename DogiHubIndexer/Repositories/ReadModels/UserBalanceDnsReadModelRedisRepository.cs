using DogiHubIndexer.Configuration;
using DogiHubIndexer.Entities.RawData;
using DogiHubIndexer.Entities.ReadModels;
using DogiHubIndexer.Helpers;
using DogiHubIndexer.Providers.Interfaces;
using DogiHubIndexer.Repositories.ReadModels.Interfaces;
using StackExchange.Redis;

namespace DogiHubIndexer.Repositories.ReadModels
{
    public class UserBalanceDnsReadModelRedisRepository : IUserBalanceDnsReadModelRepository
    {
        private readonly IRedisClient _redisClient;

        public UserBalanceDnsReadModelRedisRepository(IRedisClient redisClient)
        {
            _redisClient = redisClient;
        }

        public async Task AddAsync(UserBalanceDnsReadModel userBalanceReadModel)
        {
            var json = JsonHelper.Serialize(userBalanceReadModel);
            string userBalanceKey = RedisKeys.GetUserBalanceKey(InscriptionTypeEnum.Dns, userBalanceReadModel.Address);
            await _redisClient.HashSetAsync(userBalanceKey, new HashEntry[] { new HashEntry(userBalanceReadModel.Name, json) });
        }

        public async Task DeleteAsync(string name, string address)
        {
            string userBalanceKey = RedisKeys.GetUserBalanceKey(InscriptionTypeEnum.Dns, address);
            await _redisClient.HashDeleteAsync(userBalanceKey, name);
        }

        public async Task<UserBalanceDnsReadModel?> GetAsync(string name, string address)
        {
            string userBalanceKey = RedisKeys.GetUserBalanceKey(InscriptionTypeEnum.Dns, address);
            var json = await _redisClient.HashGetAsync(userBalanceKey, name);
            return UserBalanceDnsReadModel.Build(json, name, address);
        }

        public async Task UpdateAsync(UserBalanceDnsReadModel userBalanceReadModel)
        {
            var json = JsonHelper.Serialize(userBalanceReadModel);
            string userBalanceKey = RedisKeys.GetUserBalanceKey(InscriptionTypeEnum.Dns, userBalanceReadModel.Address);
            await _redisClient.HashSetAsync(userBalanceKey, new HashEntry[] { new HashEntry(userBalanceReadModel.Name, json) });
        }
    }
}
