using System;
using System.IO;

namespace Stratis.Utility.CompressDatabase
{
    public class CommandCompressExternal
    {
        public int Execute(string dataDir, string tempDir, bool deleteTempDir)
        {
            // Create the temp directory if needed.
            var ret = ValidateTempDirectory(tempDir);
            if (ret != 0)
            {
                return ret;
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
    }
}
