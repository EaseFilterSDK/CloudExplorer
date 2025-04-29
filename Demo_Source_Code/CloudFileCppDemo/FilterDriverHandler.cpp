///////////////////////////////////////////////////////////////////////////////
//
//    (C) Copyright 2011 EaseClouds Technologies Inc.
//    All Rights Reserved
//
//    This software is part of a licensed software product and may
//    only be used or copied in accordance with the terms of that license.
//
///////////////////////////////////////////////////////////////////////////////

#include "stdafx.h"
#include "Tools.h"
#include "FilterAPI.h"
#include "FilterDriverHandler.h"

#pragma pack (push,1)
typedef struct _DIR_ENTRY_INFO
{
	ULONG						NextEntryOffset;
	ULONG						Flags;
	ULONG						FileAttributes;
	LARGE_INTEGER  				FileId;
	LARGE_INTEGER  				FileSize;
	LARGE_INTEGER  				CreationTime;	
	LARGE_INTEGER  				LastAccessTime;	
	LARGE_INTEGER  				LastWriteTime;
	ULONG						TagDataLength;
	ULONG						FileNameLength;
	//file name buffer here
	//tag data buffer here

} DIR_ENTRY_INFO, *PDIR_ENTRY_INFO;
#pragma pack(pop)


#define	MAX_ERROR_MESSAGE_SIZE	1024

/// <summary>
/// The cache directory file list life time
/// </summary>
static  ULONG expireCachedDirectoryListingAfterSeconds = 60;

/// <summary>
/// the maximum dir info cache size in kernel memory.
/// </summary>
static  ULONG maxDirCacheSizeInKernel = 10 * 1024 * 1024; //10MB

/// <summary>
/// this is the time out of the virtual file system wait for the reply of the request.
/// </summary>
static  ULONG fileSystemWaitTimeoutInSeconds = 120;

/// <summary>
/// this is the folder to store all the physical files for the virtual folder test.
/// </summary>
 static WCHAR* sourceFolder = L"c:\\EaseCloudsTest\\sourceFolder";

/// <summary>
/// This is the test virtual folder which is empty folder without files.
/// </summary>
 static WCHAR* virtualFolder = L"c:\\EaseCloudsTest\\virtualFolder";

/// <summary>
/// This is the cache folder to store the directory list cache files.
/// </summary>
 static WCHAR* cacheFolder = L"c:\\EaseCloudsTest\\cacheFolder";

/// <summary>
/// the default name to store the directory file list in cache folder.
/// </summary>
 static WCHAR* dirInfoListName = L"Dirlist.dat";


VOID
CreateTestFiles()
{
	BOOL ret = FALSE;
	HANDLE hFile = INVALID_HANDLE_VALUE;
	
	ret = CreateDirectory(virtualFolder,NULL);
	if( !ret )
	{
		DWORD lastError = GetLastError();
		if( lastError != ERROR_ALREADY_EXISTS )
		{
			wprintf(L"Create virtualFolder %s failed, last error is:%d",virtualFolder,lastError);
			return;
		}
	}	

	ret = CreateDirectory(sourceFolder,NULL);
	if( !ret )
	{
		DWORD lastError = GetLastError();
		if( lastError != ERROR_ALREADY_EXISTS )
		{
			wprintf(L"Create sourceFolder %s failed, last error is:%d",sourceFolder,lastError);
			return;
		}
	}	

 	ret = CreateDirectory(cacheFolder,NULL);
	if( !ret )
	{
		DWORD lastError = GetLastError();
		if( lastError != ERROR_ALREADY_EXISTS )
		{
			wprintf(L"Create cacheFolder %s failed, last error is:%d",cacheFolder,lastError);
			return;
		}
	}	

	//we will create the test files in source folder.
	//when the user open the test folder, it will get file list from the source folder,
	//if user read the virtual file, it should read the data from source folder.

	char* testFileContent = "This is a test file for virtual folder.\r\n";
	WCHAR testFileName[260];

	ZeroMemory(testFileName, sizeof(testFileName));
	for ( ULONG i = 0; i < 10; i++)
	{
		swprintf_s(testFileName,L"%s\\test.%d.txt",sourceFolder,i);

		HANDLE handle = CreateFile(testFileName, GENERIC_WRITE,NULL,NULL,CREATE_ALWAYS,FILE_ATTRIBUTE_NORMAL,NULL );

		if( handle == INVALID_HANDLE_VALUE)
		{
			wprintf(L"Create test file %s failed, last error is:%d",testFileName,GetLastError());
			continue;
		}

		DWORD bytesWritten = 0;

		for( ULONG j = 0; j < (i+1)*1024; j++)
		{
			if(! WriteFile(handle,(LPVOID)testFileContent,(DWORD)strlen(testFileContent),&bytesWritten,NULL))
			{
				wprintf(L"WriteFile %s failed, last error is:%d",testFileName,GetLastError());
				
				CloseHandle(handle);
				continue;
			}
		}

		CloseHandle(handle);

		wprintf(L"Created test file %s\r\n",testFileName);

	}

}

