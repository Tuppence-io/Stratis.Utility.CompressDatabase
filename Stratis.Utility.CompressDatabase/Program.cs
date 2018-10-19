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
                    command.HelpOption(HelpOption);

                    var dataDirOption = command.Option("-datadir <datadir>", "Data Directory where the Stratis/DBreeze files are located.", CommandOptionType.SingleValue);
                    var tempDirOption = command.Option("-tempdir <tempdir>", "Directory to place the temp DBreeze files for intermediate database.", CommandOptionType.SingleValue);

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
                        if (tempDir == null || !Directory.Exists(tempDir))
                        {
                            command.Error.WriteLine("Missing or invalid temp directory option.");
                            command.ShowHelp();
                            return 1;
                        }

                        return new CommandCompressExternal().Execute(dataDir, tempDir);
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
    }
}
