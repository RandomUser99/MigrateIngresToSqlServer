using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MigrateIngresToSqlServer.LineParsers;

namespace MigrateIngresToSqlServer.FileParsers
{
    /// <summary>
    /// This class with take a table definition created by the SchemaFileParser.cs class
    /// and use that data to find the applicable .psa file. It will then read the data in
    /// the .psa file, parse it into a DataTable and return the DataTable.
    /// </summary>
    public class DataFileParser
    {
        private readonly ILineParser _lineParser;

        public DataFileParser(ILineParser lineParser)
        {
            _lineParser = lineParser;
        }

        /// <summary>
        /// Returns a DataTable containing the data parsed from the .psa file with the appropriate data type for each field.
        /// </summary>
        /// <param name="tableDefinition">The table definition created from the SchemaFileParser.cs class.</param>
        /// <param name="sourceFilesFolderPath">The path to the folder where all of the .psa files are stored.</param>
        /// <returns></returns>
        public async Task<DataTable> ParseAsync(TableDefinition tableDefinition, string sourceFilesFolderPath)
        {
            using (var dataTable = new DataTable(tableDefinition.Name))
            {
                foreach (var field in tableDefinition.Fields)
                {
                    using (var column = DataColumnFactory.Get(field))
                    {
                        dataTable.Columns.Add(column);
                    }
                }

                var readFilePath = Path.Combine(sourceFilesFolderPath, tableDefinition.FileName);

                if (!File.Exists(readFilePath))
                    return null;

                using (var reader = new StreamReader(readFilePath, Encoding.ASCII))
                {
                    string line;

                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        PopulateData(tableDefinition, line, dataTable);
                    }
                }

                return dataTable;
            }
        }

        private void PopulateData(TableDefinition tableDefinition, string line, DataTable dataTable)
        {
            if (string.IsNullOrWhiteSpace(line))
                return;

            var lineData = _lineParser.ParseLine(line, tableDefinition.Fields).ToList();

            var dataRow = dataTable.NewRow();

            for (var index = 0; index < lineData.Count; index++)
            {
                var type = dataTable.Columns[index].DataType;

                try
                {
                    if (string.IsNullOrEmpty(lineData[index]))
                    {
                        dataRow[index] = DBNull.Value;
                    }
                    else
                    {
                        try
                        {
                            var castData = Convert.ChangeType(lineData[index], type);
                            dataRow[index] = castData;
                        }
                        catch (Exception ex) when (ex is InvalidCastException || ex is FormatException)
                        {
                            // If this got caught here, the type cannot be cast
                            // so we just assume it's bad data in the DB.
                            dataRow[index] = DBNull.Value;
                        }
                    }
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                    throw;
                }
            }

            dataTable.Rows.Add(dataRow);
        }
    }
}