BOOL
GetFileList(
	IN		PMESSAGE_SEND_DATA pSendMessage,
	IN OUT	PMESSAGE_REPLY_DATA pReplyMessage)
{
	//you can provide your own virtual folder file list in a cache dir file with the format as DIR_ENTRY_INFO.
	//here we just demo how to get file list from the source folder.

	//the cache dir list name should be the cache folder + directory folder + dirlist name
	//for exameple: c:\\EaseCloudsTest\\virtualFolder\\test, the cache dir list name should be c:\\EaseCloudsTest\\cacheFolder\\test\\Dirlist.dat
	//the test source folder name should be c:\\EaseCloudsTest\\sourceFolder\\test

	WCHAR* cacheDirListName = (WCHAR*)pReplyMessage->DataBuffer;
	WCHAR* folderName = (WCHAR*)pSendMessage->FileName;

	WCHAR testSourceFolder[260];
	WCHAR* testFolderName = NULL;

	ZeroMemory(cacheDirListName,pReplyMessage->DataBufferLength);
	
	if( wcslen(folderName ) > wcslen(L"\\??\\") + wcslen(virtualFolder) )
	{
		testFolderName = (WCHAR*)(folderName +  wcslen(L"\\??\\") + wcslen(virtualFolder));
	}
	
	if( testFolderName )
	{
		//The test virtual folder will be mapped to the source folder.
		swprintf_s(testSourceFolder,L"%s%s\\*",sourceFolder,testFolderName);

		//this is the cache folder
		swprintf_s(cacheDirListName,BLOCK_SIZE/2,L"%s%s",cacheFolder,testFolderName);
	
	}
	else
	{
		//The test virtual folder will be mapped to the source folder.
		swprintf_s(testSourceFolder,L"%s\\*",sourceFolder);
		//this is the cache folder
		swprintf_s(cacheDirListName,BLOCK_SIZE/2,L"%s",cacheFolder);
	}

	
	if(!CreateDirectory(cacheDirListName,NULL))
	{
		DWORD lastError = GetLastError();
		if( lastError != ERROR_ALREADY_EXISTS )
		{
			wprintf(L"Create cache folder %s failed, last error is:%d",cacheDirListName,lastError);
			return FALSE;
		}
	}


	//this is the cache directory file name
	if( testFolderName )
	{
		swprintf_s(cacheDirListName,BLOCK_SIZE/2,L"\\??\\%s%s\\%s",cacheFolder,testFolderName,dirInfoListName);
	}
	else
	{
		swprintf_s(cacheDirListName,BLOCK_SIZE/2,L"\\??\\%s\\%s",cacheFolder,dirInfoListName);
	}

	HANDLE handle = CreateFile(cacheDirListName, GENERIC_WRITE,NULL,NULL,CREATE_ALWAYS,FILE_ATTRIBUTE_NORMAL,NULL );

	if( handle == INVALID_HANDLE_VALUE)
	{
		wprintf(L"Create cache dir file  %s failed, last error is:%d",cacheDirListName,GetLastError());
		return FALSE;
	}
	

	WIN32_FIND_DATA ffd;
	HANDLE pFile = FindFirstFile(testSourceFolder, &ffd);

	if( pFile == INVALID_HANDLE_VALUE)
	{
		wprintf(L"FindFirstFile  %s failed, last error is:%d",testSourceFolder,GetLastError());

		CloseHandle(handle);
		return FALSE;
	}

	ULONG currerentFolderFileId  = 0;

	while (FindNextFile(pFile, &ffd) != 0)
    {
		if (ffd.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY)
		{
			if(		( wcslen( ffd.cFileName) == 1 && (ffd.cFileName[0] == (WCHAR)'.' ))
				||	( wcslen( ffd.cFileName) == 2 && (ffd.cFileName[0] == (WCHAR)'.' ) && (ffd.cFileName[1] == (WCHAR)'.' )))
			{
			//this is the default directory name,skip it.
			continue;
			}
		}

		DIR_ENTRY_INFO fileEntry;

		fileEntry.Flags = 0; //reserve field
		fileEntry.FileId.QuadPart = ++currerentFolderFileId;
		fileEntry.FileAttributes = ffd.dwFileAttributes | FILE_ATTRIBUTE_OFFLINE; //add offline attribute to virtual file.
		fileEntry.FileNameLength = (ULONG)wcslen(ffd.cFileName) * 2;
		fileEntry.FileSize.LowPart = ffd.nFileSizeLow;
		fileEntry.FileSize.HighPart = ffd.nFileSizeHigh;
		fileEntry.LastAccessTime.LowPart = ffd.ftLastAccessTime.dwLowDateTime;
		fileEntry.LastAccessTime.HighPart = ffd.ftLastAccessTime.dwHighDateTime;
		fileEntry.CreationTime.LowPart = ffd.ftCreationTime.dwLowDateTime;
		fileEntry.CreationTime.HighPart = ffd.ftCreationTime.dwHighDateTime;
		fileEntry.LastWriteTime.LowPart = ffd.ftLastWriteTime.dwLowDateTime;
		fileEntry.LastWriteTime.HighPart = ffd.ftLastWriteTime.dwHighDateTime;
		//this is the custom tag data which associated to every file, you can put data here,
		//when the user is going to read the virtual file, the tag data will be provided by the filter driver.
		fileEntry.TagDataLength = 0;

		//the whole file entry size 
		ULONG FILE_ENTRY_STRUCT_SIZE = 4/*entryLength*/ + 4/*flags*/ + 4/*FileAttributes*/ + 8/*Index*/+ 8/*FileSize*/ + 8/*creationTime*/
								+ 8/*LastAccessTime*/ + 8/*LastWriteTime*/  + 4/*TagDataLength*/ + 4/*FileNameLength*/ ;

		fileEntry.NextEntryOffset = FILE_ENTRY_STRUCT_SIZE + fileEntry.FileNameLength + 0;

		DWORD bytesWritten = 0;
		WriteFile(handle,&fileEntry,sizeof(fileEntry),&bytesWritten,NULL);
		WriteFile(handle,ffd.cFileName,(DWORD)wcslen(ffd.cFileName) * 2,&bytesWritten,NULL);
    
	}
  

	if( handle )
	{
		CloseHandle(handle);
	}

   if( pFile )
   {
	   FindClose(pFile);
   }

	pReplyMessage->DataBufferLength = (ULONG)wcslen(cacheDirListName)*2;
	pReplyMessage->FilterStatus = CACHE_FILE_WAS_RETURNED;

   return TRUE;
}


