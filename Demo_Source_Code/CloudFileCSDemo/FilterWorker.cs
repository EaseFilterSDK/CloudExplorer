using System;
using System.IO;
using System.Text;
using System.Reflection;

using CloudFile.FilterControl;

namespace CloudFileDemo
{
    public class FilterWorker
    {
        static bool returnCacheFile = true;

        static Assembly assembly = System.Reflection.Assembly.GetEntryAssembly();
        static string AssemblyPath = Path.GetDirectoryName(assembly.Location);

        /// <summary>
        /// This is the cache folder to store the directory list cache files.
        /// </summary>
        public static string CacheFolder = AssemblyPath + "\\cacheFolder";
        /// <summary>
        /// this is the folder to store all the physical files for the virtual folder test.
        /// </summary>
        public static string SourceFolder = AssemblyPath + "\\TestSourceFolder";
        /// <summary>
        /// This is the test cloud folder which will simulate the files from the cloud.
        /// </summary>
        public static string CloudFolder = AssemblyPath + "\\TestCloudFolder";
        /// <summary>
        /// the cache directory file list name of the cloud folder.
        /// </summary>
        public static string DirFileListName = "dirFileList.dat";

        public static bool StartService()
        {
                      
            try
            {
                CreateTestFiles();

                //Purchase a license key with the link: http://www.easefilter.com/Order.html
                //Email us to request a trial key: info@easefilter.com //free email is not accepted.
                string licenseKey = CloudFile.CommonObjects.GlobalConfig.LicenseKey;
                
                //number of the threads to handle the file requests.
                int connectionThreads = 5;
                //the connection timeout in seconds.
                int connectionTimeout = 30;

                FilterControl filterControl = new FilterControl();
                
                //you can setup multiple cloud folder filter mask here.
                filterControl.CloudFolderList.Add(CloudFolder + "\\*");

                string lastError = string.Empty;
                if (!filterControl.StartFilter(connectionThreads, connectionTimeout, licenseKey, ref lastError))
                {
                    Console.WriteLine("\r\nStartFilter failed with error:" + lastError);
                    return false;
                }

                //exclude the current process Id.
                uint currentPid = (uint)System.Diagnostics.Process.GetCurrentProcess().Id;
                filterControl.ExcludeProcessIdList.Add(currentPid);

                if (!filterControl.SendConfigSettingsToFilter(ref lastError))
                {
                    Console.WriteLine("\r\nSendConfigSettingsToFilter failed with error:" + lastError);
                    return false;
                }

                filterControl.OnFilterRequest += OnFilterRequestHandler;

                Console.WriteLine("\r\nCloud service started, you can test the cloud files here:\n" + CloudFolder + "\r\n");

                Console.ReadKey();

                return true;
            }
            catch (Exception ex)
            {
                string lastError = "Start the driver service failed with error:" + ex.Message;
                Console.WriteLine(lastError);
                return false;
            }
            finally
            {
              CloudFile.CommonObjects.GlobalConfig.Stop();
              StopService();
            }

        }

        public static bool StopService()
        {
            FilterAPI.StopFilter();
            return true;
        }


