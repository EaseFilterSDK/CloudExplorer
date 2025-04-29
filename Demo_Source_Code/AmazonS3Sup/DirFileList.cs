
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
using System.IO;
using System.Text;

using CloudFile.CommonObjects;

namespace CloudFile.AmazonS3Sup
{

    public struct FileEntry
    {
        public uint EntryLength;
        public uint Flags;        
        public uint FileAttributes;
        public long FileId;
        public long FileSize;
        public long CreationTime;
        public long LastAccessTime;
        public long LastWriteTime;
        public uint TagDataLength;
        public uint FileNameLength;
        public string FileName;
        public byte[] TagData;
    }

    public class DirectoryList :IDisposable
    {
        public const int MAX_PATH = 260;

        private Dictionary<string, FileEntry> dirFileList = null;
        private string directoryName = string.Empty;
        private string remoteDirName = string.Empty;
        private string dirFileListCacheFileName = string.Empty;
        private FileStream dirFileListCacheFileStream = null;
        private bool isDirFileListDownloaded = false;
        private long currentFileId = 0;


        /// <summary>
        /// if this is true, it will discard the cached diectory listing, and download it again.
        /// </summary>
        private bool forceDownload = false;

        private uint FILE_ENTRY_STRUCT_SIZE = 4/*entryLength*/ + 4/*flags*/ + 4/*FileAttributes*/ + 8/*Index*/+ 8/*FileSize*/ + 8/*creationTime*/
                                              + 8/*LastAccessTime*/ + 8/*LastWriteTime*/  + 4/*TagDataLength*/ + 4/*FileNameLength*/ ;

        public DirectoryList()
        {
        }

        public DirectoryList(string dirName, AmazonS3SiteInfo siteInfo, bool downloadNeeded)
        {
            this.dirFileList = new Dictionary<string, FileEntry>();

            this.directoryName = dirName;
            this.forceDownload = downloadNeeded;

            remoteDirName = CloudUtil.GetRemotePathByLocalPath(dirName, siteInfo);
            dirFileListCacheFileName = CloudUtil.GetDirFileListCacheName(directoryName);

            if (File.Exists(dirFileListCacheFileName))
            {
                if (downloadNeeded)
                {
                    //force to download the file list, delete the cache file list here.
                    try
                    {
                        File.Delete(dirFileListCacheFileName);
                    }
                    catch { }
                }
                else
                {
                    //load the file list from the cache file.
                    isDirFileListDownloaded = true;

                    return;
                }
            }

        }

        private void Dispose(Boolean freeManagedObjectsAlso)
        {
            if (freeManagedObjectsAlso)
            {
                dirFileList.Clear();
            }

            if (null != dirFileListCacheFileStream)
            {
                dirFileListCacheFileStream.Close();
            }
        }

        public void Dispose()
        {           
            Dispose(true);
        }

        ~DirectoryList()
        {
            Dispose(false);
        }

      
        public void AddFileEntry(string fileName,byte[] tagData, long creationTime, long lastAccessTime, long lastWriteTime, long fileSize, uint fileAttributes)
        {
            fileName = fileName.Replace("%20", " ");

            //check if the file entry already exist.
            if( dirFileList.ContainsKey(fileName.ToLower()))
            {
                dirFileList.Remove(fileName.ToLower());
            }

             FileEntry fileEntry = new FileEntry();

             if (null != tagData)
             {
                 fileEntry.TagDataLength = (uint)tagData.Length;
             }
             else
             {
                 fileEntry.TagDataLength = 0;
             }

             uint EntryLength = FILE_ENTRY_STRUCT_SIZE + (uint)fileName.Length * 2 + (uint)fileEntry.TagDataLength;

            fileEntry.EntryLength = EntryLength;
            fileEntry.Flags = 0;
            fileEntry.FileId = ++currentFileId;
            fileEntry.FileAttributes = fileAttributes;
            fileEntry.FileName = fileName;
            fileEntry.FileNameLength = (uint)fileName.Length * 2;
            fileEntry.FileSize = fileSize;
            fileEntry.CreationTime = creationTime;
            fileEntry.LastAccessTime = lastAccessTime;
            fileEntry.LastWriteTime = lastWriteTime;

          
            fileEntry.TagData = tagData;
            dirFileList.Add(fileName.ToLower(),fileEntry);

            WriteFileEntry(fileEntry);

            return;
        }

        private void WriteFileEntry(FileEntry fileEntry)
        {
            if(null == dirFileListCacheFileStream)
            {
                dirFileListCacheFileStream = new FileStream(dirFileListCacheFileName, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            }

            BinaryWriter bw = new BinaryWriter(dirFileListCacheFileStream);

            bw.Write(fileEntry.EntryLength);
            bw.Write(fileEntry.Flags);
            bw.Write(fileEntry.FileAttributes);
            bw.Write(fileEntry.FileId);
            bw.Write(fileEntry.FileSize);
            bw.Write(fileEntry.CreationTime);
            bw.Write(fileEntry.LastAccessTime);
            bw.Write(fileEntry.LastWriteTime);
            bw.Write(fileEntry.TagDataLength);
            bw.Write(fileEntry.FileNameLength);
            if (fileEntry.FileNameLength > 0)
            {
                byte[] fileNameArray = UnicodeEncoding.Unicode.GetBytes(fileEntry.FileName);
                bw.Write(fileNameArray);
            }
            if (fileEntry.TagDataLength > 0)
            {
                bw.Write(fileEntry.TagData);
            }

        }

        /// <summary>
        /// the local directory path
        /// </summary>
        public string DirectoryName
        {
            get { return directoryName; }
        }

        /// <summary>
        /// the remote cloud directory path
        /// </summary>
        public string RemoteDirName
        {
            get { return remoteDirName; }
        }

        /// <summary>
        /// the cache file name of the cloud folder directory file list 
        /// </summary>
        public string DirFileListCacheFileName
        {
            get { return dirFileListCacheFileName; }
        }

        /// <summary>
        /// the cloud folder directory file list cache file exists in the cache folder. 
        /// </summary>
        public bool IsDirFileListDownloaded
        {
            get { return isDirFileListDownloaded; }
        }

        /// <summary>
        /// The file list includes all sub diectories and files in current folder.
        /// </summary>
        public Dictionary<string, FileEntry> FolderFileList
        {
            get { return dirFileList; }
        }

        public List<FileEntry> FolderList
        {
            get 
            {
                List<FileEntry> folderList = new List<FileEntry>();
                foreach (KeyValuePair<string,FileEntry> entry in dirFileList)
                {
                    FileEntry fileEntry = entry.Value;
                    if ((fileEntry.FileAttributes & (uint)FileAttributes.Directory) ==(uint)FileAttributes.Directory)
                    {
                        folderList.Add(fileEntry);
                    }
                }
                return folderList; 
            }
        }

        public List<FileEntry> FileList
        {
            get
            {
                List<FileEntry> fileList = new List<FileEntry>();
                foreach (KeyValuePair<string, FileEntry> entry in dirFileList)
                {
                    FileEntry fileEntry = entry.Value;
                    if ((fileEntry.FileAttributes & (uint)FileAttributes.Directory) != (uint)FileAttributes.Directory)
                    {
                        fileList.Add(fileEntry);
                    }
                }
                return fileList;
            }
        }

        private int CompareFile(FileEntry x, FileEntry y)
        {
            return string.Compare(x.FileName, y.FileName, true);
        }     


    }

}
