namespace DogiHubIndexer.Validators
{
    public static class MimeTypeValidator
    {
        public static bool IsMimeTypeFormatValid(string mimeType)
        {
            //just check at least there is only one / in the mimetype separting the type & subtype
            return !string.IsNullOrEmpty(mimeType) && mimeType.Contains("/") && mimeType.IndexOf("/") == mimeType.LastIndexOf("/");
        }
    }
}
