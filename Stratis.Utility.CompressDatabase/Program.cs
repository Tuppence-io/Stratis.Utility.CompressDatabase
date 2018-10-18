using System;
using System.IO;
using DBreeze;
using Microsoft.Extensions.CommandLineUtils;

namespace Stratis.Utility.CompressDatabase
{
    public class Program
    {
        private const string HelpOption = "-? | -h | --help";
        private const string TempTableName = "CompressTempTable";

        // Databases/Tables for the Stratis/DBreeze data files
        private static readonly string[] Repositories = new string[]
        {
                "Blocks",
                "Chain",
                "FinalizedBlock",
                "CoinView",
        };

        public static int Main(string[] args)
        {
            // Default the shell signal to success
            var shellSignal = 0;

            try
            {
                var app = new CommandLineApplication
                {
                    Name = "CompressDatabase",
                    ExtendedHelpText = "\n***** WARNING *****: Use this utility with extream caution!\n\n" +
                    "It is HIGHLY Recomented to make a backup copy of the database before using this utility.\n\n" +
                    "To compress the data this utility needs to unloaded and then reloaded it back into the database.  This requires that there be enough disk space to store two copies of the raw data.\n" +
                    "If you do not have enough space to do an inplace compression then you can use the compress external command to export/import the data using a directory on another disk.\n" +
                    "\n"
                };

                // Help option 
                app.HelpOption(HelpOption);

                // Compress Inplace Command and Options
                app.Command("compress-inplace", (command) =>
                {
                    command.Description = "Use temporary tables and copy data within the given database.";
                    command.HelpOption(HelpOption);

                    //var dataDirArgument = command.Argument("[datadir]", "Data Directory where the Stratis/DBreeze files are located.");
                    var dataDirOption = command.Option("-datadir <datadir>", "Data Directory where the Stratis/DBreeze files are located.", CommandOptionType.SingleValue);

                    command.OnExecute(() =>
                    {
                        var dataDir = dataDirOption.Value();
                        if (dataDir == null || !Directory.Exists(dataDir))
                        {
                            command.Error.WriteLine("Missing or invalid data directory option.");
                            command.ShowHelp();
                            return 1;
                        }

                        return CommandCompressInplace(dataDir);
                    });
                });

                // Default execution if no command was entered.
                app.OnExecute(() =>
                {
                    app.ShowHelp();
                    return 1;
                });

                shellSignal = app.Execute(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine($"Unhanled Exception: {ex.Message}");
                Console.WriteLine("Stack Trace");
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine();

                // Return a shell error signal
                shellSignal = 1;
            }

#if DEBUG
            // If in debug mode then add a console pause so we can see what happened
            // before the console closes
            Console.WriteLine("Press enter to exit...");
            Console.WriteLine();
            Console.ReadLine();
#endif
            // Return the shell signal 
            return shellSignal;
        }

        private static int CommandCompressInplace(string dataDir)
        {
            Console.WriteLine($"Compressing database inplace, data directory = {dataDir}");
            Console.WriteLine();

            foreach (var repository in Repositories)
            {
                var repoFolder = Path.Combine(dataDir, repository);

                using (var dbreeze = new DBreezeEngine(repoFolder))
                {
                    Console.WriteLine($"Opened database in directory {repoFolder}");

                    var tables = dbreeze.Scheme.GetUserTableNamesStartingWith(string.Empty);
                    Console.WriteLine($"Found {tables.Count} table in {repository} database.");

                    foreach (var table in tables)
                    {
                        using (var trans = dbreeze.GetTransaction())
                        {
                            var count = 0;
                            var total = trans.Count(table);
                            Console.WriteLine($"[{DateTime.Now}] Compressing {table} table in {repository} database with {total:n0} records.");

                            foreach (var row in trans.SelectForward<byte[], byte[]>(table))
                            {
                                // Add record to the temp table.
                                trans.Insert(TempTableName, row.Key, row.Value);

                                count++;
                                if (count % 10000 == 0)
                                {
                                    trans.Commit();
                                    Console.WriteLine($"[{DateTime.Now}] Processing {table} table, Copied {count:n0} of {total:n0} records.");
                                }
                            }

                            // Finished process the 
                            trans.Commit();
                            Console.WriteLine($"[{DateTime.Now}] Finished processing {table} table, Copied {count:n0} records.");
                        }

                        // Delete the table that we just created.
                        Console.WriteLine($"[{DateTime.Now}] Deleting old table {table}.");
                        dbreeze.Scheme.DeleteTable(table);

                        // Rename the temp table to the orginal table name.
                        Console.WriteLine($"[{DateTime.Now}] Renaming new table {table}.");
                        dbreeze.Scheme.RenameTable(TempTableName, table);
                    }
                }
            }

            Console.WriteLine($"Successfully completed compressing the database.");
            return 0;
        }
    }
}
