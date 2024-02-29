using DogiHubIndexer.Configuration;
using DogiHubIndexer.Entities.RawData;
using DogiHubIndexer.Helpers;
using DogiHubIndexer.Providers.Interfaces;
using DogiHubIndexer.Repositories.RawData.Interfaces;

namespace DogiHubIndexer.Repositories.RawData
{
    public class InscriptionTransferRawDataRepository : IInscriptionTransferRawDataRepository
    {
        private readonly IInscriptionRawDataRepository _inscriptionRawDataRepository;
        private readonly IRedisClient _redisClient;
        private readonly Options _options;

        public InscriptionTransferRawDataRepository(IRedisClient redisClient, Options options, IInscriptionRawDataRepository inscriptionRawDataRepository)
        {
            _redisClient = redisClient;
            _options = options;
            _inscriptionRawDataRepository = inscriptionRawDataRepository;
        }

        public async Task AddAsync(InscriptionTransferRawData entity, bool usePending)
        {
            await AddAsyncInternal(entity);
            if (usePending)
            {
                await AddConfirmedTypeEquivalentAsync(entity);
            }
        }

        private async Task AddConfirmedTypeEquivalentAsync(InscriptionTransferRawData entity)
        {
            var addConfirmedTransfer = entity.Inscription!.Type == InscriptionTypeEnum.Nft ? entity.Inscription.NftContent!.IsComplete : true;

            if (addConfirmedTransfer)
            {
                var confirmedTypeEquivalent = InscriptionTransferRawData.GetConfirmedTypeEquivalent(entity.InscriptionTransferType);
                if (confirmedTypeEquivalent.HasValue)
                {
                    //and we create a confirmed InscriptionTransfer at the current block height + required pending confirmation number
                    await AddAsyncInternal(new InscriptionTransferRawData()
                    {
                        BlockNumber = entity.BlockNumber + (ulong)_options.PendingConfirmationNumber,
                        InscriptionTransferType = confirmedTypeEquivalent.Value,
                        InputIndex = entity.InputIndex,
                        InscriptionId = entity.InscriptionId,
                        Inscription = entity.Inscription,
                        Receiver = entity.Receiver,
                        TransactionHash = entity.TransactionHash,
                        Sender = entity.Sender,
                        TransactionIndex = entity.TransactionIndex,
                        Date = entity.Date
                    });
                }
            }
        }

        private async Task AddAsyncInternal(InscriptionTransferRawData entity)
        {
            var inscriptionTransferHashKey = RedisKeys.GetInscriptionTransferHashKey(entity.BlockNumber, entity.TransactionIndex);
            var inscriptionTransferByBlockKey = RedisKeys.GetInscriptionTransferByBlockKey(entity.BlockNumber);
            var inscriptionTransferByInscriptionTypeKey = RedisKeys.GetInscriptionTransferByInscriptionTypeKey(entity.Inscription!.Type, entity.Inscription!.Name);

            var serialized = JsonHelper.Serialize(entity);

            await _redisClient.StringSetAsync(inscriptionTransferHashKey, serialized);

            await _redisClient.SetAddAsync(inscriptionTransferByBlockKey, inscriptionTransferHashKey);
            await _redisClient.SortedSetAddAsync(inscriptionTransferByInscriptionTypeKey, inscriptionTransferHashKey, entity.Date!.Value.ToUnixTimeSeconds());
        }

        public async Task<List<InscriptionTransferRawData>> GetInscriptionTransfersByBlockAsync(ulong blockNumber)
        {
            var inscriptionTransferByBlockKey = RedisKeys.GetInscriptionTransferByBlockKey(blockNumber);
            var hashKeys = await _redisClient.SetMembersAsync(inscriptionTransferByBlockKey);

            var tasks = hashKeys.Select(key => _redisClient.StringGetAsync(key!));
            var results = await Task.WhenAll(tasks);

            var inscriptionTransfersWithoutInscriptions = results.Select(result => JsonHelper.Deserialize<InscriptionTransferRawData>(result!)).ToList();

            if (inscriptionTransfersWithoutInscriptions.Count > 0)
            {
                var enrichTasks = inscriptionTransfersWithoutInscriptions.Select(inscriptionTransferEntity => _inscriptionRawDataRepository.GetAsync(inscriptionTransferEntity.InscriptionId)).ToArray();
                var enrichedTokens = await Task.WhenAll(enrichTasks);

                for (int i = 0; i < inscriptionTransfersWithoutInscriptions.Count; i++)
                {
                    inscriptionTransfersWithoutInscriptions[i].Inscription = enrichedTokens[i];
                }
                return inscriptionTransfersWithoutInscriptions.Where(x => x.Inscription != null).ToList();
            }
            return inscriptionTransfersWithoutInscriptions;
        }

        public async Task DeleteAsync(ulong blockNumber, int transactionIndex, InscriptionRawData inscriptionRawData)
        {
            var inscriptionTransferHashKey = RedisKeys.GetInscriptionTransferHashKey(blockNumber, transactionIndex);

            var inscriptionTransferByBlockKey = RedisKeys.GetInscriptionTransferByBlockKey(blockNumber);
            var inscriptionTransferByInscriptionTypeKey = RedisKeys.GetInscriptionTransferByInscriptionTypeKey(inscriptionRawData.Type, inscriptionRawData.Name);

            await _redisClient.KeyDeleteAsync(inscriptionTransferHashKey);

            await _redisClient.SetRemoveAsync(inscriptionTransferByBlockKey, inscriptionTransferHashKey);
            await _redisClient.SetRemoveAsync(inscriptionTransferByInscriptionTypeKey, inscriptionTransferHashKey);
        }
    }
}
