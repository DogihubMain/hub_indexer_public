using DogiHubIndexer.Extensions;

namespace DogiHubIndexer.Helpers
{
    public class EnumerableRangeHelper
    {
        public static IEnumerable<ulong> CreateNumberSequence(
            ulong startBlockNumber,
            ulong endBlockNumber
            )
        {
            var blockNumbers = EnumerableExtensions.Range(startBlockNumber, endBlockNumber - startBlockNumber + 1);
            return blockNumbers;
        }
    }
}