BOOL
EventTest()
{
	BOOL ret = FALSE;	
	DWORD dwTransferred = 0;
	DWORD nError = 0;

	CHAR* testEventFile = "c:\\EaseCloudsTest\\virtualFolder\\testEventFile.txt";
	CHAR* testEventRenameFile = "c:\\EaseCloudsTest\\virtualFolder\\testEventFile.rename.txt";
	CHAR*  testData = "This is a test data.";

	//create test event file
	HANDLE pFile = CreateFileA(testEventFile,GENERIC_ALL,NULL,NULL,CREATE_ALWAYS,FILE_ATTRIBUTE_NORMAL,NULL);
	if( INVALID_HANDLE_VALUE == pFile )
	{
		PrintErrorMessage(L"Open file for targetTestFile failed.",GetLastError());
		goto EXIT;
	}

	CloseHandle(pFile);
	printf("Create test file %s\n",testEventFile);

	pFile = CreateFileA(testEventFile,GENERIC_ALL,NULL,NULL,OPEN_EXISTING,FILE_ATTRIBUTE_NORMAL,NULL);
	if( INVALID_HANDLE_VALUE == pFile )
	{
		PrintErrorMessage(L"Open file for targetTestFile failed.",GetLastError());
		goto EXIT;
	}
	if(!WriteFile(pFile,testData,(DWORD)strlen(testData), (LPDWORD)&dwTransferred, NULL))
	{
		nError = GetLastError();
		PrintErrorMessage(L"WriteFile failed.", nError);
		ret = FALSE;
		goto EXIT;
	}

	printf("Write data to test file %s\n",testEventFile);

	CloseHandle(pFile);

	ULONG result= rename( testEventFile , testEventRenameFile );
	if ( result == 0 )
		puts ( "File successfully renamed" );
	else
	{
		perror( "Error renaming file" );
		goto EXIT;
	}

   if( remove( testEventRenameFile ) != 0 )
   {
		perror( "Error deleting file" );
		goto EXIT;
   }
   else
		puts( "File successfully deleted" );

   Sleep(2000);

	ret = TRUE;

EXIT:
	return ret;

}

