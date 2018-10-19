using System;
using System.IO;
using DBreeze;

namespace Stratis.Utility.CompressDatabase
{
    public class CommandCompressInplace
    {
        // Name of the temp file to use
        private const string TempTableName = "CompressTempTable";

        public int Execute(string dataDir)
        {
            Console.WriteLine($"Compressing database inplace, data directory = {dataDir}");
            Console.WriteLine();

            foreach (var repository in Consts.Repositories)
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
