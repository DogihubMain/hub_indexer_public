using DogiHubIndexer.Factories;
using NBitcoin;

namespace DogiHubIndexer.Services.Interfaces
{
    public interface IBlockService
    {
        Task<Block> GetBlockAsync(RPCClientPoolFactory rpcClientPoolFactory, ulong blockNumber);
        Task<Block> GetBlockAsync(RPCClientPoolFactory rpcClientPoolFactory, uint256 blockHash);
        Task<ulong> GetBlockHeightAsync(RPCClientPoolFactory rpcClientPoolFactory, uint256 blockHash);
        Task ParseBlockAsync(RPCClientPoolFactory rpcClientPoolFactory, ulong blockNumber, Block block, bool usePending);
        Task<bool> SetLastInscriptionTransfersBlockSync(ulong blockNumber);
        Task<bool> SetLastReadModelsBlockSync(ulong blockNumber);
        Task<ulong?> GetLastInscriptionTransfersBlockSync();
        Task<ulong?> GetLastReadModelsBlockSync();
    }
}
