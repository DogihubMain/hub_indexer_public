using DogiHubIndexer.Entities;
using DogiHubIndexer.Entities.RawData;
using DogiHubIndexer.Entities.RawData.Extensions;
using DogiHubIndexer.Repositories.RawData.Interfaces;
using DogiHubIndexer.Services.Interfaces;
using DogiHubIndexer.Validators;
using NBitcoin;
using Serilog;
using System.Diagnostics;

namespace DogiHubIndexer.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly IInscriptionTransferRawDataRepository _inscriptionTransferRepository;
        private readonly IInscriptionRawDataRepository _inscriptionRepository;
        private readonly IOutputRawDataRepository _outputRepository;

        private readonly IInscriptionService _inscriptionService;
        private readonly ILogger _logger;
        private readonly Options _options;

        public TransactionService(
            ILogger logger,
            IInscriptionTransferRawDataRepository inscriptionTransferRepository,
            IInscriptionRawDataRepository inscriptionRepository,
            IOutputRawDataRepository outputRepository,
            IInscriptionService inscriptionService,
            Options options)
        {
            _logger = logger;
            _inscriptionTransferRepository = inscriptionTransferRepository;
            _inscriptionRepository = inscriptionRepository;
            _outputRepository = outputRepository;
            _inscriptionService = inscriptionService;
            _options = options;
        }

        public async Task ParseTransactionsAsync(ulong blockNumber, Block block, bool usePending)
        {
            var stopwatch = Stopwatch.StartNew();

            var indexedTransactions = block.Transactions.Select((transaction, index) => new { Transaction = transaction, Index = index });

            var semaphore = new SemaphoreSlim(_options.CpuNumber);

            var tasks = indexedTransactions.Select(async indexedTransaction =>
            {
                await semaphore.WaitAsync();
                try
                {
                    await ParseTransactionAsync(
                        blockNumber,
                        block,
                        indexedTransaction.Index,
                        indexedTransaction.Transaction,
                        block.Transactions,
                        usePending);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);

            stopwatch.Stop();

            var totalTransactions = indexedTransactions.Count();
            var totalTimeMs = stopwatch.ElapsedMilliseconds;
            var averageTimePerTransaction = totalTransactions > 0 ? totalTimeMs / (double)totalTransactions : 0;

            _logger.Information($"{totalTransactions} transactions processed in {totalTimeMs} ms ({averageTimePerTransaction:F2}/tx) - {block.Header.BlockTime.ToString("dd/MM/yyyy HH:mm")}");
        }

        public async Task ParseTransactionAsync(
            ulong blockNumber,
            Block block,
            int transactionIndex,
            Transaction tx,
            List<Transaction> blockTransactions,
            bool usePending)
        {
            var inscriptionEntity = await TryGetAndSaveInscription(blockNumber, block, transactionIndex, tx);

            if (inscriptionEntity != null)
            {

                await SaveInscriptionTransferDirectlyIfInscriptionIsComplete(blockNumber, transactionIndex, block.Header.BlockTime, tx, inscriptionEntity, usePending);
                await SaveUtxo(tx, blockTransactions, inscriptionEntity);
            }

            await ProcessInputsForUtxoTransfersAsync(blockNumber, block, transactionIndex, tx, usePending);
        }

        private async Task SaveUtxo(Transaction tx, List<Transaction> blockTransactions, InscriptionRawData inscriptionEntity)
        {
            if (ShouldSaveUtxo(inscriptionEntity))
            {
                var inscriptionId = inscriptionEntity.Id.ToString();
                if (IsIncompleteNft(inscriptionEntity))
                {
                    //if it is an nft & not complete, save the output of the last tx of the current block
                    var lastNftTxHash = inscriptionEntity.NftContent!.TxIds.LastOrDefault();
                    var lastTx = blockTransactions.First(x => x.GetHash() == lastNftTxHash);
                    await SaveOutput0Async(lastTx!, inscriptionId);
                }
                else
                {
                    await SaveOutput0Async(tx, inscriptionId);
                }
            }
        }

        private bool ShouldSaveUtxo(InscriptionRawData inscriptionRawData)
        {
            //we only save UTXO if they are linked to a transaction that contain at least
            // - one token inscription and which is a inscribre transfer
            // - a dogemap, dns
            // - an nft from which we have not yet obtained all the transactions (isComplete = false)
            //sending a deploy or a mint is not relevant (we ignore it)
            return inscriptionRawData.Type == InscriptionTypeEnum.Token && inscriptionRawData.TokenContent!.op == InscriptionValidator.OpTransfer
                   || inscriptionRawData.Type == InscriptionTypeEnum.Dns
                   || inscriptionRawData.Type == InscriptionTypeEnum.Dogemap
                   || IsIncompleteNft(inscriptionRawData);
        }

        private bool IsIncompleteNft(InscriptionRawData inscriptionRawData)
        {
            return inscriptionRawData.Type == InscriptionTypeEnum.Nft && !inscriptionRawData.NftContent!.IsComplete;
        }

        private async Task SaveInscriptionTransferDirectlyIfInscriptionIsComplete(
            ulong blockNumber,
            int transactionIndex,
            DateTimeOffset transactionDate,
            Transaction tx,
            InscriptionRawData inscriptionEntity,
            bool usePending)
        {
            if (inscriptionEntity.Type != InscriptionTypeEnum.Nft || (inscriptionEntity.Type == InscriptionTypeEnum.Nft && inscriptionEntity.NftContent!.IsComplete))
            {
                //in deploy, mint, inscribre-transfer, dogemap, nft, dns cases we don't care about sender (interested for us only in real transfer)
                //Receiver is always get from the output 0
                var receiver = GetReceiver(tx, 0);
                if (receiver != null)
                {
                    var inscriptionTransferEntity = inscriptionEntity.ToInscriptionTransfer(
                        blockNumber,
                        inputIndex: 0,
                        receiver: receiver,
                        sender: null,
                        transactionHash: tx.GetHash(),
                        transactionIndex,
                        transactionDate,
                        inscriptionEntity.ToInscriptionTransferType()
                    );

                    await _inscriptionTransferRepository.AddAsync(inscriptionTransferEntity, usePending);
                }
                else
                {
                    _logger.Error("No receiver corresponding to input with index {inputIndex} for tx {transactionHash}", 0, tx.GetHash());
                }
            }
        }

        private async Task SaveOutput0Async(Transaction tx, string inscriptionId)
        {
            uint outputIndex = 0;

            var transactionHash = tx.GetHash().ToString();

            var outputEntity = new OutputRawData()
            {
                InscriptionId = inscriptionId,
                RelatedAddress = GetDestinationAddress(tx.Outputs[outputIndex])
            };
            await _outputRepository.AddAsync(transactionHash, outputIndex, outputEntity);
        }

        public async Task<InscriptionRawData?> TryGetAndSaveInscription(
            ulong blockNumber,
            Block block,
            int transactionIndex,
            Transaction tx)
        {
            var transactionHash = tx.GetHash();

            if (!tx.Inputs.Any())
            {
                _logger.Error("No input 0 for tx {transactionHash}", transactionHash);
                return null;
            }

            var txIn = tx.Inputs[0];

            //extract inscription if exists in input0
            var inscriptionEntity = _inscriptionService.ExtractInscriptionFromScriptSig(
                blockNumber,
                block,
                tx,
                txIn,
                genesisTxId: transactionHash,
                inscriptionId: new InscriptionId(transactionHash, index: 0));

            if (inscriptionEntity != null)
            {
                _logger.Debug("inscription {inscriptionEntityId} extracted from block {blockNumber}", inscriptionEntity.Id, blockNumber);

                await _inscriptionRepository.AddAsync(inscriptionEntity);
                return inscriptionEntity;
            }
            return null;
        }

        private async Task ProcessInputsForUtxoTransfersAsync(
            ulong blockNumber,
            Block block,
            int transactionIndex,
            Transaction tx,
            bool usePending)
        {
            if (tx.Inputs.Any())
            {
                //we browse all inputs to see if there is a matching UTXO in order to create a real inscription transfer if needed
                for (int inputIndex = 0; inputIndex < tx.Inputs.Count; inputIndex++)
                {
                    var txIn = tx.Inputs[inputIndex];
                    await HandlePreviousUtxoIfExists(blockNumber, block, transactionIndex, tx, txIn, usePending);
                }
            }
        }

        private async Task HandlePreviousUtxoIfExists(
            ulong blockNumber,
            Block block,
            int transactionIndex,
            Transaction tx,
            TxIn? txIn,
            bool usePending)
        {
            if (txIn == null) return;
            //we check first if we find a previous tx with this index that have an inscription in our db
            var previousOutputTxHash = txIn.PrevOut.Hash;
            var previousOutputIndex = txIn.PrevOut.N;

            var previousOutput = await _outputRepository.GetAsync(previousOutputTxHash, previousOutputIndex);

            if (previousOutput == null) return;
            var previousInscription = await _inscriptionRepository.GetAsync(previousOutput.InscriptionId);

            if (previousInscription == null) return;

            if (previousInscription.Type == InscriptionTypeEnum.Token)
            {
                await HandlePreviousUtxoToken(
                    blockNumber,
                    transactionIndex,
                    block.Header.BlockTime,
                    tx,
                    previousOutputTxHash,
                    previousOutputIndex,
                    previousOutput,
                    previousInscription,
                    usePending);
            }
            else if (previousInscription.Type == InscriptionTypeEnum.Nft)
            {
                await HandlePreviousUtxtoNft(
                    blockNumber,
                    block,
                    transactionIndex,
                    tx,
                    previousOutputTxHash,
                    previousOutputIndex,
                    previousOutput,
                    previousInscription,
                    usePending);
            }
        }

        private async Task HandlePreviousUtxtoNft(
            ulong blockNumber,
            Block block,
            int transactionIndex,
            Transaction tx,
            uint256 previousOutputTxHash,
            uint previousOutputIndex,
            OutputRawData previousOutput,
            InscriptionRawData relatedInscription,
            bool usePending)
        {
            var isComplete = relatedInscription.NftContent!.IsComplete;
            if (tx.Inputs.Any() && !isComplete)
            {
                //get all transactions related to the current nft
                var newTransactions = _inscriptionService.GetNftTransactionIdsInCurrentBlock(block, tx, tx.Inputs[0], out isComplete, isGenesis: false);

                if (newTransactions != null)
                {
                    var newTransactionIds = newTransactions.Select(x => x.GetHash());
                    relatedInscription.NftContent!.TxIds.AddRange(newTransactionIds);
                    relatedInscription.NftContent!.IsComplete = isComplete;
                    await _inscriptionRepository.UpdateAsync(relatedInscription);

                    //delete current outdated utxo and save the new one
                    await _outputRepository.DeleteAsync(previousOutputTxHash, previousOutputIndex);

                    //need last transaction used for the nft 
                    var lastTx = newTransactions.Last();
                    await SaveOutput0Async(lastTx, relatedInscription.Id.ToString());
                }
            }

            if (isComplete)
            {
                //if inscription is complete so create an nft InscriptionTransfer 
                var receiver = GetReceiver(tx, 0);

                if (receiver != null)
                {
                    //Here Create an InscriptionTransfer of type InscriptionTransferType.NFT with the receiver & sender
                    var inscriptionTransferEntity = relatedInscription.ToInscriptionTransfer(
                        blockNumber,
                        previousOutputIndex,
                        receiver: receiver!,
                        sender: previousOutput.RelatedAddress,
                        transactionHash: tx.GetHash(),
                        transactionIndex,
                        block.Header.BlockTime,
                        InscriptionTransferType.PENDING_NFT
                        );

                    await _inscriptionTransferRepository.AddAsync(inscriptionTransferEntity, usePending);

                }
                else
                {
                    _logger.Error("No receiver corresponding to output with index {inputIndex} for tx {transactionHash}", 0, tx.GetHash());
                }
            }

            //previous UTXO was used so we can delete the output
            await _outputRepository.DeleteAsync(previousOutputTxHash, previousOutputIndex);
        }

        private async Task HandlePreviousUtxoToken(
            ulong blockNumber,
            int transactionIndex,
            DateTimeOffset transactionDate,
            Transaction tx,
            uint256 previousOutputTxHash,
            uint previousOutputIndex,
            OutputRawData previousOutput,
            InscriptionRawData previousInscription,
            bool usePending)
        {
            var receiver = GetReceiver(tx, 0);

            if (receiver != null)
            {
                //Here Create an InscriptionTransfer of type InscriptionTransferType.TRANSFER with the receiver & sender
                var inscriptionTransferEntity = previousInscription.ToInscriptionTransfer(
                    blockNumber,
                    previousOutputIndex,
                    receiver: receiver!,
                    sender: previousOutput.RelatedAddress,
                    transactionHash: tx.GetHash(),
                    transactionIndex,
                    transactionDate,
                    InscriptionTransferType.PENDING_TRANSFER
                    );

                await _inscriptionTransferRepository.AddAsync(inscriptionTransferEntity, usePending);

                //previous UTXO was used so we can delete the output
                await _outputRepository.DeleteAsync(previousOutputTxHash, previousOutputIndex);
            }
            else
            {
                _logger.Error("No receiver corresponding to output with index {inputIndex} for tx {transactionHash}", 0, tx.GetHash());
            }
        }

        public string? GetReceiver(Transaction tx, uint inputIndex)
        {
            try
            {
                var matchingOutput = tx.Outputs[inputIndex];
                if (matchingOutput == null)
                {
                    return null;
                }
                return GetDestinationAddress(matchingOutput);
            }
            catch
            {
                _logger.Error("No output corresponding to input with index {inputIndex} for tx {transacctionHash}", inputIndex, tx.GetHash());
                return null;
            }
        }

        public string? GetDestinationAddress(TxOut txOut)
        {
            return txOut.ScriptPubKey.GetDestinationAddress(NBitcoin.Altcoins.Dogecoin.Instance.Mainnet)?.ToString();
        }
    }
}
