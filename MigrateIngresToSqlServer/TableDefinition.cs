using System.Collections.Generic;
using System.Diagnostics;

namespace MigrateIngresToSqlServer
{
    /// <summary>
    /// Name = the name of the source and destination table.
    /// Fields = each of the fields in the table, their name and their data type.
    /// FileName = The name of the file which contains the data for import.
    /// </summary>
    [DebuggerDisplay("{Name,nq}")]
    public class TableDefinition
    {
        public string Name { get; set; }
        public Dictionary<string, string> Fields = new Dictionary<string, string>();
        public string FileName { get; set; }
    }
}
