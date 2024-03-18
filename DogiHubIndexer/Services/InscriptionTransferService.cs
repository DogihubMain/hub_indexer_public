using DogiHubIndexer.Entities.RawData;
using DogiHubIndexer.Entities.RawData.Extensions;
using DogiHubIndexer.Entities.ReadModels;
using DogiHubIndexer.Repositories.RawData.Interfaces;
using DogiHubIndexer.Repositories.ReadModels.Interfaces;
using DogiHubIndexer.Services.Interfaces;
using Serilog;
using System.Globalization;

namespace DogiHubIndexer.Services
{
    public class InscriptionTransferService : IInscriptionTransferService
    {
        private readonly IInscriptionTransferRawDataRepository _inscriptionTransferRepository;
        private readonly IInscriptionRawDataRepository _inscriptionRepository;
        private readonly ITokenInfoReadModelRepository _tokenInfoReadModelRepository;
        private readonly IUserBalanceTokensReadModelRepository _userBalanceTokensReadModelRepository;
        private readonly IUserBalanceDnsReadModelRepository _userBalanceDnsReadModelRepository;
        private readonly IUserBalanceNftsReadModelRepository _userBalanceNftsReadModelRepository;
        private readonly IUserBalanceDogemapsReadModelRepository _userBalanceDogemapsReadModelRepository;
        private readonly ILogger _logger;
        private readonly Options _options;

        private readonly IBlockService _blockService;

        public InscriptionTransferService(
            IInscriptionTransferRawDataRepository inscriptionTransferRepository,
            IBlockService blockService,
            IInscriptionRawDataRepository inscriptionRepository,
            ITokenInfoReadModelRepository tokenReadModelRepository,
            IUserBalanceTokensReadModelRepository userBalanceTokensReadModelRepository,
            ILogger logger,
            IUserBalanceDnsReadModelRepository userBalanceDnsReadModelRepository,
            IUserBalanceNftsReadModelRepository userBalanceNftsReadModelRepository,
            IUserBalanceDogemapsReadModelRepository userBalanceDogemapsReadModelRepository,
            Options options)
        {
            _inscriptionTransferRepository = inscriptionTransferRepository;
            _blockService = blockService;
            _inscriptionRepository = inscriptionRepository;
            _tokenInfoReadModelRepository = tokenReadModelRepository;
            _userBalanceTokensReadModelRepository = userBalanceTokensReadModelRepository;
            _logger = logger;
            _userBalanceDnsReadModelRepository = userBalanceDnsReadModelRepository;
            _userBalanceNftsReadModelRepository = userBalanceNftsReadModelRepository;
            _userBalanceDogemapsReadModelRepository = userBalanceDogemapsReadModelRepository;
            _options = options;
        }

        public async Task CalculateAndUpdateReadModelsAsync(ulong blockNumber, bool usePending)
        {
            var inscriptionTransfers = await _inscriptionTransferRepository.GetInscriptionTransfersByBlockAsync(blockNumber);

            if (inscriptionTransfers.Any())
            {
                _logger.Debug("{inscriptionTransfersCount} found", inscriptionTransfers.Count());

                foreach (var inscriptionTransfer in inscriptionTransfers)
                {
                    await ParseInscriptionTransferAsync(inscriptionTransfer, usePending);
                }
            }

            await _blockService.SetLastReadModelsBlockSync(blockNumber);
        }

        private async Task ParseInscriptionTransferAsync(InscriptionTransferRawData inscriptionTransferEntity, bool usePending)
        {
            if (usePending)
            {
                await HandleInscriptionTransferWithPendingAsync(inscriptionTransferEntity);
            }
            else
            {
                await HandleInscriptionTransferAsync(inscriptionTransferEntity);
            }
        }

