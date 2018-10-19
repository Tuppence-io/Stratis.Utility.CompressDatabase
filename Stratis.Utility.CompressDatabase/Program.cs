using System;
using System.IO;
using Microsoft.Extensions.CommandLineUtils;

namespace Stratis.Utility.CompressDatabase
{
    public class Program
    {
        private const string HelpOption = "-? | -h | --help";

        public static int Main(string[] args)
        {
            // Default the shell signal to success
            var shellSignal = 0;

            try
            {
                var app = new CommandLineApplication
                {
                    Name = "CompressDatabase",
                    ExtendedHelpText =
                        "\n***** WARNING *****: Use this utility with extreme caution!!!\n\n" +

                        "It is *** HIGHLY RECOMENDED *** that you make a backup copy of the DBreeze database before using this utility.\n\n" +

                        "Do *** NOT *** have any other application or processes interacting with the database files while this process is running. " +
                        "This utility was not designed to process the data while the database is online (other application are reading/writing data). " +
                        "You *** MUST *** take any other applications and/or services off line before running this utility.\n\n" +

                        "To compress the data this utility needs to unloaded the data and then reloaded it back into the database tables.  " +
                        "This requires that there be enough disk space to store two copies of the raw data on the disk drive.  " +
                        "If you do not have enough space to do an in-place compression then you can use the external command to export/import " +
                        "the data using a directory on another disk. However this process is very slow and makes a new copy of the database. " +
                        "Then copies the database files back to the original data directory (so it ends up coping the data twice).\n\n" +
                        
                        "The overall result of this process has two benefits, the first is that only the active content is copied. " +
                        "Deleted data that is still in the database (but hidden) is not copied.  So the overall size of the files on disk " +
                        "will be reduced.  The second is that the data will be reorganized in the new tables so that all the primary keys " +
                        "are stored in ascending order which is the most optimal. This will improve overall performance of the database.\n\n" +

                        "***** WARNING *****: Oh, Have I mentioned that you should use this utility with extreme caution?\n",
                };

                // Help option 
                app.HelpOption(HelpOption);

                // Compress In-place Command and Options
                app.Command("compress-inplace", (command) =>
                {
                    command.Description = "Use temporary tables and copy data within the DBreeze database.";
                    command.ExtendedHelpText = "This process will recreate the table by coping all the records in the source table to a temp table " +
                        "then deleting the original table and renaming the temp table. You must have enough disk space on the drive containing " +
                        "the data directory to hold a complete copy of the raw data for the largest table.  Most of the time this will be the CoinView " +
                        "data (located in the ..\\<datadir>\\coin\\ directory). If you don't have enough disk space then you can use the " +
                        "compress-external command.\n\n" +

                        "***** WARNING *****: Once the process is running it is *** NOT *** recommended that you stop or interrupt the process. " +
                        "This utility was not designed to be fool proof (the universe keeps coming up with better fools so why bother). " +
                        "If you do have to interrupt it or there is a problem as long as the problem didn't occur during the table delete/rename " +
                        "processing phase then you have not lost any data.  You can use the inplace-cleanup command to delete any temp table " +
                        "that may have been left in the database.";

                    command.HelpOption(HelpOption);

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

                        return new CommandCompressInplace().Execute(dataDir);
                    });
                });

                // Compress Inplace Command and Options
                app.Command("compress-external", (command) =>
                {
                    command.Description = "Use external directory to hold the intermediate database while coping data.";
                    command.ExtendedHelpText = "This command is used when you do not have enough free disk space on the drive that holds the data directory. " +
                        "The process will create a new database in the temp location (tempdir) and copy all the records to that database.  Then it will " +
                        "the delete the original files from the data directory and copy the new files back into place.\n\n" +   
                        
                        "***** WARNING *****: Once the process is running it is *** NOT *** recommended that you stop or interrupt the process. " +
                        "This utility was not designed to be fool proof (the universe keeps coming up with better fools so why bother). " +
                        "If you do have to interrupt it or there is a problem as long as the problem didn't occur during the table delete/rename " +
                        "processing phase then you have not lost any data.  You can delete the temp location as nothing in the original database " +
                        "has been changed until the delete/copy phase.  If there is a problem during the delete/copy phase the data in the temp " +
                        "location is a complete copy of the original data.  You can correct the problem and copy the files back manually if needed.\n\n" +

                        "Note: The temp directory and the new database is not deleted by default for data safety.  You will need to delete the " +
                        "directory manually or specify the --delete-tempdir option on the command line.";

                    command.HelpOption(HelpOption);

                    var dataDirOption = command.Option("-datadir <datadir>", "Data Directory where the Stratis/DBreeze files are located.", CommandOptionType.SingleValue);
                    var tempDirOption = command.Option("-tempdir <tempdir>", "Directory to place the temp DBreeze files for intermediate database (will create the directory if needed).", CommandOptionType.SingleValue);
                    var deleteOption = command.Option("--delete-tempdir", "Delete the Temp Directory and all copies of the data after the processing has completed.", CommandOptionType.NoValue);

                    command.OnExecute(() =>
                    {
                        var dataDir = dataDirOption.Value();
                        if (dataDir == null || !Directory.Exists(dataDir))
                        {
                            command.Error.WriteLine("Missing or invalid data directory option.");
                            command.ShowHelp();
                            return 1;
                        }

                        var tempDir = tempDirOption.Value();
                        if (tempDir == null )
                        {
                            command.Error.WriteLine("Missing temp directory option.");
                            command.ShowHelp();
                            return 1;
                        }

                        return new CommandCompressExternal().Execute(dataDir, tempDir, deleteOption.HasValue());
                    });
                });

                // Compress Inplace Command and Options
                app.Command("inplace-cleanup", (command) =>
                {
                    command.Description = "Used to clean up an temp table left behind.  Used in case the compress-inplace command had an exception and did not finish correctly.";
                    command.HelpOption(HelpOption);

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

                        return new CommandCleanUpTempTable().Execute(dataDir);
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
                Console.WriteLine($"Unhandled Exception: {ex.Message}");
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
    }
}
