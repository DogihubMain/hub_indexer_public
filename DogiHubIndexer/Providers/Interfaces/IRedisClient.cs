using StackExchange.Redis;
using System.Net;

namespace DogiHubIndexer.Providers.Interfaces
{
    public interface IRedisClient
    {
        void FlushAllDatabases();
        Task RunDumpAsync(ulong blockNumber);
        Task<ulong?> RunRestoreAsync(ulong blockNumber);
        IBatch CreateBatch();
        Task<bool> StringSetAsync(string key, RedisValue value);
        Task<RedisValue?> StringGetAsync(string key);
        Task<bool> SortedSetAddAsync(string key, RedisValue member, double score);
        Task<RedisValue[]> SortedSetRangeByScoreAsync(string key);
        Task KeyDeleteAsync(string outputKey);
        Task SortedSetRemoveAsync(string key, RedisValue member);
        IEnumerable<EndPoint> GetEndPoints();
        IServer GetServer(EndPoint endpoint);
        Task HashSetAsync(string key, HashEntry[] hashFields);
        Task<RedisValue> HashGetAsync(string key, RedisValue hashField);
        Task<RedisValue[]> SetMembersAsync(string key);
        Task<bool> SetAddAsync(string key, RedisValue value);
        Task<bool> SetRemoveAsync(string key, RedisValue value);
        Task HashDeleteAsync(string key, RedisValue value);
    }
}
