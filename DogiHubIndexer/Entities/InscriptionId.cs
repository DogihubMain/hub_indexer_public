using NBitcoin;
using System.Globalization;

namespace DogiHubIndexer.Entities
{
    public class InscriptionId
    {
        public uint256 Txid { get; private set; }
        public uint Index { get; private set; }

        public InscriptionId(uint256 txid, uint index)
        {
            Txid = txid;
            Index = index;
        }

        public override string ToString()
        {
            return $"{Txid}i{Index}";
        }

        public static InscriptionId Parse(string s)
        {
            const int TxidLength = 64;

            if (s.Length < TxidLength + 2 || s[TxidLength] != 'i')
                throw new FormatException("Invalid InscriptionId format");

            var txidStr = s.Substring(0, TxidLength);
            var indexStr = s.Substring(TxidLength + 1);

            if (!uint.TryParse(indexStr, NumberStyles.None, CultureInfo.InvariantCulture, out var index))
                throw new FormatException("Invalid index format");

            return new InscriptionId(uint256.Parse(txidStr), index);
        }
    }
}
