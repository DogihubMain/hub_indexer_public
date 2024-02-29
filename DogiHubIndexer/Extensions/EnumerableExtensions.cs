namespace DogiHubIndexer.Extensions
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<ulong> Range(ulong start, ulong count)
        {
            for (ulong i = start; i < start + count; i++)
            {
                yield return i;
            }
        }
    }
}
