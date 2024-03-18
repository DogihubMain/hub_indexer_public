
using DogiHubIndexer.Factories;
using DogiHubIndexer.Providers.Interfaces;
using DogiHubIndexer.Services.Interfaces;
using NBitcoin;
using NBitcoin.RPC;
using Serilog;

namespace DogiHubIndexer.Services
{
    public class DoginalsIndexerService : IDoginalsIndexerService
    {
        private readonly IRedisClient _redisClient;
        private readonly IBlockService _blockService;
        private readonly IInscriptionTransferService _inscriptionTransferService;
        private readonly ILogger _logger;
        private readonly Options _options;

        public DoginalsIndexerService(
            IBlockService blockService,
            IInscriptionTransferService inscriptionTransferService,
            ILogger logger,
            IRedisClient redisClient,
            Options options)
        {
            _blockService = blockService;
            _inscriptionTransferService = inscriptionTransferService;
            _logger = logger;
            _redisClient = redisClient;
            _options = options;
        }

        public async Task RunAsync(Options options)
        {
            try
            {
                RPCClientPoolFactory rpcClientPoolFactory = GetRpcClientPoolFactory(options);

                if (options.Mode == OptionsModeEnum.Startup)
                {
                    await RunStartupModeAsync(options, rpcClientPoolFactory);
                }
                else if (options.Mode == OptionsModeEnum.Daemon)
                {
                    await RunDaemonModeAsync(options, rpcClientPoolFactory);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "unhandled exception");
                throw;
            }
        }

        public async Task RunStartupModeAsync(Options options, RPCClientPoolFactory rpcClientPoolFactory)
        {
            ulong? lastSyncBlockNumber = await _blockService.GetLastInscriptionTransfersBlockSync();

            var firstInscriptionBlockNumber = ulong.Parse(options.FirstInscriptionBlockHeight);

            var startBlockNumber = lastSyncBlockNumber.HasValue ? lastSyncBlockNumber.Value + 1 : firstInscriptionBlockNumber;
            var endBlockNumber = await GetLastBlockchainBlockNumber(rpcClientPoolFactory);
            if (ulong.TryParse(options.LastStartupBlockHeight, out ulong lastStartupBlockHeight))
            {
                endBlockNumber = lastStartupBlockHeight;
            }

            await HandleBlocks(
                options,
                rpcClientPoolFactory,
                startBlockNumber,
                endBlockNumber,
                usePending: false,
                automaticDumpStep: _options.StartupAutomaticDumpStep
            );

            await _redisClient.RunDumpAsync(endBlockNumber);
        }

        public async Task RunDaemonModeAsync(Options options, RPCClientPoolFactory rpcClientPoolFactory)
        {
            int blocksSinceLastDump = 0;
            uint256? lastKnownBlockHash = null;

            //After that we have to iterate through all new block one by one
            //For each block, extract insriptions from transactions & update read models
            while (true)
            {
                //Get last block in the blockchain
                var lastBlockchainBlockNumber = await GetLastBlockchainBlockNumber(rpcClientPoolFactory);
                var lastSyncBlockNumber = await _blockService.GetLastInscriptionTransfersBlockSync();

                if (lastSyncBlockNumber == null)
                {
                    lastSyncBlockNumber = ulong.Parse(options.FirstInscriptionBlockHeight);
                }

                try
                {
                    //we stay by default 12 blocks late behind blockchain
                    if (lastBlockchainBlockNumber - lastSyncBlockNumber.Value > (ulong)options.NumberOfBlockBehindBlockchain.GetValueOrDefault(0))
                    {
                        var currentBlockNumber = lastSyncBlockNumber.Value + 1;

                        var currentBlock = await _blockService.GetBlockAsync(rpcClientPoolFactory, currentBlockNumber);
                        if (lastKnownBlockHash != null && currentBlock.Header.HashPrevBlock != lastKnownBlockHash)
                        {
                            await HandleReorgAsync(rpcClientPoolFactory, lastKnownBlockHash);
                            blocksSinceLastDump = 0;
                            lastKnownBlockHash = null;
                            continue;
                        }

                        _logger.Information("Parsing block {blockNumber}", currentBlockNumber);
                        await HandleBlock(rpcClientPoolFactory, currentBlockNumber, usePending: true);

                        blocksSinceLastDump++;
                        lastKnownBlockHash = currentBlock.GetHash();

                        if (blocksSinceLastDump >= 50)
                        {
                            await _redisClient.RunDumpAsync(currentBlockNumber);
                            blocksSinceLastDump = 0;
                        }
                    }
                }
                catch (Exception)
                {
                    throw;
                }
                finally
                {
                    // if we are too close from last blockchain height wait 10 seconds
                    if (lastBlockchainBlockNumber - lastSyncBlockNumber.Value <= (ulong)options.NumberOfBlockBehindBlockchain.GetValueOrDefault(0))
                    {
                        Thread.Sleep(10000);
                    }
                }
            }
        }

