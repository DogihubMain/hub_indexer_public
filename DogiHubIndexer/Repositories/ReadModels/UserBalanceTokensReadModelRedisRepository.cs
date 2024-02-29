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
            var json = JsonHelper.Serialize(userBalanceReadModel);

            string balanceDetailKey = RedisKeys.GetBalanceDetailKey(userBalanceReadModel.Address, userBalanceReadModel.TokenTick);
            string userBalanceKey = RedisKeys.GetUserBalanceKey(InscriptionTypeEnum.Token, userBalanceReadModel.Address);
            string tokenBalanceKey = RedisKeys.GetTokenBalanceKey(userBalanceReadModel.TokenTick);

            var batch = _redisClient.CreateBatch();
            var balanceDetailTask = batch.StringSetAsync(balanceDetailKey, json);
            var userTask = batch.SortedSetAddAsync(userBalanceKey, balanceDetailKey, DateTimeOffset.Now.ToUnixTimeSeconds());
            var tokenTask = batch.SortedSetAddAsync(tokenBalanceKey, balanceDetailKey, (double)userBalanceReadModel.BalanceSum);
            batch.Execute();

            await Task.WhenAll(balanceDetailTask, userTask, tokenTask);
        }

        public async Task<UserBalanceTokenReadModel?> GetAsync(string tick, string address)
        {
            string balanceDetailKey = RedisKeys.GetBalanceDetailKey(address, tick);
            var json = await _redisClient.StringGetAsync(balanceDetailKey);
            return UserBalanceTokenReadModel.Build(json, tick, address);
        }

        public async Task UpdateAsync(UserBalanceTokenReadModel userBalanceReadModel)
        {
            var json = JsonHelper.Serialize(userBalanceReadModel);

            string balanceDetailKey = RedisKeys.GetBalanceDetailKey(userBalanceReadModel.Address, userBalanceReadModel.TokenTick);
            string userBalanceKey = RedisKeys.GetUserBalanceKey(InscriptionTypeEnum.Token, userBalanceReadModel.Address);
            string tokenBalanceKey = RedisKeys.GetTokenBalanceKey(userBalanceReadModel.TokenTick);

            var batch = _redisClient.CreateBatch();
            var balanceDetailTask = batch.StringSetAsync(balanceDetailKey, json);
            var userTask = batch.SortedSetAddAsync(userBalanceKey, balanceDetailKey, DateTimeOffset.Now.ToUnixTimeSeconds());
            var tokenTask = batch.SortedSetAddAsync(tokenBalanceKey, balanceDetailKey, (double)userBalanceReadModel.BalanceSum);
            batch.Execute();

            await Task.WhenAll(balanceDetailTask, userTask, tokenTask);
        }
    }
}