        private async Task HandleInscriptionTransferWithPendingAsync(InscriptionTransferRawData inscriptionTransferEntity)
        {
            switch (inscriptionTransferEntity.InscriptionTransferType)
            {
                //TOKENS TYPES
                case InscriptionTransferType.DEPLOY:
                    await HandleDeployAsync(inscriptionTransferEntity);
                    break;
                case InscriptionTransferType.MINT:
                    await HandleMintAsync(inscriptionTransferEntity);
                    break;
                case InscriptionTransferType.INSCRIBE_TRANSFER:
                    await HandleInscribeTransferAsync(inscriptionTransferEntity);
                    break;

                ////PENDING TYPES
                case InscriptionTransferType.PENDING_TRANSFER:
                    await HandlePendingTransferAsync(inscriptionTransferEntity);
                    break;
                case InscriptionTransferType.PENDING_DNS:
                    await HandlePendingDnsAsync(inscriptionTransferEntity);
                    break;
                case InscriptionTransferType.PENDING_DOGEMAP:
                    await HandlePendingDogemapAsync(inscriptionTransferEntity);
                    break;
                case InscriptionTransferType.PENDING_NFT:
                    await HandlePendingNftAsync(inscriptionTransferEntity);
                    break;

                ////CONFIRMED TRANSFERS
                case InscriptionTransferType.CONFIRMED_TRANSFER:
                    await HandleConfirmedTransferAsync(inscriptionTransferEntity);
                    break;
                case InscriptionTransferType.CONFIRMED_DNS:
                    await HandleConfirmedDnsAsync(inscriptionTransferEntity);
                    break;
                case InscriptionTransferType.CONFIRMED_DOGEMAP:
                    await HandleConfirmedDogemapAsync(inscriptionTransferEntity);
                    break;
                case InscriptionTransferType.CONFIRMED_NFT:
                    await HandleConfirmedNftAsync(inscriptionTransferEntity);
                    break;

                default:
                    throw new ArgumentException("Invalid inscription transfer type", nameof(InscriptionTransferType));
            }

            //in all cases we deleted confirmed inscription transfer (like they are juste temporary to handle pending mode)
            //and others only if "delete transaction history" parameter is enabled
            if(_options.DeleteTransactionHistory
                || inscriptionTransferEntity.InscriptionTransferType == InscriptionTransferType.CONFIRMED_TRANSFER
                || inscriptionTransferEntity.InscriptionTransferType == InscriptionTransferType.CONFIRMED_DNS
                || inscriptionTransferEntity.InscriptionTransferType == InscriptionTransferType.CONFIRMED_DOGEMAP
                || inscriptionTransferEntity.InscriptionTransferType == InscriptionTransferType.CONFIRMED_NFT)
            {
                await DeleteInscriptionTransferAsync(inscriptionTransferEntity);
            }
        }

        private async Task HandleInscriptionTransferAsync(InscriptionTransferRawData inscriptionTransferEntity)
        {
            switch (inscriptionTransferEntity.InscriptionTransferType)
            {
                //TOKENS TYPES
                case InscriptionTransferType.DEPLOY:
                    await HandleDeployAsync(inscriptionTransferEntity);
                    break;
                case InscriptionTransferType.MINT:
                    await HandleMintAsync(inscriptionTransferEntity);
                    break;
                case InscriptionTransferType.INSCRIBE_TRANSFER:
                    await HandleInscribeTransferAsync(inscriptionTransferEntity);
                    break;

                ////IN NORMAL MODE, "PENDING STATUS" ARE USED DIRECTLY TO CALCULATE BALANCES

                case InscriptionTransferType.PENDING_TRANSFER:
                    await HandleTransferAsync(inscriptionTransferEntity);
                    break;
                case InscriptionTransferType.PENDING_DNS:
                    await HandleDnsAsync(inscriptionTransferEntity);
                    break;
                case InscriptionTransferType.PENDING_DOGEMAP:
                    await HandleDogemapAsync(inscriptionTransferEntity);
                    break;
                case InscriptionTransferType.PENDING_NFT:
                    await HandleNftAsync(inscriptionTransferEntity);
                    break;

                ////IN NORMAL MODE, "CONFIRMED STATUS" ARE NOT USED

                default:
                    throw new ArgumentException("Invalid inscription transfer type", nameof(InscriptionTransferType));
            }

            if (_options.DeleteTransactionHistory)
            {
                await DeleteInscriptionTransferAsync(inscriptionTransferEntity);
            }
        }

        #region DELOY
        private async Task HandleDeployAsync(InscriptionTransferRawData inscriptionTransferEntity)
        {
            TokenReadModel? tokenReadModel = await _tokenInfoReadModelRepository.GetAsync(inscriptionTransferEntity.Inscription!.TokenContent!.tick);

            // Only if not already deploy we can create token
            if (tokenReadModel != null)
            {
                return;
            }

            await _tokenInfoReadModelRepository.AddAsync(
                inscriptionTransferEntity.ToTokenReadModel(inscriptionTransferEntity.Date)
            );
        }
        #endregion

