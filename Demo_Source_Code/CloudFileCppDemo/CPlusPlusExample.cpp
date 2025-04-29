///////////////////////////////////////////////////////////////////////////////
//
//    (C) Copyright 2011 EaseClouds Technologies Inc.
//    All Rights Reserved
//
//    This software is part of a licensed software product and may
//    only be used or copied in accordance with the terms of that license.
//
///////////////////////////////////////////////////////////////////////////////

// CPlusPlusDemo.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include "Tools.h"
#include "FilterDriverHandler.h"

//set the include directory for this libary to folder Bin\x64  or Bin\Win32
#pragma comment(lib, "FilterAPI.lib")

#define	MAX_ERROR_MESSAGE_SIZE	1024

#define PrintMessage	wprintf //ToDebugger

VOID
__stdcall
DisconnectCallback();

BOOL
__stdcall
MessageCallback(
   IN		PMESSAGE_SEND_DATA pSendMessage,
   IN OUT	PMESSAGE_REPLY_DATA pReplyMessage);

void 
SendConfigInfoToFilter(ULONG FilterType,WCHAR* FilterFolder,ULONG IoRegistration ,ULONG AccessFlag);


VOID
Usage (
    VOID
    )
/*++

Routine Description

    Prints usage

Arguments

    None

Return Value

    None

--*/
{

	printf( "\nUsage:		CPlusPlusExample  <option>\n" );
	printf( "\noption:\n" );
	printf( "		i ----- Install Driver\n" );
	printf( "		u ----- UnInstall Driver\n" );
	printf( "		t ----- Driver UnitTest\n" );
	
}


int _tmain(int argc, _TCHAR* argv[])
{
    DWORD	threadCount = 5;
	BOOL	ret = FALSE;
	
	//Purchase a license key with the link: http://www.EaseClouds.com/Order.html
    //Email us to request a trial key: info@EaseClouds.com //free email is not accepted.
	char*	registerKey = "*********************************";


	if(argc <= 1)
	{
		Usage();
		return 1;
	}

	TCHAR op = *argv[1];

	switch(op)
	{
		case 'i': //install driver
		{
			//Install the driver only once.
			ret = InstallDriver();	
			if( !ret )
			{
				PrintLastErrorMessage( L"InstallDriver failed.",__LINE__);
				return 1;
			}

			PrintPassedMessage(L"Install filter driver succeeded!");

			break;
		}

		case 'u': //uninstall driver
		{
			ret = UnInstallDriver();
			if( !ret )
			{
				PrintLastErrorMessage( L"UnInstallDriver failed.",__LINE__);
				return 1;
			}

			PrintPassedMessage(L"UnInstall filter driver succeeded!");

			break;

		}

		case 't': //driver unit test 
		{
			ret = UnInstallDriver();
			Sleep(2000);

			ret = InstallDriver();	
			if( !ret )
			{
				PrintLastErrorMessage( L"InstallDriver failed.",__LINE__);
				return 1;
				return 1;
			}

			PrintPassedMessage(L"Install filter driver succeeded!");


			ret = SetRegistrationKey(registerKey);
			if( !ret )
			{
				PrintLastErrorMessage( L"SetRegistrationKey failed.",__LINE__);
				return 1;
			}

			ret = RegisterMessageCallback(threadCount,MessageCallback,DisconnectCallback);
			if( !ret )
			{
				PrintLastErrorMessage( L"RegisterMessageCallback failed.",__LINE__);
				return 1;
			}			

			CreateTestFiles();

			//this the demo how to use the stub file API.
			SendConfigSettingsToFilter();

			printf("\r\n\r\n\r\nNow you can test the virtual file in folder c:\\EaseCloudsTest\\virtualFolder.\r\n\r\n\r\n."); 

			system("pause");

			Disconnect();

			break;

		}

		default:
			{
				Usage(); 
				return 1;
			}

	}
		
	return 0;
}



