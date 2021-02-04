using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MigrateIngresToSqlServer
{
    using Data;
    using FileParsers;
    using LineParsers;

    class Program
    {
        private static readonly string SourceFilesFolderPath = ConfigurationManager.AppSettings["SourceFilesFolderPath"];
        private static readonly string LogFolderPath = ConfigurationManager.AppSettings["LogFolderPath"];

        private const bool SaveToDatabase = false;

        static async Task Main()
        {
            var definitions = await GenerateTableDefinitionsAsync();

            await MigrateDataAsync(definitions);
        }

        private static async Task<TableDefinition[]> GenerateTableDefinitionsAsync()
        {
            var copyInParser = new SchemaFileParser();

            var tableDefinitions = await copyInParser.ParseToTableDefinitionsAsync(SourceFilesFolderPath);

            return tableDefinitions as TableDefinition[] ?? tableDefinitions.ToArray();
        }

        private static async Task MigrateDataAsync(IReadOnlyCollection<TableDefinition> definitions)
        {
            using var connectionFactory = new DbConnectionFactory();

            var logPath = Path.Combine(LogFolderPath, $"MigrateIngresToSqlServer-{DateTime.Now:yyyyMMdd_HHmmss}.csv");

            using var writer = new StreamWriter(logPath) { AutoFlush = true };

            await writer.WriteLineAsync("FileName,TableName,FileRowCount,DatabaseRowCount,CountsMatch");

            var repository = new SqlRepository(connectionFactory);

            var parser = new DataFileParser(new StringSplitLineParser(new FieldParser()));

            var total = definitions.Count;
            var count = 1;

            foreach (var table in definitions)
            {
                SetConsoleTextColour(count);

                WriteTableDetailToConsole(table, count, total);

                if (SaveToDatabase)
                {
                    if (await repository.TableExistsAsync(table.Name))
                    {
                        Console.WriteLine("Table exists in DB already, skipping.");
                        count++;
                        continue;
                    }
                }

                using (var dataTable = await parser.ParseAsync(table, SourceFilesFolderPath))
                {
                    if (dataTable == null)
                    {
                        Console.WriteLine("File not found or no data in file to write to DB, skipping.");
                        count++;
                        continue;
                    }

                    if (SaveToDatabase)
                    {
                        Console.WriteLine("Writing to DB.");
                        await repository.SaveAsync(dataTable);
                    }

                    var savedRowsCount = await repository.GetTableRowCountAsync(table.Name);

                    await WriteDetailToLog(writer, table, dataTable.Rows.Count, savedRowsCount);
                }

                count++;
            }
        }

        private static void WriteTableDetailToConsole(TableDefinition table, int count, int total)
        {
            Console.WriteLine($"{table.FileName} => {table.Name} ({count} of {total}). Time: {DateTime.Now:T}");
        }

        private static async Task WriteDetailToLog(TextWriter writer, TableDefinition definition, int datatableRowCount, int databaseRowCount)
        {
            await writer.WriteLineAsync($"{definition.FileName},{definition.Name},{datatableRowCount},{databaseRowCount},{databaseRowCount == datatableRowCount}");
        }

        private static void SetConsoleTextColour(int count)
        {
            Console.ForegroundColor = count % 2 == 0 ? ConsoleColor.Yellow : ConsoleColor.Cyan;
        }
    }
}