        #region MINT
        private async Task HandleMintAsync(InscriptionTransferRawData inscriptionTransferEntity)
        {
            var tokenReadModel = await _tokenInfoReadModelRepository.GetAsync(inscriptionTransferEntity.Inscription!.TokenContent!.tick);

            //mint works only if token already deployed
            if (tokenReadModel == null) return;
            if (!decimal.TryParse(inscriptionTransferEntity.Inscription!.TokenContent.amt, NumberStyles.Any, CultureInfo.InvariantCulture, out var amt)) return;

            //check if limit is not already reached
            bool isBelowLimit = amt <= tokenReadModel.Lim;
            if (!isBelowLimit) return;

            // check if there is suppy left to mint
            bool isSupplyLeft = tokenReadModel.CurrentSupply < tokenReadModel.Max;
            if (!isSupplyLeft) return;

            var amountToBeMinted = decimal.Min(amt, tokenReadModel.Max!.Value - tokenReadModel.CurrentSupply);

            var userBalanceReadModel = await _userBalanceTokensReadModelRepository.GetAsync(tokenReadModel.Tick, inscriptionTransferEntity.Receiver);

            if (userBalanceReadModel != null)
            {
                //update balance
                userBalanceReadModel.IncreaseBalance(amountToBeMinted);
                await _userBalanceTokensReadModelRepository.UpdateAsync(userBalanceReadModel);
            }
            else
            {
                //create balance
                await _userBalanceTokensReadModelRepository.AddAsync(new UserBalanceTokenReadModel()
                {
                    Address = inscriptionTransferEntity.Receiver,
                    TokenTick = tokenReadModel.Tick,
                    Pending = 0,
                    Transferable = 0,
                    Available = amountToBeMinted
                });
            }

            //update token current supply
            tokenReadModel.IncreaseCurrentSupply(amountToBeMinted);
            await _tokenInfoReadModelRepository.UpdateAsync(tokenReadModel);
        }
        #endregion

        #region INSCRIBRE TRANSFER
        private async Task HandleInscribeTransferAsync(InscriptionTransferRawData inscriptionTransferEntity)
        {
            var tokenReadModel = await _tokenInfoReadModelRepository.GetAsync(inscriptionTransferEntity.Inscription!.TokenContent!.tick);

            // token must be deployed
            if (tokenReadModel == null) return;

            var userBalanceReadModel = await _userBalanceTokensReadModelRepository.GetAsync(tokenReadModel.Tick, inscriptionTransferEntity.Receiver);

            //user must have a balance for this tick
            if (userBalanceReadModel == null) return;

            if (!decimal.TryParse(inscriptionTransferEntity.Inscription!.TokenContent.amt, NumberStyles.Any, CultureInfo.InvariantCulture, out var amt)) return;

            //check if user have enough funds in its balance for this tick
            if (userBalanceReadModel.Available < amt) return;

            //shift available to transferable balance
            userBalanceReadModel.ShiftAvailableToTransferableBalance(amt);
            await _userBalanceTokensReadModelRepository.UpdateAsync(userBalanceReadModel);
        }
        #endregion

        #region TRANSFERS
        private Task HandleTransferAsync(InscriptionTransferRawData inscriptionTransferEntity)
        {
            return HandleTransferInternalAsync(inscriptionTransferEntity, usePending: false);
        }

        private Task HandlePendingTransferAsync(InscriptionTransferRawData inscriptionTransferEntity)
        {
            return HandleTransferInternalAsync(inscriptionTransferEntity, usePending: true);
        }

        private async Task HandleTransferInternalAsync(InscriptionTransferRawData inscriptionTransferEntity, bool usePending)
        {
            var tokenReadModel = await _tokenInfoReadModelRepository.GetAsync(inscriptionTransferEntity.Inscription!.TokenContent!.tick);

            // token must be deployed
            if (tokenReadModel == null) return;

            if (string.IsNullOrWhiteSpace(inscriptionTransferEntity.Sender)) return;

            var senderBalanceReadModel = await _userBalanceTokensReadModelRepository.GetAsync(tokenReadModel.Tick, inscriptionTransferEntity.Sender);

            if (!decimal.TryParse(inscriptionTransferEntity.Inscription!.TokenContent.amt, NumberStyles.Any, CultureInfo.InvariantCulture, out var amt)) return;

            // check if the sender already has enough transferable balance for that tick ; if not ignore
            if (senderBalanceReadModel == null || senderBalanceReadModel?.Transferable < amt) return;

            // update transferable balance of sender
            senderBalanceReadModel!.DecreaseTransferableBalance(amt);
            await _userBalanceTokensReadModelRepository.UpdateAsync(senderBalanceReadModel);

            // update available pending balance of receiver (address)
            var receiverBalanceReadModel = await _userBalanceTokensReadModelRepository.GetAsync(tokenReadModel.Tick, inscriptionTransferEntity.Receiver);

            if (receiverBalanceReadModel != null)
            {
                //update receiver pending balance
                if (usePending)
                {
                    receiverBalanceReadModel.IncreasePendingBalance(amt);
                }
                else
                {
                    receiverBalanceReadModel.IncreaseBalance(amt);
                }
                await _userBalanceTokensReadModelRepository.UpdateAsync(receiverBalanceReadModel);
            }
            else
            {
                //create receiver balance
                await _userBalanceTokensReadModelRepository.AddAsync(new UserBalanceTokenReadModel()
                {
                    Address = inscriptionTransferEntity.Receiver,
                    TokenTick = tokenReadModel.Tick,
                    Pending = usePending ? amt : 0,
                    Transferable = 0,
                    Available = usePending ? 0 : amt
                });
            }
        }

