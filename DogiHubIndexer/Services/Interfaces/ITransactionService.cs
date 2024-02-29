using NBitcoin;

namespace DogiHubIndexer.Services.Interfaces
{
    public interface ITransactionService
    {
        Task ParseTransactionsAsync(ulong blockNumber, Block block, bool usePending);
    }
}
