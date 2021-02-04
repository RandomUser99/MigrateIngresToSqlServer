using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MigrateIngresToSqlServer.FileParsers
{
    /// <summary>
    /// The copy.in file is a metadata file created by Ingres as part of the COPYDB command.
    /// https://docs.actian.com/ingres/10.2/index.html#page/CommandRef%2Fcopydb_Command--Copy_and_Restore_a_Database.htm
    /// </summary>
    public class SchemaFileParser
    {
        /// <summary>
        /// Retrieves details for each table defined in the copy.in file.
        /// </summary>
        /// <param name="folderPath">Path to the folder containing the copy.in file.</param>
        /// <returns>And IEnumerable of TableDefinitions.</returns>
        public async Task<IEnumerable<TableDefinition>> ParseToTableDefinitionsAsync(string folderPath)
        {
            var copyInFilePath = Path.Combine(folderPath, "copy.in");

            using var reader = new StreamReader(copyInFilePath, Encoding.ASCII);

            return await ParseFileAsync(reader);
        }

        private static async Task<List<TableDefinition>> ParseFileAsync(TextReader reader)
        {
            var isTableDetail = false;
            var tableCompleted = false;

            TableDefinition definition = null;

            var definitions = new List<TableDefinition>();

            string fileLine;

            while ((fileLine = await reader.ReadLineAsync()) != null)
            {
                if (IsNotOfInterest(fileLine))
                    continue;

                if (!isTableDetail && fileLine.StartsWith("create table ") && !IsTempOrIiTable(fileLine))
                {
                    definition = new TableDefinition { Name = GetTableName(fileLine) };
                    isTableDetail = true;
                    continue;
                }

                if (isTableDetail && !tableCompleted)
                {
                    if (IsEndOfTableDetails(fileLine))
                    {
                        tableCompleted = true;
                        continue;
                    }

                    var value = GetFieldValue(fileLine);

                    if (IsOfInterest(value))
                    {
                        var type = GetFieldType(fileLine);
                        definition.Fields.Add(value, type);
                    }

                    continue;
                }

                if (isTableDetail && fileLine.StartsWith("from ") && !IsTempOrIiTable(fileLine))
                {
                    definition.FileName = GetFileName(fileLine);

                    if (HasDesiredData(definition))
                        definitions.Add(definition);

                    definition = null;
                    tableCompleted = false;
                    isTableDetail = false;
                }
            }

            var orderedDefinitions = definitions.OrderBy(x => x.Name).ToList();

            return orderedDefinitions;
        }

        private static bool IsOfInterest(string field)
        {
            return field != "nl";
        }

        private static bool IsTempOrIiTable(string line)
        {
            return line.Contains("tt_") || line.Contains("ii_");
        }

        private static bool IsEndOfTableDetails(string line)
        {
            const string endPattern = ")";
            return line.StartsWith(endPattern);
        }

        private static string GetTableName(string line)
        {
            return line.Split(' ')[2].Replace("(", string.Empty);
        }

        private static string GetFieldValue(string line)
        {
            return line.Split(' ')[0].Trim();
        }

        private static string GetFieldType(string line)
        {
            return line.Split(' ')[1].Replace(",", string.Empty);
        }

        private static bool HasDesiredData(TableDefinition definition)
        {
            return definition.FileName.Contains(".psa") && !definition.Name.Contains("temp");
        }

        private static string GetFileName(string line)
        {
            return line.Split('/')[4].Replace("'", string.Empty);
        }

        private static bool IsNotOfInterest(string line)
        {
            return string.IsNullOrEmpty(line) || line.StartsWith("copy ii_");
        }
    }
}
