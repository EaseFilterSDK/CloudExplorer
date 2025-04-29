
///////////////////////////////////////////////////////////////////////////////
//
//    (C) Copyright 2011 EaseFilter Technologies
//    All Rights Reserved
//
//    This software is part of a licensed software product and may
//    only be used or copied in accordance with the terms of that license.
//
//    NOTE:  THIS MODULE IS UNSUPPORTED SAMPLE CODE
//
//    This module contains sample code provided for convenience and
//    demonstration purposes only,this software is provided on an 
//    "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, 
//     either express or implied.  
//
///////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

using CloudFile.CommonObjects;

namespace CloudFile.AmazonS3Sup
{
    static public class CloudUtil
    {

        public static bool IsMappedFolder(string folderName)
        {
            foreach (AmazonS3SiteInfo siteInfo in S3Config.AmazonS3SiteInfos.Values)
            {
                if (folderName.ToLower().StartsWith(siteInfo.LocalPath.ToLower()))
                {
                    return true;
                }
            }

            return false;
        }

        public static AmazonS3SiteInfo GetSiteInfoByLocalPath(string localFileName)
        {

            foreach (AmazonS3SiteInfo siteInfo in S3Config.AmazonS3SiteInfos.Values)
            {
                if (localFileName.ToLower().StartsWith(siteInfo.LocalPath.ToLower()))
                {
                    return siteInfo;
                }
            }

            return null;
        }

        public static AmazonS3SiteInfo GetSiteInfoBySiteName(string siteName)
        {

            foreach (AmazonS3SiteInfo siteInfo in S3Config.AmazonS3SiteInfos.Values)
            {
                if (string.Compare(siteInfo.SiteName,siteName) == 0)
                {
                    return siteInfo;
                }
            }

            return null;
        }


        public static bool IsLocalPathAlreadyMapped(string siteName,string localFolder)
        {
            string lowerLocalPath = localFolder.ToLower();

            foreach (AmazonS3SiteInfo siteInfo in S3Config.AmazonS3SiteInfos.Values)
            {
                string siteInfolocalPath = siteInfo.LocalPath.ToLower();

                if (string.Compare(siteInfo.SiteName, siteName) == 0)
                {
                    continue;
                }

                if (lowerLocalPath.StartsWith(siteInfolocalPath))
                {
                    string restFolder = localFolder.Substring(siteInfo.LocalPath.Length);
                    if (restFolder.Length == 0 || restFolder.StartsWith("\\"))
                    {
                        return false;
                    }
                }

                if (  siteInfolocalPath.StartsWith(lowerLocalPath))
                {
                    string restFolder = siteInfolocalPath.Substring(localFolder.Length);

                    if (restFolder.Length == 0 || restFolder.StartsWith("\\"))
                    {
                        return false;
                    }
                }
            }

            return true;
        }     

        public static void GetMappingInfo(AmazonS3SiteInfo siteInfo, string fileName, ref string localCacheFileName, ref string remoteFileName)
        {
            string cacheFolder = GlobalConfig.CacheFolder;
            string mappingFolder = siteInfo.LocalPath;

            string cacheFileName = Path.Combine(cacheFolder, siteInfo.SiteName);
            
            remoteFileName = GetRemotePathByLocalPath(fileName, siteInfo);

            string remoteName = siteInfo.RemotePath;

            localCacheFileName = cacheFileName;

            if (remoteName.Length > 0)
            {
                //the site info has the remote path setting
                if (remoteFileName.Length > remoteName.Length)
                {
                    localCacheFileName = Path.Combine(cacheFileName, remoteFileName.Substring(remoteName.Length + 1));
                }
            }
            else
            {
                localCacheFileName = Path.Combine(cacheFileName, remoteFileName);
            }

            localCacheFileName = localCacheFileName.Replace("/", "\\");

            string directory = Path.GetDirectoryName(localCacheFileName);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

        }

        public static bool TestRemotePath(AmazonS3SiteInfo siteInfo,string remotePath,out string lastError)
        {
            lastError = string.Empty;
            bool ret = false;

            try
            {
                AmazonS3 s3 = new AmazonS3(siteInfo);
                return s3.TestConnection(ref lastError);
            }
            catch (Exception ex)
            {
                lastError = "TestConnection failed:" + ex.Message;
            }

            return ret;

        }

        public static string GetRemotePathByLocalPath(string localPath, AmazonS3SiteInfo siteInfo)
        {
            string remotePath = localPath;

            if (remotePath.StartsWith(siteInfo.LocalPath, true, System.Globalization.CultureInfo.CurrentCulture))
            {
                remotePath = remotePath.Substring(siteInfo.LocalPath.Length);
            }
            else
            {
                remotePath = localPath;
            }

            if (remotePath.StartsWith("\\") || remotePath.StartsWith("/"))
            {
                remotePath = remotePath.Substring(1);
            }

            remotePath = Path.Combine(siteInfo.RemotePath, remotePath);
            remotePath = remotePath.Replace("\\", "/");

            return remotePath;
        }