        private async Task HandleConfirmedTransferAsync(InscriptionTransferRawData inscriptionTransferEntity)
        {
            var tokenReadModel = await _tokenInfoReadModelRepository.GetAsync(inscriptionTransferEntity.Inscription!.TokenContent!.tick);

            // token must be deployed
            if (tokenReadModel == null) return;

            var receiverBalanceReadModel = await _userBalanceTokensReadModelRepository.GetAsync(tokenReadModel.Tick, inscriptionTransferEntity.Receiver);

            //receiver must have a balance
            if (receiverBalanceReadModel == null) return;

            if (!decimal.TryParse(inscriptionTransferEntity.Inscription!.TokenContent.amt, NumberStyles.Any, CultureInfo.InvariantCulture, out var amt)) return;

            //receiver must have enough pending balance 
            if (receiverBalanceReadModel.Pending < amt) return;

            receiverBalanceReadModel.IncreaseConfirmedBalance(amt);
            await _userBalanceTokensReadModelRepository.UpdateAsync(receiverBalanceReadModel);
        }

        #endregion

        #region DNS

        private Task HandleDnsAsync(InscriptionTransferRawData inscriptionTransferEntity)
        {
            return HandleDnsInternalAsync(inscriptionTransferEntity, usePending: false);
        }

        private Task HandlePendingDnsAsync(InscriptionTransferRawData inscriptionTransferEntity)
        {
            return HandleDnsInternalAsync(inscriptionTransferEntity, usePending: true);
        }

        private async Task HandleDnsInternalAsync(InscriptionTransferRawData inscriptionTransferEntity, bool usePending)
        {
            if (!string.IsNullOrWhiteSpace(inscriptionTransferEntity.Sender))
            {
                var senderBalanceDnsReadModel = await _userBalanceDnsReadModelRepository.GetAsync(inscriptionTransferEntity.Inscription!.DnsContent!.name, inscriptionTransferEntity.Sender);

                if (senderBalanceDnsReadModel != null)
                {
                    await _userBalanceDnsReadModelRepository.DeleteAsync(inscriptionTransferEntity.Inscription!.DnsContent!.name, inscriptionTransferEntity.Sender);
                }
            }

            await _userBalanceDnsReadModelRepository.AddAsync(new UserBalanceDnsReadModel()
            {
                InscriptionId = inscriptionTransferEntity.InscriptionId,
                Name = inscriptionTransferEntity.Inscription!.DnsContent!.name,
                Address = inscriptionTransferEntity.Receiver,
                IsPending = usePending
            });
        }

        private async Task HandleConfirmedDnsAsync(InscriptionTransferRawData inscriptionTransferEntity)
        {
            if (string.IsNullOrWhiteSpace(inscriptionTransferEntity.Receiver)) return;

            var receiverBalanceDnsReadModel = await _userBalanceDnsReadModelRepository.GetAsync(inscriptionTransferEntity.Inscription!.DnsContent!.name, inscriptionTransferEntity.Receiver);
            if (receiverBalanceDnsReadModel == null) return;

            receiverBalanceDnsReadModel.ConfirmBalance();
            await _userBalanceDnsReadModelRepository.UpdateAsync(receiverBalanceDnsReadModel);
        }
        #endregion

        #region DOGEMAP

        private Task HandleDogemapAsync(InscriptionTransferRawData inscriptionTransferEntity)
        {
            return HandleDogemapInternalAsync(inscriptionTransferEntity, usePending: false);
        }

        private Task HandlePendingDogemapAsync(InscriptionTransferRawData inscriptionTransferEntity)
        {
            return HandleDogemapInternalAsync(inscriptionTransferEntity, usePending: true);
        }

