using System;
using System.Collections.Generic;
using System.Linq;

namespace MigrateIngresToSqlServer.LineParsers
{
    /// <summary>
    /// Get's all of the values from a .psa file line which are tab delimited.
    /// </summary>
    public class StringSplitLineParser : ILineParser
    {
        private readonly IFieldParser _fieldParser;

        public StringSplitLineParser(IFieldParser fieldParser)
        {
            _fieldParser = fieldParser;
        }

        public IEnumerable<string> ParseLine(string fileLine, Dictionary<string, string> fields)
        {
            const string separator = "\t";

            var entries = fileLine.Split(separator.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            for (var index = 0; index < entries.Length; index++)
            {
                var fieldType = fields.ElementAt(index).Value;
                entries[index] = _fieldParser.GetFieldValue(entries[index].Trim(), fieldType);
            }

            return entries;
        }
    }
}