BOOL
__stdcall
MessageCallback(
   IN		PMESSAGE_SEND_DATA pSendMessage,
   IN OUT	PMESSAGE_REPLY_DATA pReplyMessage)
{
	BOOL	ret = TRUE;

	WCHAR userName[MAX_PATH];
	WCHAR domainName[MAX_PATH];

	int userNameSize = MAX_PATH;
	int domainNameSize = MAX_PATH;
	SID_NAME_USE snu;

	__try
	{
		BOOL ret = LookupAccountSid( NULL,
									pSendMessage->Sid,
									userName,
									(LPDWORD)&userNameSize,
									domainName,
									(LPDWORD)&domainNameSize,
									&snu); 
	
		PrintMessage( L"\nId#[%d] MessageType:[0X%0x]\n UserName:[%ws\\%ws]  ProcessId:[%d] ThreadId:[%d]\nFileSize:[%I64d] Attributes:[%0x] FileName:%ws\n"
			,pSendMessage->MessageId,pSendMessage->MessageType,domainName,userName
			,pSendMessage->ProcessId,pSendMessage->ThreadId
			,pSendMessage->FileSize,pSendMessage->FileAttributes,pSendMessage->FileName);

		switch(pSendMessage->MessageType)
		{
			case MESSAGE_TYPE_SEND_EVENT_NOTIFICATION:
			{
				ULONG eventType = pSendMessage->InfoClass;

				switch(eventType)
				{
					case FILE_CREATEED: PrintMessage(L"FILE_CREATEED Event,new file %ws was created.\n",pSendMessage->FileName);break;
					case FILE_CHANGED: PrintMessage(L"FILE_CHANGED Event,file %ws was modified.\n",pSendMessage->FileName);break;
					case FILE_RENAMED: PrintMessage(L"FILE_RENAMED Event,file %ws was rename to %ws.\n",pSendMessage->FileName,pSendMessage->DataBuffer);break;
					case FILE_DELETED: PrintMessage(L"FILE_DELETED Event,file %ws was deleted.\n",pSendMessage->FileName);break;
					default:PrintMessage(L"Unknow eventType:%0x.\n",eventType);break;
				}

				break;
			}


			case MESSAGE_TYPE_GET_FILE_LIST:
                {
                    //To get the directory file list in a cache file.
					ret = GetFileList(pSendMessage, pReplyMessage);

                    break;
                }
            case MESSAGE_TYPE_RESTORE_FILE_TO_CACHE:
                {
                    //for memory mapping file open( for example open file with notepad in local computer,
                    //it also needs to download the whole cache file and return the cache file name to the filter driver,
                    //the filter driver will read the cache file data.

                    ret = DownloadCacheFile(pSendMessage, pReplyMessage,FALSE);
                    break;
                }
            case MESSAGE_TYPE_RESTORE_FILE_TO_ORIGINAL_FOLDER:
                {
                    //for the write request or rename request, the filter driver needs to restore the whole file first,
                    //here we need to download the whole file to the virtual folder, replace the virtual file with the physical file.
                    ret = DownloadCacheFile(pSendMessage, pReplyMessage,TRUE);
                    break;
                }
            case MESSAGE_TYPE_RESTORE_BLOCK_OR_FILE:
                {

                    //for this request, the user is trying to read block of data, you can either return the whole cache file if the whole cache file was downloaded
                    //or you can just restore the block of data as the request needs.

                    //ret = DownloadCacheFile(cacheFileName, replyDataPtr);

                    ret = GetRequestedBlockData(pSendMessage, pReplyMessage);
                    break;
                }                    
            case MESSAGE_TYPE_DELETE_FILE:
                {
                    //before the file is going to delete, it send this request for permission, you can block or allow the request continue.
                    //the filter driver is blocking for the response.
                    printf("Deleting the file %s \n", pSendMessage->FileName );
                    ret = true;
                    break;
                }
            case MESSAGE_TYPE_RENAME_FILE:
                {
                    //before the file is going to rename, it send this request for permission, you can block or allow the request continue.
                    //the filter driver is blocking for the response.
					printf("Renaming the file %s to new file name:%s\n", pSendMessage->FileName,pSendMessage->DataBuffer);

                    ret = true;
                    break;
                }
                   
			default:
				{
					printf("Message type %d is not valid.\n",pSendMessage->MessageType);
					break;
				}
		}

	}
	__except( EXCEPTION_EXECUTE_HANDLER  )
	{
		PrintErrorMessage( L"MessageCallback failed.",GetLastError());     
	}


	return ret;
}

VOID
__stdcall
DisconnectCallback()
{
	PrintLastErrorMessage(L"Filter connection was disconnected.",__LINE__);
	return;
}



