using System;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Generic;

using CloudFile.CommonObjects;
using CloudFile.FilterControl;
using CloudFile.AmazonS3Sup;

namespace CloudFileS3Demo
{
    public class FilterWorker
    {
        public enum StartType
        {
            WindowsService = 0,
            GuiApp,
            ConsoleApp
        }

        static FilterControl filterControl = new FilterControl();

        static StartType startType = StartType.GuiApp;
        static FilterMessage filterMessage = null;
        static private Dictionary<string, AsyncTask> workingTasks = new Dictionary<string, AsyncTask>();

        public static bool StartService(StartType _startType, ListView listView_Message, out string lastError)
        {
            bool ret = true;
            lastError = string.Empty;

            startType = _startType;

            try
            {
                //Purchase a license key with the link: http://www.easefilter.com/Order.htm
                //Email us to request a trial key: info@easefilter.com //free email is not accepted.
                string licenseKey = GlobalConfig.LicenseKey;

                if (!filterControl.StartFilter((int)GlobalConfig.FilterConnectionThreads, GlobalConfig.ConnectionTimeOut, licenseKey, ref lastError))
                {
                    return false;
                }

                filterControl.ExcludeProcessIdList = GlobalConfig.ExcludePidList;

                foreach(AmazonS3SiteInfo s3SiteInfo in S3Config.GetAmazonS3SiteInfo().Values)
                {
                    //Add the local mapped cloud folder to the filter driver.
                    filterControl.CloudFolderList.Add(s3SiteInfo.LocalPath + "\\*");
                }

                if (!filterControl.SendConfigSettingsToFilter(ref lastError))
                {
                    return false;
                }

                filterControl.OnFilterRequest += OnFilterRequestHandler;

                System.Timers.Timer clearCachedFilesTimer = new System.Timers.Timer();
                clearCachedFilesTimer.Interval = GlobalConfig.DeleteCachedFilesAfterSeconds * 1000; //millisecond
                clearCachedFilesTimer.Start();
                clearCachedFilesTimer.Enabled = true;
                clearCachedFilesTimer.Elapsed += new System.Timers.ElapsedEventHandler(CloudUtil.DeleteExpiredCachedFiles);

                filterMessage = new FilterMessage(listView_Message, false);

                Console.WriteLine("Start cloud file service succeeded.");

            }
            catch (Exception ex)
            {
                lastError = "Start filter service failed with error " + ex.Message;
                EventManager.WriteMessage(104, "StartFilter", EventLevel.Error, lastError);
                ret = false;
            }

            return ret;
        }

        public static bool StopService()
        {
            GlobalConfig.Stop();
            filterControl.StopFilter();

            return true;
        }

