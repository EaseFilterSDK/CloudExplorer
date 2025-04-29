using System;
using System.ServiceProcess;
using System.Reflection;
using System.Configuration.Install;

using CloudFile.FilterControl;

namespace CloudFileDemo
{
    static class program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            try
            {
                string lastError = string.Empty;
                Utils.CopyOSPlatformDependentFiles(ref lastError);

                if (Environment.UserInteractive)
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

                                    break;
                                }

                            case "-uninstalldriver":
                                {
                                    FilterAPI.StopFilter();
                                    bool ret = FilterAPI.UnInstallDriver();

                                    if (!ret)
                                    {
                                        Console.WriteLine("UnInstall driver failed:" + FilterAPI.GetLastErrorMessage());
                                    }

                                    break;
                                }

                            case "-installservice":
                                {
                                    try
                                    {
                                        ManagedInstallerClass.InstallHelper(new string[] { Assembly.GetExecutingAssembly().Location });
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine("Install service failed:" + ex.Message);
                                    }

                                    break;
                                }

                            case "-uninstallservice":
                                {
                                    try
                                    {
                                        ManagedInstallerClass.InstallHelper(new string[] { "/u", Assembly.GetExecutingAssembly().Location });
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine("UnInstall service failed:" + ex.Message);
                                    }

                                    break;
                                }

                            case "-console":
                                {
                                    try
                                    {
                                        Console.WriteLine("Starting CloudFile File System Service...");
                                        FilterWorker.StartService();

                                        FilterWorker.StopService();
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine("Start CloudFile service failed:" + ex.Message);
                                    }

                                    break;
                                }

                            default: Console.WriteLine("The command " + command + " doesn't exist"); PrintUsage(); break;

                        }

                        return;
                    }
                    else
                    {
                        PrintUsage();
                    }

                }
                else
                {
                    Console.WriteLine("NOT Starting CloudFile console application...");
                    CloudService service = new CloudService();
                    ServiceBase.Run(service);
                }
            }
            catch (Exception ex)
            {
               Console.WriteLine("Cloud service failed with error " + ex.Message);
            }

        }

        static void PrintUsage()
        {
            Console.WriteLine("Usage: CloudFileCSDemo command");
            Console.WriteLine("Commands:");
            Console.WriteLine("-InstallDriver       --Install CloudFile filter driver.");
            Console.WriteLine("-UninstallDriver     --Uninstall CloudFile filter driver.");
            Console.WriteLine("-InstallService      --Install CloudFile Windows service.");
            Console.WriteLine("-UnInstallService    ---Uninstall CloudFile Windows service.");
            Console.WriteLine("-Console             ----start the file system service as console application.");          
        }

    }
}
