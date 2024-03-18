using DogiHubIndexer.Entities.ReadModels;
using System.Globalization;

namespace DogiHubIndexer.Entities.RawData.Extensions
{
    public static class InscriptionTransferRawDataExtensions
    {
        public static TokenReadModel ToTokenReadModel(
            this InscriptionTransferRawData inscriptionTransferEntity,
            DateTimeOffset date)
        {

            var styles = NumberStyles.AllowLeadingWhite
                         | NumberStyles.AllowTrailingWhite
                         | NumberStyles.AllowDecimalPoint;

            return new TokenReadModel()
            {
                Tick = inscriptionTransferEntity.Inscription!.TokenContent!.tick,
                Protocol = inscriptionTransferEntity.Inscription!.TokenContent.p,
                Lim = decimal.Parse(inscriptionTransferEntity.Inscription!.TokenContent.lim!, styles, CultureInfo.InvariantCulture),
                Max = decimal.Parse(inscriptionTransferEntity.Inscription!.TokenContent.max!, styles, CultureInfo.InvariantCulture),
                CurrentSupply = 0,
                TransactionHash = inscriptionTransferEntity.Inscription!.GenesisTxId.ToString(),
                Date = date,
                Decimal = int.TryParse(inscriptionTransferEntity.Inscription!.TokenContent.dec!, styles, CultureInfo.InvariantCulture, out int decInt)
                        ? decInt
                        : (int?)null,
                DeployedBy = inscriptionTransferEntity.Receiver
            };
        }
    }
}