        static void OnFilterRequestHandler(object sender, FilterRequestEventArgs e)
        {
            try
            {
                switch (e.MessageType)
                {
                    case FilterAPI.MessageType.MESSAGE_TYPE_GET_FILE_LIST:
                        {
                            //To get the directory file list in a cache file.
                            string cacheDirName = "";
                            if (GetDirFileList(e.FileName, ref cacheDirName))
                            {
                                e.ReturnCacheFileName = "\\??\\" + cacheDirName;
                                e.FilterStatus = FilterAPI.FilterStatus.CACHE_FILE_WAS_RETURNED;
                                e.ReturnStatus = (uint)FilterAPI.NTSTATUS.STATUS_SUCCESS;
                            }
                            else
                            {
                                EventManager.WriteMessage(260, "DirFileList", EventLevel.Error, "Get directory " + e.FileName + " file list failed.");
                                e.ReturnStatus = FilterAPI.NTSTATUS.STATUS_UNSUCCESSFUL;
                            }

                            break;
                        }
                    case FilterAPI.MessageType.MESSAGE_TYPE_RESTORE_FILE_TO_CACHE:
                    case FilterAPI.MessageType.MESSAGE_TYPE_RESTORE_BLOCK_OR_FILE:
                        {
                            //for the write request, the filter driver needs to restore the whole file first,
                            //here we need to download the whole cache file and return the cache file name to the filter driver,
                            //the filter driver will replace the stub file data with the cache file data.

                            //for memory mapped file open( for example open file with notepad in local computer )
                            //it also needs to download the whole cache file and return the cache file name to the filter driver,
                            //the filter driver will read the cache file data, but it won't restore the stub file.

                            DownloadAmazonS3File(e);

                            break;
                        }
                    case FilterAPI.MessageType.MESSAGE_TYPE_RESTORE_FILE_TO_ORIGINAL_FOLDER:
                        {
                            DownloadAmazonS3File(e);

                            if (File.Exists(e.ReturnCacheFileName))
                            {
                                File.Copy(e.ReturnCacheFileName, e.FileName);

                                Console.WriteLine("Download file to " + e.FileName);
                            }

                            break;
                        }
                    case FilterAPI.MessageType.MESSAGE_TYPE_DELETE_FILE:
                        {
                            DeleteAmazonS3File(e);

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

                                //download the file to the cache folder, delete the file from S3.
                                DownloadAmazonS3File(e);

                                if (File.Exists(e.ReturnCacheFileName))
                                {
                                    File.Copy(e.ReturnCacheFileName, newFileName);

                                    DeleteAmazonS3File(e);
                                }

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

                filterMessage.DisplayMessage(e);

                string message = "MessageId:" + e.MessageId + ",UserName:" + e.UserName + ",ProcessName:" + e.ProcessName + "\r\n";
                message += "MessageType:" + e.MessageType.ToString() + "\n";
                message += "FileName:" + e.FileName + "\n";
                message += "ReturnStatus:" + ((FilterAPI.NTSTATUS)(e.ReturnStatus)).ToString() + ",FilterStatus:" + e.FilterStatus + "\r\n"
                          + "ReturnCacheFileName:" + e.ReturnCacheFileName + "\r\n";

                Console.WriteLine(message);

                EventManager.WriteMessage(390, "OnFileRequestHandler", EventLevel.Verbose, message);

            }
            catch (Exception ex)
            {
                string message = "MessageId:" + e.MessageId + ",UserName:" + e.UserName + ",ProcessName:" + e.ProcessName + "\r\n";
                message += "MessageType:" + e.MessageType.ToString() + "\n";
                message += "ReturnStatus:" + ((FilterAPI.NTSTATUS)(e.ReturnStatus)).ToString() + ",FilterStatus:" + e.FilterStatus + "\r\n"
                          + "ReturnCacheFileName:" + e.ReturnCacheFileName + ",exception:" + ex.Message;

                Console.WriteLine(message);

                EventManager.WriteMessage(390, "OnFileRequestHandler", EventLevel.Verbose, message );
            }
        }

        public static bool DownloadAmazonS3File(FilterRequestEventArgs e)
        {
            bool ret = false;
            bool isCacheFileDownloaded = false;

            try
            {
                //this is the test tag data from the cloud file, you can custom your own tag data for the cloud file.
                //"sitename:s3 site name;remotepath"
                string tagDataString = Encoding.Unicode.GetString(e.TagData);
                tagDataString = tagDataString.Substring(0, e.TagDataLength / 2);
                
                AmazonS3SiteInfo siteInfo = CloudUtil.GetSiteInfoByLocalPath(e.FileName);
                string remotePath = CloudUtil.GetRemotePathByLocalPath(e.FileName,siteInfo);
                string returnCacheFileName = CloudUtil.GetCacheFileName(e.FileName);

                if (null == siteInfo)
                {
                    EventManager.WriteMessage(90, "Download S3 File", EventLevel.Error, "Download file " + e.FileName + " from S3 failed,can't get the site name from configuration.");
                    e.ReturnStatus = FilterAPI.NTSTATUS.STATUS_UNSUCCESSFUL;

                    return false;
                }

                if (File.Exists(returnCacheFileName))
                {
                    isCacheFileDownloaded = true;
                }
                else
                {

                    AsyncTask asyncTask = null;

                    lock (workingTasks)
                    {
                        if (workingTasks.ContainsKey(remotePath.ToLower()))
                        {
                            asyncTask = workingTasks[remotePath.ToLower()];
                            asyncTask.CompletedEvent.WaitOne();
                        }
                    }

                    if (null == asyncTask)
                    {

                        asyncTask = new AsyncTask(TaskType.DownloadFile, siteInfo, returnCacheFileName, remotePath,
                            e.FileSize, DateTime.FromFileTime(e.CreationTime), (FileAttributes)e.Attributes
                            , 0, "", "");

                        lock (workingTasks)
                        {
                            if (!workingTasks.ContainsKey(remotePath.ToLower()))
                            {
                                workingTasks[remotePath.ToLower()] = asyncTask;
                            }
                        }

                        AmazonS3 s3 = new AmazonS3(siteInfo, null, asyncTask);
                        Task downloadTask = s3.DownloadAsync();
                        downloadTask.Wait();
                        asyncTask.CompleteTask("");

                        lock (workingTasks)
                        {
                            if (workingTasks.ContainsKey(remotePath.ToLower()))
                            {
                                workingTasks.Remove(remotePath.ToLower());
                            }
                        }
                    }

                    if (asyncTask.IsTaskCompleted && asyncTask.State == TaskState.completed && File.Exists(returnCacheFileName))
                    {
                        isCacheFileDownloaded = true;
                    }
                    else
                    {
                        //the file didn't download successfully
                        File.Delete(returnCacheFileName);
                    }
                }

                if (isCacheFileDownloaded)
                {
                    e.ReturnCacheFileName = "\\??\\" + returnCacheFileName;
                    e.FilterStatus = FilterAPI.FilterStatus.CACHE_FILE_WAS_RETURNED;
                    e.ReturnStatus = (uint)FilterAPI.NTSTATUS.STATUS_SUCCESS;

                    ret = true;
                }
                else
                {
                    EventManager.WriteMessage(130, "Download S3 File", EventLevel.Error, "Download file " + e.FileName + " from S3 failed. Cache file:" + returnCacheFileName + " doesn't exist.");
                    e.ReturnStatus = FilterAPI.NTSTATUS.STATUS_UNSUCCESSFUL;
                }

            }
            catch (Exception ex)
            {
                EventManager.WriteMessage(190, "Download S3 File", EventLevel.Error, "Download file " + e.FileName + " from S3 got exception:" + ex.Message);
                e.ReturnStatus = FilterAPI.NTSTATUS.STATUS_UNSUCCESSFUL;
            }

            return ret;

        }

        public static bool DeleteAmazonS3File(FilterRequestEventArgs e)
        {
            bool ret = false;

            try
            {
                //this is the tag data from the cloud file.
                string tagDataString = Encoding.Unicode.GetString(e.TagData);
                tagDataString = tagDataString.Substring(0, e.TagDataLength / 2);

                string siteName = tagDataString.Substring(0, tagDataString.IndexOf(";"));
                string remotePath = tagDataString.Substring(siteName.Length + 1);

                siteName = siteName.Replace("sitename:", "");

                AmazonS3SiteInfo siteInfo = CloudUtil.GetSiteInfoBySiteName(siteName);

                if (null == siteInfo)
                {
                    EventManager.WriteMessage(90, "DeleteAmazonS3File", EventLevel.Error, "DeleteAmazonS3File " + e.FileName + " from S3 failed, the site name:"
                        + siteName + " can't be found from the configuration setting.");
                    e.ReturnStatus = FilterAPI.NTSTATUS.STATUS_UNSUCCESSFUL;

                    return false;
                }

                AsyncTask asyncTask = new AsyncTask(TaskType.DeleteFile, siteInfo, remotePath, remotePath,
                       0, DateTime.Now, FileAttributes.Normal, 0, Environment.UserName, "CloudFileS3Service");

                AmazonS3 s3 = new AmazonS3(siteInfo, null, asyncTask);
                Task downloadTask = s3.DeleteFileAsync();
                downloadTask.Wait();
                asyncTask.CompleteTask("");

                //delete the directory file list cache file in cache folder.
                string dirFileListCacheFileName = CloudUtil.GetDirFileListCacheName(e.FileName);

            }
            catch (Exception ex)
            {
                EventManager.WriteMessage(190, "Delete S3 File", EventLevel.Error, "Delete file " + e.FileName + " from S3 got exception:" + ex.Message);
                e.ReturnStatus = FilterAPI.NTSTATUS.STATUS_UNSUCCESSFUL;
            }

            return ret;

        }

        static bool GetDirFileList(string localDirectoryName, ref string dirFileListCacheFilePath)
        {
            try
            {
                AmazonS3SiteInfo currentSiteInfo = CloudUtil.GetSiteInfoByLocalPath(localDirectoryName);

                if(null == currentSiteInfo)
                {
                    Console.WriteLine("GetDirFileList dirName " + localDirectoryName + " failed, can't get the site info from s3 config settings.");
                    return false;
                }

                AmazonS3 s3 = new AmazonS3(currentSiteInfo, null);

                Task<DirectoryList> downloadFileListTask = s3.DownloadFileListAsync(localDirectoryName, false);

                DirectoryList directoryList = downloadFileListTask.Result;
                dirFileListCacheFilePath = directoryList.DirFileListCacheFileName;

                directoryList.Dispose();

                if (File.Exists(dirFileListCacheFilePath))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch( Exception ex)
            {
                Console.WriteLine("GetDirFileList dirName " + localDirectoryName + "  failed with error:" + ex.Message);
            }

            return false;
        }
       
    }
}
