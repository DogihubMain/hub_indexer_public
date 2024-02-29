using DogiHubIndexer.Factories;
using DogiHubIndexer.Services.Interfaces;
using NBitcoin;
using NBitcoin.RPC;

namespace DogiHubIndexer.Services
{
    public class BlockService : IBlockService
    {
        private readonly Repositories.RawData.Interfaces.IBlockRawDataRepository _blockRepository;

        private readonly ITransactionService _transactionService;

        public BlockService(ITransactionService transactionService, Repositories.RawData.Interfaces.IBlockRawDataRepository blockRepository)
        {
            _transactionService = transactionService;
            _blockRepository = blockRepository;
        }

        public async Task<Block> GetBlockAsync(RPCClientPoolFactory rpcClientPoolFactory, ulong blockNumber)
        {
            return await rpcClientPoolFactory.UseClientAsync(async rpcClient =>
            {
                var blockHash = await rpcClient.GetBlockHashAsync((int)blockNumber);
                var block = await rpcClient.GetBlockAsync(blockHash);
                return block;
            });
        }

        public async Task<Block> GetBlockAsync(RPCClientPoolFactory rpcClientPoolFactory, uint256 blockHash)
        {
            return await rpcClientPoolFactory.UseClientAsync(async rpcClient =>
            {
                var block = await rpcClient.GetBlockAsync(blockHash);
                return block;
            });
        }

        public async Task<ulong> GetBlockHeightAsync(RPCClientPoolFactory rpcClientPoolFactory, uint256 blockHash)
        {
            return await rpcClientPoolFactory.UseClientAsync(async rpcClient =>
            {
                var blockInfo = await rpcClient.SendCommandAsync(RPCOperations.getblock, blockHash.ToString());
                var blockHeight = (ulong)blockInfo.Result["height"]!;
                return blockHeight;
            });
        }

        public async Task ParseBlockAsync(RPCClientPoolFactory rpcClientPoolFactory, ulong blockNumber, Block block, bool usePending)
        {
            await _transactionService.ParseTransactionsAsync(blockNumber, block, usePending);
            await SetLastInscriptionTransfersBlockSync(blockNumber);
        }

        public Task<bool> SetLastInscriptionTransfersBlockSync(ulong blockNumber)
            => _blockRepository.SetLastInscriptionTransfersBlockSync(blockNumber);

        public Task<bool> SetLastReadModelsBlockSync(ulong blockNumber)
            => _blockRepository.SetLastReadModelsBlockSync(blockNumber);

        public Task<ulong?> GetLastInscriptionTransfersBlockSync()
            => _blockRepository.GetLastInscriptionTransfersBlockSync();

        public Task<ulong?> GetLastReadModelsBlockSync()
           => _blockRepository.GetLastReadModelsBlockSync();
    }
}
