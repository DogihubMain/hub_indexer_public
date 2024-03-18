using DogiHubIndexer.Configuration;
using DogiHubIndexer.Entities.ReadModels;
using DogiHubIndexer.Helpers;
using DogiHubIndexer.Providers.Interfaces;
using DogiHubIndexer.Repositories.ReadModels.Interfaces;

namespace DogiHubIndexer.Repositories.ReadModels
{
    public class TokenInfoReadModelRedisRepository : ITokenInfoReadModelRepository
    {
        private readonly IRedisClient _redisClient;

        public TokenInfoReadModelRedisRepository(IRedisClient redisClient)
        {
            _redisClient = redisClient;
        }

        public async Task AddAsync(TokenReadModel readModel)
        {
            var serialized = JsonHelper.Serialize(readModel);
            string tokenKey = RedisKeys.GetTokenInfoKey(readModel.Tick);
            string tokenListKey = RedisKeys.GetTokenListKey();

            await _redisClient.StringSetAsync(tokenKey, serialized);
            await _redisClient.SortedSetAddAsync(tokenListKey, tokenKey, readModel.Date.ToUnixTimeSeconds());
        }

        public async Task<TokenReadModel?> GetAsync(string tick)
        {
            string tokenKey = RedisKeys.GetTokenInfoKey(tick);
            var json = await _redisClient.StringGetAsync(tokenKey);
            return TokenReadModel.Build(json, tick);
        }

        public async Task UpdateAsync(TokenReadModel readModel)
        {
            var serialized = JsonHelper.Serialize(readModel);
            string tokenKey = RedisKeys.GetTokenInfoKey(readModel.Tick);
            await _redisClient.StringSetAsync(tokenKey, serialized);
        }
    }
}