BOOL
GetRequestedBlockData(
	IN		PMESSAGE_SEND_DATA pSendMessage,
	IN OUT	PMESSAGE_REPLY_DATA pReplyMessage)
{
	//for our test, we only assume that if the file exist in the source folder, then this is our test files,
	//we will process it, or we return false;
	WCHAR* fileName = pSendMessage->FileName;
	ULONG  fileNameLength = pSendMessage->FileNameLength;
	WCHAR* cacheFileName = (PWCHAR)pReplyMessage->DataBuffer;

	memset(cacheFileName,0,BLOCK_SIZE);

	fileName[fileNameLength] = '\0';

	if( fileName[0] == (WCHAR)'\\')
	{
		//remove the prefix "\\??\\"
		fileName = &(pSendMessage->FileName[4]);
	}


	//get the test cache file name from the source folder.
	//for example: c:\EaseCloudsTest\virtualFolder\test1\test.txt  ==>  c:\EaseCloudsTest\sourceFolder\test1\test.txt

	fileName = &(fileName[wcslen(virtualFolder)]);
	swprintf_s(cacheFileName,BLOCK_SIZE/2,L"\\??\\%s%s",sourceFolder,fileName);

	if(GetFileAttributes(cacheFileName) != INVALID_FILE_ATTRIBUTES)
	{
				
		HANDLE handle = CreateFile(cacheFileName, GENERIC_READ,FILE_SHARE_READ,NULL,OPEN_EXISTING,FILE_ATTRIBUTE_NORMAL,NULL );

		if( handle == INVALID_HANDLE_VALUE)
		{
			wprintf(L"Open cache file  %s failed, last error is:%d",cacheFileName,GetLastError());
			return false;
		}

		LARGE_INTEGER distanceToMove;
		distanceToMove.QuadPart = pSendMessage->Offset;

		SetFilePointerEx(handle,distanceToMove,NULL,FILE_BEGIN);

		if( ReadFile(handle,pReplyMessage->DataBuffer,pSendMessage->Length,&pReplyMessage->DataBufferLength,NULL))
		{
			pReplyMessage->FilterStatus = BLOCK_DATA_WAS_RETURNED;
			pReplyMessage->ReturnStatus = STATUS_SUCCESS;
		}
		else
		{
			CloseHandle(handle);

			PrintErrorMessage( L"Read cache file failed.",GetLastError()); 
			return false;
		}

		CloseHandle(handle);

		return TRUE;
	}
	else
	{
		PrintErrorMessage( L"Download cache file failed.",0); 
		return FALSE;
	}
}

BOOL
DownloadCacheFile(  
	IN		PMESSAGE_SEND_DATA pSendMessage,
	IN OUT	PMESSAGE_REPLY_DATA pReplyMessage,
	IN		BOOL toOriginalFolder )
{
	//for our test, we only assume that if the file exist in the source folder, then this is our test files,
	//we will process it, or we return false;
	WCHAR* fileName = pSendMessage->FileName;
	WCHAR* cacheFileName = (PWCHAR)pReplyMessage->DataBuffer;

	memset(cacheFileName,0,BLOCK_SIZE);


	if( fileName[0] == (WCHAR)'\\')
	{
		//remove the prefix "\\??\\"
		fileName = &(pSendMessage->FileName[4]);
	}


	//get the test cache file name from the source folder.
	//for example: c:\EaseCloudsTest\virtualFolder\test1\test.txt  ==>  c:\EaseCloudsTest\sourceFolder\test1\test.txt

	fileName = &(fileName[wcslen(virtualFolder)]);
	swprintf_s(cacheFileName,BLOCK_SIZE/2,L"\\??\\%s%s",sourceFolder,fileName);

	if(GetFileAttributes(cacheFileName) != INVALID_FILE_ATTRIBUTES)
	{
		pReplyMessage->FilterStatus = CACHE_FILE_WAS_RETURNED;
		pReplyMessage->ReturnStatus = STATUS_SUCCESS;
		
		ULONG dataLength = (ULONG)wcslen(cacheFileName)*2;		

		if( toOriginalFolder )
		{
			//replace the virtual file with the test file here.
			if(!CopyFile(cacheFileName,pSendMessage->FileName,TRUE))
			{
				PrintErrorMessage( L"Copy cache file to original folder failed.",GetLastError()); 
			}

			dataLength = (ULONG)wcslen(pSendMessage->FileName)*2;
			memcpy(pReplyMessage->DataBuffer,pSendMessage->FileName,dataLength);
		}
		else
		{
			memcpy(pReplyMessage->DataBuffer,cacheFileName,dataLength);
		}

		pReplyMessage->DataBufferLength = dataLength;

		return TRUE;
	}
	else
	{
		PrintErrorMessage( L"Download cache file failed.",0); 
		return FALSE;
	}
}
	 
