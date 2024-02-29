namespace DogiHubIndexer.Helpers
{
    public static class DateTimeHelper
    {
        public static long ConvertToUnixTimestamp(DateTimeOffset? dateTime)
        {
            return dateTime?.ToUnixTimeSeconds() ?? 0;
        }
    }
}
