using System;
using System.IO;
using DBreeze;

namespace Stratis.Utility.CompressDatabase
{
    public class CommandCompressExternal
    {
        public int Execute(string dataDir, string tempDir, bool deleteTempDir)
        {
            Console.WriteLine($"Compressing database external, data directory = {dataDir}, temp directory = {tempDir}");

            // Create the temp directory if needed.
            var ret = ValidateTempDirectory(tempDir);
            if (ret != 0)
            {
                return ret;
            }

            CopyDatabaseData(dataDir, tempDir);

            DeleteDatabaseFiles(dataDir);
            CopyDatabaseFiles(dataDir, tempDir);

            if(deleteTempDir)
            {
                var tempDirInfo = new DirectoryInfo(tempDir);
                if(tempDirInfo.Exists)
                {
                    Console.WriteLine($"Deleting temp directory {tempDir}");
                    tempDirInfo.Delete(true);
                }
            }

            Console.WriteLine($"Successfully completed compressing the database.");
            return 0;
        }

        public int ValidateTempDirectory(string tempDir)
        {
            // If the directory doesn't exist then create it.
            try
            {
                if (!Directory.Exists(tempDir))
                {
                    Console.WriteLine($"Creating directory {tempDir}.");
                    Directory.CreateDirectory(tempDir);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unable to validate or create the directory {tempDir}.");
                Console.WriteLine(ex.Message);
                return 1;
            }

            // This directory should not have any files in it.  If it does then we warn the user and exit.
            // I don't want to overwrite (or duplicate) any data that may already exist.  The user must clean it up before running the process.
            var entries = Directory.GetFileSystemEntries(tempDir);
            if (entries != null && entries.Length > 0)
            {
                Console.WriteLine($"The directory {tempDir} is not empty.  The directory must be empty so that the process does not overwritten or delete anything that it shouldn't.");
                Console.WriteLine($"Please remove all files and sub-directories from the {tempDir} or select a different location.");
                return 1;
            }

            return 0;
        }

        private void CopyDatabaseData(string dataDir, string tempDir)
        {
            foreach (var repository in Consts.Repositories)
            {
                var sourceRepoDir = Path.Combine(dataDir, repository);
                var targetRepoDir = Path.Combine(tempDir, repository);

                Console.WriteLine($"Copying repository {repository}");

                using (var sourceDBreeze = new DBreezeEngine(sourceRepoDir))
                using (var targetDBreeze = new DBreezeEngine(targetRepoDir))
                {
                    CopyTablesData(sourceDBreeze, targetDBreeze, repository);
                }
            }
        }

        private void CopyTablesData(DBreezeEngine sourceDBreeze, DBreezeEngine targetDBreeze, string repository)
        {
            var tables = sourceDBreeze.Scheme.GetUserTableNamesStartingWith(string.Empty);
            Console.WriteLine($"Found {tables.Count} table in {repository} database.");

            foreach (var table in tables)
            {
                using (var soruceTrans = sourceDBreeze.GetTransaction())
                using (var targetTrans = targetDBreeze.GetTransaction())
                {
                    var count = 0;
                    var total = soruceTrans.Count(table);
                    Console.WriteLine($"[{DateTime.Now}] Compressing {table} table in {repository} database with {total:n0} records.");

                    foreach (var row in soruceTrans.SelectForward<byte[], byte[]>(table))
                    {
                        // Add record to the table in the target database
                        targetTrans.Insert(table, row.Key, row.Value);

                        count++;
                        if (count % 10000 == 0)
                        {
                            targetTrans.Commit();
                            Console.WriteLine($"[{DateTime.Now}] Processing {table} table, Copied {count:n0} of {total:n0} records.");
                        }
                    }

                    // Finished copying of the records
                    targetTrans.Commit();
                    Console.WriteLine($"[{DateTime.Now}] Finished copying {table} table, Copied {count:n0} records.");
                }
            }
        }

        private void DeleteDatabaseFiles(string dataDir)
        {
            foreach (var repository in Consts.Repositories)
            {
                Console.WriteLine($"Removing files from repository {repository}");

                var repoDir = Path.Combine(dataDir, repository);
                var dirInfo = new DirectoryInfo(repoDir);
                if (dirInfo.Exists)
                {
                    dirInfo.Delete(true);
                }
            }
        }

        private void CopyDatabaseFiles(string dataDir, string tempDir)
        {
            foreach (var repository in Consts.Repositories)
            {
                Console.WriteLine($"Copying files from repository {repository}");

                // Now we are move the files from the temp directory (source) to the data directory (target).
                var sourceRepoDir = Path.Combine(tempDir, repository);
                var targetRepoDir = Path.Combine(dataDir, repository);

                // Make sure that the target directory exist
                if (!Directory.Exists(targetRepoDir))
                {
                    Directory.CreateDirectory(targetRepoDir);
                }

                // Copy all the files in the folder
                var sourceDirInfo = new DirectoryInfo(sourceRepoDir);
                foreach(var fileInfo in sourceDirInfo.GetFiles())
                {
                    Console.WriteLine($"Copy file {fileInfo.Name} starting at {DateTime.Now}");
                    fileInfo.CopyToDirectory(targetRepoDir, ReportFileProgress);
                    Console.WriteLine($"Copy file {fileInfo.Name} finished at {DateTime.Now}");
                }
            }
        }

        private void ReportFileProgress(FileInfo fileInfo, long bytesCopied, long totalBytes)
        {
            double percentDone = (double) bytesCopied / (double) totalBytes;

            Console.WriteLine($"[{DateTime.Now}] {fileInfo.Name} copied {percentDone:p} ({bytesCopied}/{totalBytes}) ");
        }
    }
}
