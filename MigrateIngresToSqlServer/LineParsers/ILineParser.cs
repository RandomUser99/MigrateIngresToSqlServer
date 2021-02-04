using System.Collections.Generic;

namespace MigrateIngresToSqlServer.LineParsers
{
    public interface ILineParser
    {
        IEnumerable<string> ParseLine(string fileLine, Dictionary<string, string> fields);
    }
}