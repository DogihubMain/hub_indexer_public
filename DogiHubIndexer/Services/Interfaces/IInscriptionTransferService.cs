namespace DogiHubIndexer.Services.Interfaces
{
    public interface IInscriptionTransferService
    {
        Task CalculateAndUpdateReadModelsAsync(ulong blockNumber, bool usePending);
        Task CalculateAndUpdateReadModelsAsync(ulong startBlockNumber, ulong endBlockNumber, bool usePending);
    }
}