        private async Task<ulong?> HandleReorgAsync(
            RPCClientPoolFactory rpcClientPoolFactory,
            uint256 lastKnownBlockHash)
        {
            var lastKnowBlockHeight = await _blockService.GetBlockHeightAsync(rpcClientPoolFactory, lastKnownBlockHash);
            var restoredBlockHeight = await _redisClient.RunRestoreAsync(lastKnowBlockHeight);
            return restoredBlockHeight;
        }

        private async Task HandleBlocks(
            Options options,
            RPCClientPoolFactory rpcClientPoolFactory,
            ulong startBlockNumber,
            ulong endBlockNumber,
            bool usePending,
            int? automaticDumpStep)
        {
            int blocksSinceLastDump = 0;

            var totalBlockToParse = endBlockNumber - startBlockNumber;
            var percentageIndex = 0;
            for (var i = startBlockNumber; i <= endBlockNumber; i++)
            {
                var percentage = Math.Round((decimal)percentageIndex / totalBlockToParse * 100, 2);
                _logger.Information("Parsing block {blockNumber} - {percentage}%", i, percentage);
                await HandleBlock(rpcClientPoolFactory, i, usePending);
                percentageIndex++;
                blocksSinceLastDump++;

                if (automaticDumpStep.HasValue && blocksSinceLastDump >= automaticDumpStep.Value)
                {
                    await _redisClient.RunDumpAsync(i);
                    blocksSinceLastDump = 0;
                }
            }
        }

        private async Task HandleBlock(RPCClientPoolFactory rpcClientPoolFactory, ulong i, bool usePending)
        {
            await GetAndParseBlockToFindAndSaveInscriptions(i, rpcClientPoolFactory, usePending);
            await _inscriptionTransferService.CalculateAndUpdateReadModelsAsync(i, usePending);
        }

        private async Task GetAndParseBlockToFindAndSaveInscriptions(
            ulong blockNumber,
            RPCClientPoolFactory rpcClientPoolFactory,
            bool usePending)
        {
            var block = await _blockService.GetBlockAsync(rpcClientPoolFactory, blockNumber);
            await _blockService.ParseBlockAsync(rpcClientPoolFactory, blockNumber, block, usePending);
        }

        private RPCClientPoolFactory GetRpcClientPoolFactory(Options options)
        {
            var authenticationString =
                !string.IsNullOrWhiteSpace(options.RpcUsername) || !string.IsNullOrWhiteSpace(options.RpcPassword)
                ? $"{options.RpcUsername}:{options.RpcPassword}"
                : string.Empty;

            var rpcClientPoolFactory = new RPCClientPoolFactory(authenticationString, options.RpcUrl, int.Parse(options.RpcPoolSize));
            return rpcClientPoolFactory;
        }

        private async Task<ulong> GetLastBlockchainBlockNumber(RPCClientPoolFactory rpcClientPoolFactory)
        {
            var rpcClient = await rpcClientPoolFactory.GetClientAsync();
            var blockNumber = await GetLastBlockchainBlockNumber(rpcClient);
            rpcClientPoolFactory.ReturnClient(rpcClient);
            return blockNumber;
        }

        private async Task<ulong> GetLastBlockchainBlockNumber(RPCClient rpcClient)
        {
            var chainInfo = await rpcClient.GetBlockchainInfoAsync();
            return chainInfo.Blocks;
        }
    }
}
