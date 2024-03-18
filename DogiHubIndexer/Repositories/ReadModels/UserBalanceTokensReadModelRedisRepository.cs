using DogiHubIndexer.Configuration;
using DogiHubIndexer.Entities.RawData;
using DogiHubIndexer.Entities.ReadModels;
using DogiHubIndexer.Helpers;
using DogiHubIndexer.Providers.Interfaces;
using DogiHubIndexer.Repositories.ReadModels.Interfaces;

namespace DogiHubIndexer.Repositories.ReadModels
{
    public class UserBalanceTokensReadModelRedisRepository : IUserBalanceTokensReadModelRepository
    {
        private readonly IRedisClient _redisClient;

        public UserBalanceTokensReadModelRedisRepository(IRedisClient redisClient)
        {
            _redisClient = redisClient;
        }

        public async Task AddAsync(UserBalanceTokenReadModel userBalanceReadModel)
        {
            await AddOrUpdateInternalAsync(userBalanceReadModel);
        }

        public async Task<UserBalanceTokenReadModel?> GetAsync(string tick, string address)
        {
            string balanceDetailKey = RedisKeys.GetBalanceDetailKey(address, tick);
            var json = await _redisClient.StringGetAsync(balanceDetailKey);
            return UserBalanceTokenReadModel.Build(json, tick, address);
        }

        public async Task UpdateAsync(UserBalanceTokenReadModel userBalanceReadModel)
        {
            await AddOrUpdateInternalAsync(userBalanceReadModel);
        }

        private async Task AddOrUpdateInternalAsync(UserBalanceTokenReadModel userBalanceReadModel)
        {
            var json = JsonHelper.Serialize(userBalanceReadModel);

            string balanceDetailKey = RedisKeys.GetBalanceDetailKey(userBalanceReadModel.Address, userBalanceReadModel.TokenTick);
            string userBalanceKey = RedisKeys.GetUserBalanceKey(InscriptionTypeEnum.Token, userBalanceReadModel.Address);
            string tokenBalanceKey = RedisKeys.GetTokenBalanceKey(userBalanceReadModel.TokenTick);

            var batch = _redisClient.CreateBatch();
            var tasks = new List<Task>
            {
                batch.StringSetAsync(balanceDetailKey, json)
            };

            if (userBalanceReadModel.BalanceSum == 0)
            {
                //for user balance with keep the tick even if quantity become 0 for tracibility
                tasks.Add(batch.SortedSetAddAsync(userBalanceKey, balanceDetailKey, DateTimeOffset.Now.ToUnixTimeSeconds()));
                //remove from holders list
                tasks.Add(batch.SortedSetRemoveAsync(tokenBalanceKey, balanceDetailKey));
            }
            else
            {
                tasks.Add(batch.SortedSetAddAsync(userBalanceKey, balanceDetailKey, DateTimeOffset.Now.ToUnixTimeSeconds()));
                tasks.Add(batch.SortedSetAddAsync(tokenBalanceKey, balanceDetailKey, (double)userBalanceReadModel.BalanceSum));
            }

            batch.Execute();
            await Task.WhenAll(tasks);
        }
    }
}