        static void OnFilterRequestHandler(object sender, FilterRequestEventArgs e)
        {

            try
            {
                string cacheFileName = string.Empty;

                if (e.TagDataLength > 0)
                {
                    //in our test, the cloud file uses the test source file name path as the tag data, you can have your own custom tag data in the cloud file list.
                    cacheFileName = Encoding.Unicode.GetString(e.TagData);
                    //remove the extra data of the file name.
                    cacheFileName = cacheFileName.Substring(0, e.TagDataLength / 2);
                }

                switch (e.MessageType)
                {
                    case FilterAPI.MessageType.MESSAGE_TYPE_GET_FILE_LIST:
                        {
                            //To get the directory file list in a cache file.
                            string cacheDirName = "";
                            if (DirectoryList.GetFileList(e.FileName, ref cacheDirName))
                            {
                                e.ReturnCacheFileName = "\\??\\" + cacheDirName;
                                e.FilterStatus = FilterAPI.FilterStatus.CACHE_FILE_WAS_RETURNED;
                                e.ReturnStatus = (uint)FilterAPI.NTSTATUS.STATUS_SUCCESS;
                            }
                            else
                            {
                                Console.WriteLine("Directory " + e.FileName + " messageType:" + e.MessageType + " ,can't get the directory file list.");
                                e.ReturnStatus = FilterAPI.NTSTATUS.STATUS_UNSUCCESSFUL;
                            }

                            break;
                        }
                    case FilterAPI.MessageType.MESSAGE_TYPE_RESTORE_FILE_TO_CACHE:
                        {
                            //for the write request, the filter driver needs to restore the whole file first,
                            //here we need to download the whole cache file and return the cache file name to the filter driver,
                            //the filter driver will replace the stub file data with the cache file data.

                            //for memory mapped file open( for example open file with notepad in local computer )
                            //it also needs to download the whole cache file and return the cache file name to the filter driver,
                            //the filter driver will read the cache file data, but it won't restore the stub file.

                            e.ReturnCacheFileName = cacheFileName;

                            //if you want to restore the file to the orignal folder, you can download it the oringal folder now.

                            e.FilterStatus = FilterAPI.FilterStatus.CACHE_FILE_WAS_RETURNED;
                            e.ReturnStatus = (uint)FilterAPI.NTSTATUS.STATUS_SUCCESS;
                            break;
                        }
                    case FilterAPI.MessageType.MESSAGE_TYPE_RESTORE_BLOCK_OR_FILE:
                        {

                            e.ReturnCacheFileName = cacheFileName;

                            //for this request, the user is trying to read block of data, you can either return the whole cache file

                            //if you want to return the whole cache file, then do as below
                            if (returnCacheFile)
                            {
                                e.FilterStatus = FilterAPI.FilterStatus.CACHE_FILE_WAS_RETURNED;
                            }
                            else
                            {
                                //we return the block the data back to the filter driver.
                                FileStream fs = new FileStream(cacheFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                                fs.Position = e.ReadOffset;

                                int returnReadLength = fs.Read(e.ReturnBuffer, 0, (int)e.ReadLength);
                                e.ReturnBufferLength = (uint)returnReadLength;

                                e.FilterStatus = FilterAPI.FilterStatus.BLOCK_DATA_WAS_RETURNED;

                                fs.Close();

                            }

                            e.ReturnStatus = FilterAPI.NTSTATUS.STATUS_SUCCESS;
                            break;
                        }
                    case FilterAPI.MessageType.MESSAGE_TYPE_RESTORE_FILE_TO_ORIGINAL_FOLDER:
                        {
                            //download the file to the original folder, for the write request, it will require restore the file first.
                            string sourceFileName = Path.Combine(SourceFolder, e.FileName.Substring(CloudFolder.Length + 1));
                            File.Copy(sourceFileName, e.FileName);

                            Console.WriteLine("Download file to " + e.FileName);

                            break;
                        }
                    case FilterAPI.MessageType.MESSAGE_TYPE_DELETE_FILE:
                        {                            
                            string cacheDirName = "";
                            if (DirectoryList.GetFileList(Path.GetDirectoryName(e.FileName), ref cacheDirName))
                            {
                                //the cache file of the directory file list needs to be deleted.
                                Console.WriteLine("Cloud file " + e.FileName + " is going to be deleted. delete the cache directory file list file:" + cacheDirName);
                                
                                File.Delete(cacheDirName);
                            }

                            break;
                        }
                    case FilterAPI.MessageType.MESSAGE_TYPE_RENAME_FILE:
                        {
                            string newFileName = "";
                            if (e.TagDataLength > 0)
                            {
                                byte[] buffer = new byte[e.TagDataLength];
                                Array.Copy(e.TagData, buffer, buffer.Length);
                                newFileName = Encoding.Unicode.GetString(buffer);
                                if (newFileName.StartsWith("\\??\\"))
                                {
                                    newFileName = newFileName.Substring(4);
                                }

                                if (newFileName.IndexOf((char)0) > 0)
                                {
                                    newFileName = newFileName.Remove(newFileName.IndexOf((char)0));
                                }

                                //download the file to the new file name, delete the old file here.
                                string sourceFileName = Path.Combine(SourceFolder, e.FileName.Substring(CloudFolder.Length + 1));

                                //it needs to delete the old file, then create the new file.
                                File.Move(sourceFileName, newFileName);

                                Console.WriteLine("Rename file " + e.FileName + " to new file: " + newFileName);
                            }

                            break;
                        }
                    case FilterAPI.MessageType.MESSAGE_TYPE_SEND_EVENT_NOTIFICATION:
                        {
                            FilterAPI.EVENTTYPE eventType = (FilterAPI.EVENTTYPE)e.InfoClass;
                            Console.WriteLine("File " + e.FileName + " file event " + eventType.ToString() + " was triggered.");

                            break;
                        }
                    default:
                        {
                            Console.WriteLine("File " + e.FileName + " messageType:" + e.MessageType + " unknow.");
                            e.ReturnStatus = FilterAPI.NTSTATUS.STATUS_UNSUCCESSFUL;

                            break;

                        }
                }

                string message = "MessageId:" + e.MessageId + ",UserName:" + e.UserName + ",ProcessName:" + e.ProcessName + "\r\n";
                message += "MessageType:" + e.MessageType.ToString() + "\n";
                message += "FileName:" + e.FileName + "\n";
                message += "ReturnStatus:" + ((FilterAPI.NTSTATUS)(e.ReturnStatus)).ToString() + ",FilterStatus:" + e.FilterStatus + "\r\n"
                          + "ReturnCacheFileName:" + e.ReturnCacheFileName + "\r\n";

                Console.WriteLine(message);

            }
            catch (Exception ex)
            {
                string message = "MessageId:" + e.MessageId + ",UserName:" + e.UserName + ",ProcessName:" + e.ProcessName + "\r\n";
                message += "MessageType:" + e.MessageType.ToString() + "\n";
                message += "ReturnStatus:" + ((FilterAPI.NTSTATUS)(e.ReturnStatus)).ToString() + ",FilterStatus:" + e.FilterStatus + "\r\n"
                          + "ReturnCacheFileName:" + e.ReturnCacheFileName + "\r\n";

                Console.WriteLine(message);

                Console.WriteLine("Process request exception:" + ex.Message);
            }

        }

        public static bool CreateTestFiles()
        {
            bool retVal = true;

            try
            {
                if (!Directory.Exists(CloudFolder))
                {
                    Directory.CreateDirectory(CloudFolder);
                }

                if (!Directory.Exists(CacheFolder))
                {
                    Directory.CreateDirectory(CacheFolder);
                }

                if (!Directory.Exists(SourceFolder))
                {
                    Directory.CreateDirectory(SourceFolder);
                }

                //create the test file here.                    
                for (int i = 1; i < 11; i++)
                {
                    string testStr = string.Empty;
                    byte[] buffer = new byte[i * 2048];
                    for (int j = 0; j < buffer.Length; j++)
                    {
                        int rem = 0;
                        Math.DivRem(j, 26, out rem);
                        testStr += (char)('a' + rem);
                        if (rem == 0)
                        {
                            testStr += Environment.NewLine;
                        }
                    }

                    string testFileName = Path.Combine(SourceFolder, "cloudFile." + i.ToString() + ".txt");
                    File.AppendAllText(testFileName, testStr);
                }


            }
            catch (Exception ex)
            {
                retVal = false;
                Console.WriteLine("Clear cache file failed with error " + ex.Message);
            }

            return retVal;
        }


    }

}
