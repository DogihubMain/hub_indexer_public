using DogiHubIndexer.Providers.Interfaces;
using StackExchange.Redis;
using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;

namespace DogiHubIndexer.Providers
{
    public class RedisClient : IRedisClient
    {
        private IDatabase _database;
        private ConnectionMultiplexer _connectionMultiplexer;
        private readonly Options _options;

        public RedisClient(string connectionString, Options options, bool flushDatabase = false)
        {
            _connectionMultiplexer = ConnectionMultiplexer.Connect(connectionString);
            _options = options;
            _database = _connectionMultiplexer.GetDatabase();

            if (flushDatabase)
            {
                FlushAllDatabases();
            }
        }

        public void FlushAllDatabases()
        {
            foreach (var endPoint in _connectionMultiplexer!.GetEndPoints())
            {
                var server = _connectionMultiplexer.GetServer(endPoint);
                if (!server.IsReplica)
                {
                    server.FlushAllDatabases();
                }
            }
        }

        private void EnsureDatabaseConnection()
        {
            if (_database is null)
                throw new InvalidOperationException("RedisClient is not configured with a connection string.");
        }

        public async Task RunDumpAsync(ulong blockNumber)
        {
            var lastSave = await GetLastDumpDateAsync();
            await _database.ExecuteAsync("BGSAVE");

            while (true)
            {
                await Task.Delay(1000);
                var currentSave = await GetLastDumpDateAsync();
                if (currentSave > lastSave)
                {
                    //dump finished
                    var dumpFilePathName = Path.Combine(_options.RedisDataFolder, "dump.rdb");
                    var newDumpFilePathName = Path.Combine(_options.RedisDataFolder, $"dump_{blockNumber}.rdb");

                    File.Copy(dumpFilePathName, newDumpFilePathName, overwrite: true);
                    break;
                }
            }
        }

        public async Task<ulong?> RunRestoreAsync(ulong blockNumber)
        {
            EnsureDatabaseConnection();

            ulong? restoredBlockHeight = null;

            // Attempt to find the exact match first
            var dumpFilePathName = Path.Combine(_options.RedisDataFolder, $"dump_{blockNumber}.rdb");

            if (!File.Exists(dumpFilePathName))
            {
                // If not found, try to find the closest lower block number
                var directoryInfo = new DirectoryInfo(_options.RedisDataFolder);
                var files = directoryInfo.GetFiles("dump_*.rdb")
                    .Select(f => (FileName: f.Name, BlockNumber: ulong.Parse(Regex.Match(f.Name, @"dump_(\d+).rdb").Groups[1].Value)))
                    .Where(f => f.BlockNumber <= blockNumber)
                    .OrderByDescending(f => f.BlockNumber)
                    .ToList();

                if (files.Count() == 0)
                {
                    throw new FileNotFoundException("No dump files available to restore from.");
                }

                // Use the closest block number file
                dumpFilePathName = Path.Combine(_options.RedisDataFolder, files.First().FileName);
                restoredBlockHeight = files.First().BlockNumber;
            }

            await StopRedisServerAsync();

            var targetDumpFilePathName = Path.Combine(_options.RedisDataFolder, "dump.rdb");
            File.Copy(dumpFilePathName, targetDumpFilePathName, overwrite: true);

            await StartRedisServerAsync();

            return restoredBlockHeight;
        }

        private Task StopRedisServerAsync()
        {
            return ExecuteDockerComposeCommandAsync("down");
        }

        private Task StartRedisServerAsync()
        {
            return ExecuteDockerComposeCommandAsync("up -d");
        }

        private async Task ExecuteDockerComposeCommandAsync(string command)
        {
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "docker-compose",
                    Arguments = command,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = "/app/redis/"
                }
            };

            process.Start();
            await process.WaitForExitAsync();
        }

        public async Task<DateTime> GetLastDumpDateAsync()
        {
            foreach (var endPoint in _connectionMultiplexer!.GetEndPoints())
            {
                var server = _connectionMultiplexer.GetServer(endPoint);
                var lastSaveDateTime = await server.LastSaveAsync();
                return lastSaveDateTime;
            }
            throw new InvalidOperationException("No redis endpoint found to dump db");
        }

        public void DumpDatabase()
        {
            _database.Execute("BGSAVE");
        }

        public IBatch CreateBatch()
        {
            EnsureDatabaseConnection();
            return _database.CreateBatch();
        }

        public Task<bool> StringSetAsync(string key, RedisValue value)
        {
            EnsureDatabaseConnection();
            return _database.StringSetAsync(key, value);
        }
        public Task<bool> SortedSetAddAsync(string key, RedisValue member, double score)
        {
            EnsureDatabaseConnection();
            return _database.SortedSetAddAsync(key, member, score);
        }

        public Task<RedisValue[]> SortedSetRangeByScoreAsync(string key)
        {
            EnsureDatabaseConnection();
            return _database.SortedSetRangeByScoreAsync(key);
        }

        public async Task<RedisValue?> StringGetAsync(string key)
        {
            EnsureDatabaseConnection();
            RedisValue value = await (_database.StringGetAsync(key) ?? Task.FromResult(RedisValue.Null));

            if (value.IsNull)
            {
                return null;
            }

            return value.ToString();
        }

        public Task HashSetAsync(string key, HashEntry[] hashFields)
        {
            EnsureDatabaseConnection();
            return _database.HashSetAsync(key, hashFields);
        }

        public Task<RedisValue> HashGetAsync(string key, RedisValue hashField)
        {
            EnsureDatabaseConnection();
            return _database.HashGetAsync(key, hashField);
        }

        public Task HashDeleteAsync(string key, RedisValue hashField)
        {
            EnsureDatabaseConnection();
            return _database.HashDeleteAsync(key, hashField);
        }

        public Task<RedisValue[]> SetMembersAsync(string key)
        {
            EnsureDatabaseConnection();
            return _database.SetMembersAsync(key);
        }

        public Task<bool> SetAddAsync(string key, RedisValue value)
        {
            EnsureDatabaseConnection();
            return _database.SetAddAsync(key, value);
        }

        public Task<bool> SetRemoveAsync(string key, RedisValue value)
        {
            EnsureDatabaseConnection();
            return _database.SetRemoveAsync(key, value);
        }

        public Task KeyDeleteAsync(string key)
        {
            return _database.KeyDeleteAsync(key);
        }

        public Task SortedSetRemoveAsync(string key, RedisValue member)
        {
            return _database.SortedSetRemoveAsync(key, member);
        }

        public IEnumerable<EndPoint> GetEndPoints()
        {
            return _connectionMultiplexer.GetEndPoints();
        }

        public IServer GetServer(EndPoint endpoint)
        {
            return _connectionMultiplexer.GetServer(endpoint);
        }


    };
}
