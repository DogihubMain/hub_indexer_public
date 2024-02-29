using System.Text.Json;

namespace DogiHubIndexer.Validators
{
    public class JsonValidator
    {
        private const string P = "p";
        private const string Op = "op";
        private const string Max = "max";
        private const string Lim = "lim";
        private const string Amt = "amt";
        private const string Tick = "tick";
        private const string Type = "type";
        private const string Name = "name"; //DNS

        public static bool TryGetValidJson(string content, out JsonDocument? jsonDocument)
        {
            jsonDocument = null;
            try
            {
                jsonDocument = JsonDocument.Parse(content);
                //check if content is a valid json
                if (jsonDocument == null) return false;
                if (jsonDocument.RootElement.ValueKind != JsonValueKind.Object) return false;
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        public static bool AreFieldsStrings(JsonDocument jsonDocument)
        {
            try
            {
                foreach (var element in jsonDocument.RootElement.EnumerateObject())
                {
                    if (element.Name == P
                        || element.Name == Op
                        || element.Name == Max
                        || element.Name == Lim
                        || element.Name == Amt
                        || element.Name == Tick
                        || element.Name == Type
                        || element.Name == Name)
                    {
                        if (element.Value.ValueKind != JsonValueKind.String)
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}
