using CommandLine;

namespace DogiHubIndexer
{
    public class Options
    {
        [Option('f', "firstInscriptionBlockHeight", Default = "4609723", Required = true, HelpText = "First block containing an inscription")]
        public required string FirstInscriptionBlockHeight { get; set; }

        [Option('l', "lastStartupBlockHeight",  Required = true, HelpText = "Last block to pars during startup mode (higher block height is taken from blockchain if null)")]
        public  string? LastStartupBlockHeight { get; set; }

        [Option("rpcUrl", Required = true, HelpText = "RPC Url")]
        public required string RpcUrl { get; set; }

        [Option("rpcUsername", Required = false, HelpText = "RPC Username")]
        public string? RpcUsername { get; set; }

        [Option("rpcPassword", Required = false, HelpText = "RPC Password")]
        public string? RpcPassword { get; set; }

        [Option("rpcPoolSize", Default = "100", Required = true, HelpText = "Number of RPC Client in the pool")]
        public required string RpcPoolSize { get; set; }

        [Option("redisConnectionString", Required = true, HelpText = "Redis connection string")]
        public required string RedisConnectionString { get; set; }
        //[Option("postgresConnectionString", Required = true, HelpText = "Postgres connection string")]
        //public required string PostgresConnectionString { get; set; }

        [Option("flushRedis", HelpText = "For testing purpose, flush all redis databases (must be admin)")]
        public bool? FlushRedis { get; set; }

        [Option("pendingConfirmationNumber", Default = 12, HelpText = "Number of pending blocks to confirm a transfer in a balance")]
        public required int PendingConfirmationNumber { get; set; }

        [Option("cpuNumber", Default = 8, HelpText = "Host CPU number for parallelism")]
        public required int CpuNumber { get; set; }

        [Option("redisDataFolder", Default = 8, HelpText = "Redis data folder (used for backup system)")]
        public required string RedisDataFolder { get; set; }

        [Option("logFilePath", HelpText = "File path of logs")]
        public required string LogFilePath { get; set; }

        [Option("mode", Required = true, Default = nameof(OptionsModeEnum.Startup), HelpText = "Startup or Daemon mode are available")]
        public OptionsModeEnum? Mode { get; set; }
    }

    public enum OptionsModeEnum
    {
        Startup,
        Daemon
    }
}