void 
SendConfigSettingsToFilter()
{
	int bufferSize = 260;
	WCHAR* filterMask  = NULL;

	BOOL ret = FALSE;

	//Reset the filter config setting.
	ret = ResetConfigData();
	if( !ret )
	{
		PrintLastErrorMessage( L"ResetConfigData failed.",__LINE__);
		goto EXIT;
	}


	//Set filter maiximum wait for user mode response time out.
	ret = SetConnectionTimeout(30); 
	if( !ret )
	{
		PrintLastErrorMessage( L"SetConnectionTimeout failed.",__LINE__);
		goto EXIT;
	}

	//exclude the current process from the filter driver

	ULONG  currentPid = GetCurrentProcessId();
    ret = AddExcludedProcessId(currentPid);
    if (!ret)
    {
		PrintLastErrorMessage( L"AddExcludedProcessId failed.",__LINE__);
        goto EXIT;
    }
    else
    {
      printf("Set Excluded current process id %d succeeded.\n",currentPid);
    }

    //set the managed virtual folder by filter driver.
	filterMask = (WCHAR*)malloc( bufferSize );
	ZeroMemory(filterMask,bufferSize);
	swprintf_s(filterMask,bufferSize/2,L"%s\\*",virtualFolder);
    ret = AddNewFilterRule(ALLOW_MAX_RIGHT_ACCESS, filterMask);	

    if (!ret)
    {
       PrintLastErrorMessage( L"AddNewFilterRule failed.",__LINE__);
       goto EXIT;
    }
    else
    {
       printf("Set virtual folder %ws succeeded.\n",filterMask);
    }

                
    //Register the File IO event notification for the virtual folder.
    ULONG eventType = FILE_CREATEED | FILE_CHANGED;
    ret =RegisterEventTypeToFilterRule(filterMask, eventType);
    if (!ret)
    {
       PrintLastErrorMessage( L"RegisterEventTypeToFilterRule failed.",__LINE__);
       goto EXIT;
    }
    else
    {
       printf("RegisterEventTypeToFilterRule %x succeeded.\n",eventType);
    }

                    
    //set the time to live for the directory cache in the filter driver.
    ret = SetIntegerData(DIR_CACHE_TIMEOUT,expireCachedDirectoryListingAfterSeconds);
    if (!ret)
    {
       PrintLastErrorMessage( L"Set expireCachedDirectoryListingAfterSeconds failed.",__LINE__);
      goto EXIT;
    }
    else
    {
       printf("Set expireCachedDirectoryListingAfterSeconds %d succeeded.\n",expireCachedDirectoryListingAfterSeconds);
    }


    //set the maximum directory cache size in filter driver.
	 ret = SetIntegerData(MAX_TOTAL_DIR_CACHE_SIZE,maxDirCacheSizeInKernel);
    if (!ret)
    {
       PrintLastErrorMessage( L"Set maxDirCacheSizeInKernel failed.",__LINE__);
       goto EXIT;
    }
    else
    {
       printf("Set maxDirCacheSizeInKernel %d succeeded.\n",maxDirCacheSizeInKernel);
    }


	printf("\n\nTest virtual file instruction:\n");
	printf("Creat/Copy your test files to source folder:%ws, then you can open the virtual files from the virtual folder:%ws\n\n", sourceFolder,virtualFolder);

EXIT:

	if( filterMask)
	{
		free(filterMask);
	}

}