        public static string GetDirFileListCacheName(string localDirectoryName)
        {
            AmazonS3SiteInfo siteInfo = GetSiteInfoByLocalPath(localDirectoryName);

            if( null == siteInfo )
            {
                EventManager.WriteMessage(173, "GetCacheFileNameByFolderName", EventLevel.Error, "Can't get site info by folder name :" + localDirectoryName);
                return string.Empty;
            }

            string cacheFolder = GlobalConfig.CacheFolder;
            string mappingFolder = siteInfo.LocalPath.ToLower();

            string cacheDirName = Path.Combine(cacheFolder, siteInfo.SiteName);

            if (mappingFolder.Length > 0)
            {
                if (!localDirectoryName.ToLower().StartsWith(mappingFolder, StringComparison.CurrentCultureIgnoreCase))
                {
                    EventManager.WriteMessage(185, "GetCacheFileNameByFolderName", EventLevel.Error, "Folder name " + localDirectoryName + " doesn't match the mapping folder " + mappingFolder);
                    return string.Empty;
                }
                else
                {

                    string folder = localDirectoryName.Substring(mappingFolder.Length);
                    if (folder.StartsWith("\\"))
                    {
                        folder = folder.Substring(1);
                    }

                    cacheDirName = Path.Combine(cacheDirName, folder);
                }
            }
          

            if (!Directory.Exists(cacheDirName))
            {
                Directory.CreateDirectory(cacheDirName);
            }

            cacheDirName = Path.Combine(cacheDirName, GlobalConfig.DirFileListName);

            return cacheDirName;
        }

        public static string GetCacheFileName(string localFileName)
        {
            AmazonS3SiteInfo siteInfo = GetSiteInfoByLocalPath(localFileName);

            if (null == siteInfo)
            {
                EventManager.WriteMessage(240, "GetCacheFileName", EventLevel.Error, "Can't get site info by file name :" + localFileName);
                return string.Empty;
            }

            string cacheFolder = GlobalConfig.CacheFolder;
            string mappingFolder = siteInfo.LocalPath.ToLower();

            string cacheFileName = Path.Combine(cacheFolder, siteInfo.SiteName);

            if (mappingFolder.Length > 0)
            {
                if (!localFileName.ToLower().StartsWith(mappingFolder, StringComparison.CurrentCultureIgnoreCase))
                {
                    EventManager.WriteMessage(185, "GetCacheFileName", EventLevel.Error, "Folder name " + localFileName + " doesn't match the mapping folder " + mappingFolder);
                    return string.Empty;
                }
                else
                {

                    string remotPath = localFileName.Substring(mappingFolder.Length);
                    if (remotPath.StartsWith("\\"))
                    {
                        remotPath = remotPath.Substring(1);
                    }

                    cacheFileName = Path.Combine(cacheFileName, remotPath);
                }
            }


            if (!Directory.Exists(Path.GetDirectoryName(cacheFileName)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(cacheFileName));
            }

            return cacheFileName;
        }

        private static void DeleteExpiredCachedFiles(string folder)
        {
            try
            {
                string[] subDirs = Directory.GetDirectories(folder);

                foreach (string dir in subDirs)
                {
                    DeleteExpiredCachedFiles(dir);
                }

                string[] files = Directory.GetFiles(folder);

                int expireCachedDirListingSeconds = GlobalConfig.ExpireCachedDirectoryListingAfterSeconds;
                int deleteCachedFileSeconds = GlobalConfig.DeleteCachedFilesAfterSeconds;
                string dirListingName = GlobalConfig.DirFileListName;

                bool deleteFolderNeeded = true;

                foreach (string file in files)
                {
                    try
                    {
                        FileInfo fileInfo = new FileInfo(file);
                        TimeSpan timeSpan = DateTime.Now - fileInfo.LastWriteTime;

                        if (string.Compare(dirListingName, Path.GetFileName(file)) == 0)
                        {
                            //this is the directory listing file
                            if (timeSpan.TotalSeconds > expireCachedDirListingSeconds)
                            {
                                File.Delete(file);
                            }
                            else
                            {
                                deleteFolderNeeded = false;
                            }
                        }
                        else
                        {
                            if (timeSpan.TotalSeconds > deleteCachedFileSeconds)
                            {
                                File.Delete(file);
                            }
                            else
                            {
                                deleteFolderNeeded = false;
                            }
                        }


                    }
                    catch (Exception ex)
                    {
                        EventManager.WriteMessage(243, "DeleteExpiredCachedFiles", EventLevel.Verbose, "Delete file " + file + " in folder " + folder + " failed with error " + ex.Message);
                    }
                }

                if (deleteFolderNeeded && subDirs.Length == 0)
                {
                    Directory.Delete(folder);
                }

            }
            catch (Exception ex)
            {
                EventManager.WriteMessage(255, "DeleteExpiredCachedFiles", EventLevel.Verbose, "DeleteExpiredCachedFiles in folder " + folder + " failed with error " + ex.Message);
            }
        }

        public static void DeleteExpiredCachedFiles(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                string cacheFolder = GlobalConfig.CacheFolder;
                DeleteExpiredCachedFiles(cacheFolder);

            }
            catch (Exception ex)
            {
                EventManager.WriteMessage(136, "ClearCacheFiles", EventLevel.Error, "Clear cache file failed with error " + ex.Message);
            }
        }


    }
}
