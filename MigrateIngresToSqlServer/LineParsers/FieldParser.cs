namespace MigrateIngresToSqlServer.LineParsers
{
    public interface IFieldParser
    {
        string GetFieldValue(string value, string fieldType);
    }

    /// <summary>
    /// Get's the correct value from a raw value depending upon it's data (field) type.
    /// </summary>
    public class FieldParser : IFieldParser
    {
        public string GetFieldValue(string value, string fieldType)
        {
            if (fieldType == "money" && value.StartsWith("?"))
                return value.Replace("?", string.Empty);

            if (!(fieldType.StartsWith("varchar") | fieldType.StartsWith("char")))
                return value;

            if (value.Length == 1)
                return value;

            for (var index = 1; index <= value.Length; index++)
            {
                var startChars = value.Substring(0, index);
                var substring = value.Substring(index);

                var isInt = int.TryParse(startChars, out var intValue);

                if (index == 1 && !isInt)
                    return TidyValue(value);

                if (!isInt)
                    continue;

                if (substring.Length == intValue)
                    return TidyValue(substring);
            }

            return value;
        }

        private static string TidyValue(string valueToTidy)
        {
            // replace nulls and that funny � char with empty strings.
            return valueToTidy == "]^NULL^[" ? "" : valueToTidy.Replace("�", string.Empty);
        }
    }
}
