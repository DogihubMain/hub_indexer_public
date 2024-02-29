using DogiHubIndexer.Configuration;
using DogiHubIndexer.Entities.RawData;
using DogiHubIndexer.Entities.ReadModels;
using DogiHubIndexer.Helpers;
using DogiHubIndexer.Providers.Interfaces;
using DogiHubIndexer.Repositories.ReadModels.Interfaces;
using StackExchange.Redis;

namespace DogiHubIndexer.Repositories.ReadModels
{
    public class UserBalanceDogemapsReadModelRedisRepository : IUserBalanceDogemapsReadModelRepository
    {
        private readonly IRedisClient _redisClient;

        public UserBalanceDogemapsReadModelRedisRepository(IRedisClient redisClient)
        {
            _redisClient = redisClient;
        }

        public async Task AddAsync(UserBalanceDogemapReadModel userBalanceDogemapReadModel)
        {
            var json = JsonHelper.Serialize(userBalanceDogemapReadModel);
            string userBalanceDogemapsKey = RedisKeys.GetUserBalanceKey(InscriptionTypeEnum.Dogemap, userBalanceDogemapReadModel.Address);
            await _redisClient.HashSetAsync(userBalanceDogemapsKey, new HashEntry[] { new HashEntry(userBalanceDogemapReadModel.Number.ToString(), json) });

        }

        public async Task DeleteAsync(int number, string address)
        {
            string userBalanceDogemapsKey = RedisKeys.GetUserBalanceKey(InscriptionTypeEnum.Dogemap, address);
            await _redisClient.HashDeleteAsync(userBalanceDogemapsKey, number.ToString());
        }

        public async Task<UserBalanceDogemapReadModel?> GetAsync(int number, string address)
        {
            string userBalanceDogemapsKey = RedisKeys.GetUserBalanceKey(InscriptionTypeEnum.Dogemap, address);
            var json = await _redisClient.HashGetAsync(userBalanceDogemapsKey, number.ToString());
            return UserBalanceDogemapReadModel.Build(json, number, address);
        }

        public async Task UpdateAsync(UserBalanceDogemapReadModel userBalanceReadModel)
        {
            var json = JsonHelper.Serialize(userBalanceReadModel);
            string userBalanceDogemapsKey = RedisKeys.GetUserBalanceKey(InscriptionTypeEnum.Dogemap, userBalanceReadModel.Address);
            await _redisClient.HashSetAsync(userBalanceDogemapsKey, new HashEntry[] { new HashEntry(userBalanceReadModel.Number.ToString(), json) });
        }
    }
}