        private async Task HandleDogemapInternalAsync(InscriptionTransferRawData inscriptionTransferEntity, bool usePending)
        {
            if (!string.IsNullOrWhiteSpace(inscriptionTransferEntity.Sender))
            {
                var senderBalanceDogemapsReadModel = await _userBalanceDogemapsReadModelRepository.GetAsync(inscriptionTransferEntity.Inscription!.DogemapContent!.Number, inscriptionTransferEntity.Sender);

                if (senderBalanceDogemapsReadModel != null)
                {
                    await _userBalanceDogemapsReadModelRepository.DeleteAsync(inscriptionTransferEntity.Inscription!.DogemapContent!.Number, inscriptionTransferEntity.Sender);
                }
            }

            await _userBalanceDogemapsReadModelRepository.AddAsync(new UserBalanceDogemapReadModel()
            {
                InscriptionId = inscriptionTransferEntity.InscriptionId,
                Number = inscriptionTransferEntity.Inscription!.DogemapContent!.Number,
                Address = inscriptionTransferEntity.Receiver,
                IsPending = usePending
            });
        }

        private async Task HandleConfirmedDogemapAsync(InscriptionTransferRawData inscriptionTransferEntity)
        {
            if (string.IsNullOrWhiteSpace(inscriptionTransferEntity.Receiver)) return;

            var receiverBalanceDogemapReadModel = await _userBalanceDogemapsReadModelRepository.GetAsync(inscriptionTransferEntity.Inscription!.DogemapContent!.Number, inscriptionTransferEntity.Receiver);
            if (receiverBalanceDogemapReadModel == null) return;

            receiverBalanceDogemapReadModel.ConfirmBalance();
            await _userBalanceDogemapsReadModelRepository.UpdateAsync(receiverBalanceDogemapReadModel);
        }
        #endregion

        #region NFT

        private Task HandleNftAsync(InscriptionTransferRawData inscriptionTransferEntity)
        {
            return HandleNftInternalAsync(inscriptionTransferEntity, usePending: false);
        }

        private Task HandlePendingNftAsync(InscriptionTransferRawData inscriptionTransferEntity)
        {
            return HandleNftInternalAsync(inscriptionTransferEntity, usePending: true);
        }

        private async Task HandleNftInternalAsync(InscriptionTransferRawData inscriptionTransferEntity, bool usePending)
        {
            if (!string.IsNullOrWhiteSpace(inscriptionTransferEntity.Sender))
            {
                var senderBalanceNftsReadModel = await _userBalanceNftsReadModelRepository.GetAsync(inscriptionTransferEntity.InscriptionId, inscriptionTransferEntity.Sender);

                if (senderBalanceNftsReadModel != null)
                {
                    await _userBalanceNftsReadModelRepository.DeleteAsync(inscriptionTransferEntity.InscriptionId, inscriptionTransferEntity.Sender);
                }
            }

            await _userBalanceNftsReadModelRepository.AddAsync(new UserBalanceNftReadModel()
            {
                InscriptionId = inscriptionTransferEntity.InscriptionId,
                Address = inscriptionTransferEntity.Receiver,
                IsPending = usePending
            });
        }

        private async Task HandleConfirmedNftAsync(InscriptionTransferRawData inscriptionTransferEntity)
        {
            if (string.IsNullOrWhiteSpace(inscriptionTransferEntity.Receiver)) return;

            var receiverBalanceNftsReadModel = await _userBalanceNftsReadModelRepository.GetAsync(inscriptionTransferEntity.InscriptionId, inscriptionTransferEntity.Receiver);
            if (receiverBalanceNftsReadModel == null) return;

            receiverBalanceNftsReadModel.ConfirmBalance();
            await _userBalanceNftsReadModelRepository.UpdateAsync(receiverBalanceNftsReadModel);
        }
        #endregion

        private async Task DeleteInscriptionTransferAsync(InscriptionTransferRawData inscriptionTransferEntity)
        {
            await _inscriptionTransferRepository.DeleteAsync(
                inscriptionTransferEntity.TransactionHash,
                inscriptionTransferEntity.BlockNumber,
                inscriptionTransferEntity.Inscription!);
        }

        public async Task CalculateAndUpdateReadModelsAsync(ulong startBlockNumber, ulong endBlockNumber, bool usePending)
        {
            var totalBlockToParse = endBlockNumber - startBlockNumber;
            var percentageIndex = 1;

            for (var i = startBlockNumber; i <= endBlockNumber; i++)
            {
                var percentage = Math.Round((decimal)percentageIndex / totalBlockToParse * 100, 2);
                _logger.Information("Calculating balances with block {blockNumber} - {percentage}%", i, percentage);
                await CalculateAndUpdateReadModelsAsync(i, usePending);
                await _blockService.SetLastReadModelsBlockSync(i);
                percentageIndex++;
            }
        }
    }
}
