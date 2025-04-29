using System;
using System.Text;
using System.IO;

namespace CloudFileDemo
{
    /// <summary>
    /// this is the struture of a file in the directory list.
    /// </summary>
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

    public class DirectoryList
    {
      
        private static string GetFolderListCacheFileName(string directoryName)
        {
            try
            {
                string cacheFolder = FilterWorker.CacheFolder;

                string cacheDirName = directoryName.Replace(":\\", "\\");
                cacheDirName = Path.Combine(cacheFolder, cacheDirName);

                if (!Directory.Exists(cacheDirName))
                {
                    Directory.CreateDirectory(cacheDirName);
                }

                cacheDirName = Path.Combine(cacheDirName, FilterWorker.DirFileListName);

                return cacheDirName;
            }
            catch (Exception ex)
            {
               Console.WriteLine("Get cloud folder:" + directoryName + " file name exception:" + ex.Message);
                return "";
            }
        }

        private static void WriteFileEntry(FileStream fs, FileEntry fileEntry)
        {
            BinaryWriter bw = new BinaryWriter(fs);

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


        public static  bool GetFileList(string directoryName, ref string cacheDirName )
        {
            FileStream fs = null;

            try
            {
                //for the test, we only get the file list from the source folder.
                //you can the file list from your database or remote site.
                string sourceDirectoryName = FilterWorker.SourceFolder;

                if (directoryName.ToLower().StartsWith(FilterWorker.CloudFolder.ToLower()))
                {
                    if (directoryName.Length > FilterWorker.CloudFolder.Length)
                    {
                        sourceDirectoryName = Path.Combine(sourceDirectoryName, directoryName.Substring(FilterWorker.CloudFolder.Length + 1));
                    }
                }
                else
                {
                    Console.WriteLine(directoryName + " doesn't match with the CloudFolder:" + FilterWorker.CloudFolder);
                    return false;
                }

                cacheDirName = GetFolderListCacheFileName(directoryName);

                fs = new FileStream(cacheDirName, FileMode.Create, FileAccess.ReadWrite, FileShare.None);

                int currerentFolderFileId = 0;

                if (Directory.Exists(sourceDirectoryName))
                {
                    DirectoryInfo directoryInfo = new DirectoryInfo(sourceDirectoryName);
                    int fileCount = 0;
                    int dirCount = 0;
                    foreach (DirectoryInfo dirInfo in directoryInfo.GetDirectories())
                    {
                        FileEntry fileEntry = new FileEntry();

                        fileEntry.Flags = 0; //reserve field
                        fileEntry.FileId = ++currerentFolderFileId;
                        fileEntry.FileAttributes = (uint)FileAttributes.Directory;
                        fileEntry.FileName = dirInfo.Name;
                        fileEntry.FileNameLength = (uint)dirInfo.Name.Length * 2;
                        fileEntry.FileSize = 0;
                        fileEntry.CreationTime = dirInfo.CreationTime.ToFileTime();
                        fileEntry.LastAccessTime = dirInfo.LastAccessTime.ToFileTime();
                        fileEntry.LastWriteTime = dirInfo.LastWriteTime.ToFileTime();

                        //this is the custom tag data which associated to every file, you can put data here,
                        //when the user is going to read the virtual file, the tag data will be provided by the filter driver.
                        fileEntry.TagData = new byte[10];
                        fileEntry.TagDataLength = (uint)fileEntry.TagData.Length;


                        //the whole file entry size 
                        uint FILE_ENTRY_STRUCT_SIZE = 4/*entryLength*/ + 4/*flags*/ + 4/*FileAttributes*/ + 8/*Index*/+ 8/*FileSize*/ + 8/*creationTime*/
                                                  + 8/*LastAccessTime*/ + 8/*LastWriteTime*/  + 4/*TagDataLength*/ + 4/*FileNameLength*/ ;

                        fileEntry.EntryLength = FILE_ENTRY_STRUCT_SIZE + (uint)dirInfo.Name.Length * 2 + (uint)fileEntry.TagDataLength;

                        dirCount++;

                        WriteFileEntry(fs, fileEntry);

                    }

                    foreach (FileInfo fileInfo in directoryInfo.GetFiles())
                    {
                        FileEntry fileEntry = new FileEntry();

                        fileEntry.Flags = 0; //reserve field
                        fileEntry.FileId = ++currerentFolderFileId;
                        fileEntry.FileAttributes = (uint)fileInfo.Attributes | (uint)FileAttributes.Offline;
                        fileEntry.FileName = fileInfo.Name;
                        fileEntry.FileNameLength = (uint)fileInfo.Name.Length * 2;
                        fileEntry.FileSize = fileInfo.Length;
                        fileEntry.CreationTime = fileInfo.CreationTime.ToFileTime();
                        fileEntry.LastAccessTime = fileInfo.LastAccessTime.ToFileTime();
                        fileEntry.LastWriteTime = fileInfo.LastWriteTime.ToFileTime();

                        //this is the custom tag data which associated to every file, you can put data here,
                        //when the user is going to read the cloud file, the tag data will be provided by the filter driver.
                        fileEntry.TagData = UnicodeEncoding.Unicode.GetBytes("\\??\\" + fileInfo.FullName);
                        fileEntry.TagDataLength = (uint)fileEntry.TagData.Length;


                        //the whole file entry size 
                        uint FILE_ENTRY_STRUCT_SIZE = 4/*entryLength*/ + 4/*flags*/ + 4/*FileAttributes*/ + 8/*Index*/+ 8/*FileSize*/ + 8/*creationTime*/
                                                  + 8/*LastAccessTime*/ + 8/*LastWriteTime*/  + 4/*TagDataLength*/ + 4/*FileNameLength*/ ;

                        fileEntry.EntryLength = FILE_ENTRY_STRUCT_SIZE + (uint)fileInfo.Name.Length * 2 + (uint)fileEntry.TagDataLength;

                        fileCount++;

                        WriteFileEntry(fs, fileEntry);

                    }

                   Console.WriteLine("Request test cloud folder " + directoryName + "\nFile list cache file name:" + cacheDirName + "\ntotalFolders:" + dirCount + ",totalFiles:" + fileCount + "\r\n");
                }
                else
                {
                    Console.WriteLine("Get directory " + directoryName + " source folder:" + sourceDirectoryName + " doesn't exist");
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Get directory " + directoryName + " file list got exception:" + ex.Message);
                return false;
            }
            finally
            {
                if (null != fs)
                {
                    fs.Flush();
                    fs.Close();
                }
            }

        }       
      
    }

}
