using System;
using System.Windows.Forms;

using CloudFile.CommonObjects;
using CloudFile.FilterControl;

namespace CloudFileS3Demo
{
    static class Program
    {

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {

            if (args.Length > 0)
            {
                string command = args[0];
                switch (command.ToLower())
                {
                    case "-installdriver":
                        {
                            bool ret = FilterAPI.InstallDriver();
                            if (!ret)
                            {
                                Console.WriteLine("Install driver failed:" + FilterAPI.GetLastErrorMessage());
                            }
                            else
                            {
                                Console.WriteLine("Install CloudFile driver succeeded.");
                            }

                            break;
                        }

                    case "-uninstalldriver":
                        {
                            bool ret = FilterAPI.UnInstallDriver();
                            if (!ret)
                            {
                                Console.WriteLine("UnInstall driver failed:" + FilterAPI.GetLastErrorMessage());
                            }
                            else
                            {
                                Console.WriteLine("UnInstall driver succeded.");
                            }

                            break;
                        }

                    case "-console":
                        {
                            try
                            {
                                Console.WriteLine("Starting CloudFile console app, you can open the cloud file in the cloud folders.");

                                string lastError = string.Empty;

                                if (!FilterWorker.StartService(FilterWorker.StartType.ConsoleApp, null, out lastError))
                                {
                                    Console.WriteLine("\n\nStart service failed." + lastError);
                                    return;
                                }

                                Console.WriteLine("\n\nPress any key to stop program");
                                Console.Read();

                                FilterWorker.StopService();
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Start CloudFile service failed:" + ex.Message);
                            }

                            break;
                        }

                    case "-help":
                        {
                            PrintUsage();
                            break;
                        }
                    default:

                        Console.WriteLine("The command " + command + " doesn't support.");
                        PrintUsage();

                        break;
                }
            }
            else
            {

                EventManager.Output = EventOutputType.File;
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new CloudFileS3Form());
            }
        }

        static void PrintUsage()
        {
            Console.WriteLine("Usage: CloudFileDemo command");
            Console.WriteLine("Commands:");
            Console.WriteLine("                     --start the Windows forms application.");
            Console.WriteLine("-InstallDriver       --Install EaseFilter filter driver.");
            Console.WriteLine("-UninstallDriver     --Uninstall EaseFilter filter driver.");
            Console.WriteLine("-InstallService      --Install EaseFilter Windows service.");
            Console.WriteLine("-UnInstallService    --Uninstall EaseFilter Windows service.");
            Console.WriteLine("-Console             ---start the console application.");
        }
    }
}
