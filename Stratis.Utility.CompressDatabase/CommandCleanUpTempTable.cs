using System;
using System.IO;
using DBreeze;

namespace Stratis.Utility.CompressDatabase
{
    public class CommandCleanUpTempTable
    {
        public int Execute(string dataDir)
        {
            Console.WriteLine($"Looking for any temp tables to clean up, data directory = {dataDir}");

            foreach (var repository in Consts.Repositories)
            {
                var repoFolder = Path.Combine(dataDir, repository);
                using (var dbreeze = new DBreezeEngine(repoFolder))
                {
                    Console.WriteLine($"Cleaning database in directory {repoFolder}");

                    var tables = dbreeze.Scheme.GetUserTableNamesStartingWith(string.Empty);
                    foreach (var table in tables)
                    {
                        if(table == Consts.TempTableName)
                        {
                            Console.WriteLine($"Removing a temp table from the {repository} database.");
                            dbreeze.Scheme.DeleteTable(Consts.TempTableName);
                        }
                    }
                }
            }

            Console.WriteLine($"Successfully completed cleaning up any temp tables from the database.");
            return 0;
        }
    }
}
