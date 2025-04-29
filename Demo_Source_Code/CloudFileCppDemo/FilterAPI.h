///////////////////////////////////////////////////////////////////////////////
//
//    (C) Copyright 2011 EaseClouds Technologies Inc.
//    All Rights Reserved
//
//    This software is part of a licensed software product and may
//    only be used or copied in accordance with the terms of that license.
//
//	  This header file includes the structures and exported API from the FilterAPI.DLL
//	    	
//
///////////////////////////////////////////////////////////////////////////////


#ifndef __FILTER_API_H__
#define __FILTER_API_H__

#define	STATUS_SUCCESS						0
#define STATUS_REPARSE						0x00000104L
#define STATUS_NO_MORE_FILES				0x80000006L
#define STATUS_WARNING						(ULONG)0x80000000
#define STATUS_ERROR						(ULONG)0xc0000000
#define STATUS_UNSUCCESSFUL					0xc0000001L
#define STATUS_END_OF_FILE                  0xC0000011L
#define STATUS_ACCESS_DENIED				0xC0000022L

#define MESSAGE_SEND_VERIFICATION_NUMBER	0xFF000001
#define BLOCK_SIZE							65536
#define MAX_FILE_NAME_LENGTH				1024
#define MAX_SID_LENGTH						256
#define MAX_PATH							260
#define ALLOW_MAX_RIGHT_ACCESS				0xfffffff0

/// <summary>
/// the message type of the filter driver send request 
/// </summary>
typedef enum  _MessageType
{
    /// <summary>
    /// This message type indicates you can restore the full content of the file, 
    /// or restore the request block of data from the offset and length
    /// </summary>
    MESSAGE_TYPE_RESTORE_BLOCK_OR_FILE = 0x00000001,

    /// <summary>
    /// This message type indicates you have to restore the full content of the file.
    /// </summary>
    MESSAGE_TYPE_RESTORE_FILE_TO_ORIGINAL_FOLDER = 0x00000002,

    /// <summary>
    /// request to get the directory file list
    /// </summary>
    MESSAGE_TYPE_GET_FILE_LIST = 0x00000004,

    /// <summary>
    /// require to download the whole file to the cache folder,and return cache file name to filter
    /// </summary>
    MESSAGE_TYPE_RESTORE_FILE_TO_CACHE = 0x00000008,

    /// <summary>
    /// This is the notification event of the file, it doesn't need to reply the request.
    /// </summary>
    MESSAGE_TYPE_SEND_EVENT_NOTIFICATION = 0x00000010,

    /// <summary>
    /// request to delete file
    /// </summary>
    MESSAGE_TYPE_DELETE_FILE = 0x00000020,

    /// <summary>
    /// request to rename the file
    /// </summary>
    MESSAGE_TYPE_RENAME_FILE = 0x00000040,

    /// <summary>
    /// Queue the file changed event to a file to batch the event messages.
    /// </summary>
    MESSAGE_TYPE_SEND_MESSAGE_FILENAME = 0x00000080,

}MessageType;


// the user mode app sends the integer data to filter driver, this is the index of the integer data.
typedef enum _DataControlId 
{
	FILTER_TYPE_ID = 1,			//The filter driver type.
	EVENT_OUTPUT_TYPE_ID,		//Control send the event output type.
	EVENT_LEVEL_ID,				//Control send event level.
	EVENT_FLAGS_ID,				//Control send the event modules
	CONNECTION_TIMEOUT_ID,		//Control send client connection timout in seconds.
	BOOLEAN_CONFIG_ID,			//All the boolean config data setting
	WAIT_BLOCK_DATA_INTERVAL,	//the interval time in milliseconds to wait for the block data download
	WAIT_BLOCK_DATA_TIMEOUT,	//the timeout in milliseconds to wait for the block data ready
	DIR_CACHE_TIMEOUT,			//the directory cache file list time to live in milliseconds
	MAX_TOTAL_DIR_CACHE_SIZE,   //the total size of the dir info buffer
	DELETE_NO_ACCESS_DIR_INFO_IN_SECONDS,   //delete the directory info if there are no access more than this value.
	MESSAGE_IN_QUEUE_TTL_IN_SECONDS,		//set the file changed queue time to live.
	MAX_MESSAGES_IN_QUEUE, //set the maximum files can be kept in queue.

	MAX_DATA_CONTROL_ID,

} DataControlId;

//the filter rule commands 
typedef enum _FilterRuleId 
{
	ADD_NEW_FILTER_RULE = 0x100,						
	FILTER_RULE_ADD_EXCLUDE_FILE_FILTER_MASK,
	FILTER_RULE_REMOVE_EXLCUDE_FILE_FILTER_MASK,
	FILTER_RULE_ADD_HIDDEN_FILE_FILTER_MASK,
	FILTER_RULE_REMOVE_HIDDEN_FILE_FILTER_MASK,
	FILTER_RULE_ADD_REPARSE_FILTER_MASK,
	FILTER_RULE_REMOVE_REPARSE_FILTER_MASK,
	FILTER_RULE_ADD_ENCRYPTION_KEY,
	FILTER_RULE_ADD_INCLUDE_PROCESS_ID,
	FILTER_RULE_REMOVE_INCLUDE_PROCESS_ID,
	FILTER_RULE_ADD_EXCLUDE_PROCESS_ID,
	FILTER_RULE_REMOVE_EXCLUDE_PROCESS_ID,
	FILTER_RULE_REGISTER_EVENTYPE,
	FILTER_RULE_UNREGISTER_EVENTYPE,
	FILTER_RULE_ADD_INCLUDE_PROCESS_NAME,
	FILTER_RULE_REMOVE_INCLUDE_PROCESS_NAME,
	FILTER_RULE_ADD_EXCLUDE_PROCESS_NAME,
	FILTER_RULE_REMOVE_EXCLUDE_PROCESS_NAME,
	FILTER_RULE_ADD_INCLUDE_USER_NAME,
	FILTER_RULE_REMOVE_INCLUDE_USER_NAME,
	FILTER_RULE_ADD_EXCLUDE_USER_NAME,
	FILTER_RULE_REMOVE_EXCLUDE_USER_NAME,
	FILTER_RULE_REGISTER_MONITOR_IO,
	FILTER_RULE_REGISTER_CONTROL_IO,

}FilterRuleId;

//the user mode app sends the string data to the filter driver,
//this is the index of the string data.
typedef enum _StringControlId 
{
	STRING_TRASACTION_FOLDER_ID = 1,
	STRING_REALTIME_JOB_SUFFIX_ID = 2,

	MAX_STRING_CONTROL_ID,

} StringControlId;

//this is the boolean data of the user mode app sending to the filter.
//this is the boolean configuration of the filter driver.
typedef enum _BooleanConfig 
{
	ENABLE_NO_RECALL_FLAG = 0x00000001, //for easetag, if it was true, after the reparsepoint file was opened, it won't restore data back for read and write.
	DISABLE_FILTER_UNLOAD_FLAG = 0x00000002, //if it is true, the filter driver can't be unloaded.
	ENABLE_SET_OFFLINE_FLAG = 0x00000004, //for virtual file, it will set offline attribute if it is true.
	ENABLE_DEFAULT_IV_TAG = 0x00000008, //for encryption, it is true, it will use the default IV tag to encrypt the files.
	ENABLE_ADD_MESSAGE_TO_FILE = 0x00000010, //for file changed event, if it is enabled, it will save the file name to a persistent file, or it will send the event to service right away. 
	GET_ENCRYPTION_KEY_FROM_SERVICE = 0x0000020, //for encryption rule, get the encryption key from user mode for the new file creation.
	REQUEST_ENCRYPT_KEY_AND_IV_FROM_SERVICE = 0x00000040,  //for encryption rule, get the encryption key and IV from user mode for the new file creation.

} BooleanConfig;

//this is the data structure which send control message to kernel from user mode.
//first it needs to set the control type which shows as above enumeration.
//the second is the control id for integer data.
//the third is the integer data.
typedef struct _CONTROL_DATA 
{
	ULONG		ControlType;
	ULONG		ControlId;
	LONGLONG	IntegerData;
	ULONG		StringLength1;
	WCHAR		StringData1[MAX_PATH];
	ULONG		StringLength2;
	WCHAR		StringData2[MAX_PATH];
	ULONG		KeyLength;
	UCHAR		Key[MAX_PATH];
	
} CONTROL_DATA, *PCONTROL_DATA;

//the file was changed, this is the meta data of the file information.
typedef struct _FILE_CHANGED_DATA 
{
	ULONG		SizeOfEntry;
	ULONG		FileEventType;
	LONGLONG	LastWriteTime;
	ULONG		FileNameLength;
	WCHAR		FileName[1];
	//the whole file name path is appended here.

} FILE_CHANGED_DATA, *PFILE_CHANGED_DATA;
typedef enum _EVENT_TYPE 
{
	FILE_CREATEED = 0x00000020,
    FILE_CHANGED = 0x00000040,
    FILE_RENAMED = 0x00000080,
    FILE_DELETED = 0x00000100,
}EVENT_TYPE;

//The status return to filter,instruct filter what process needs to be done.
typedef enum _FilterStatus 
{
	FILTER_MESSAGE_IS_DIRTY			= 0x00000001, //Set this flag if the reply message need to be processed.
	FILTER_COMPLETE_PRE_OPERATION	= 0x00000002, //Set this flag if complete the pre operation. 
	FILTER_DATA_BUFFER_IS_UPDATED	= 0x00000004, //Set this flag if the databuffer was updated.
	BLOCK_DATA_WAS_RETURNED			= 0x00000008, //Set this flag if return read block databuffer to filter.
	CACHE_FILE_WAS_RETURNED			= 0x00000010, //Set this flag if the cache file was restored.
	
} FilterStatus;


//this is the data structure of the filter driver sending data to the user mode app.
typedef struct _MESSAGE_SEND_DATA 
{
	ULONG			MessageId;
	PVOID			FileObject;
	PVOID			FsContext;
	ULONG			MessageType;	
	ULONG			ProcessId;
    ULONG			ThreadId;   
	LONGLONG		Offset; // read/write offset 
	ULONG			Length; //read/write length
	LONGLONG		FileSize;
	LONGLONG		TransactionTime;
	LONGLONG		CreationTime;
	LONGLONG		LastAccessTime;
	LONGLONG		LastWriteTime;
	ULONG			FileAttributes;
	//The disired access,share access and disposition for Create request.
	ULONG			DesiredAccess;
	ULONG			Disposition;
	ULONG			ShareAccess;
	ULONG			CreateOptions;
	ULONG			CreateStatus;

	//For QueryInformation,SetInformation,Directory request it is information class
	//For QuerySecurity and SetSecurity request,it is securityInformation.
	ULONG			InfoClass; 

	ULONG			Status;
	ULONG			FileNameLength;
	WCHAR			FileName[MAX_FILE_NAME_LENGTH];
	ULONG			SidLength;
    UCHAR			Sid[MAX_SID_LENGTH];
	ULONG			DataBufferLength;
	UCHAR			DataBuffer[BLOCK_SIZE];

	ULONG			VerificationNumber;

} MESSAGE_SEND_DATA, *PMESSAGE_SEND_DATA;

//This the structure return back to filter,only for call back filter.
typedef struct _MESSAGE_REPLY_DATA 
{
	ULONG		MessageId;
	ULONG		MessageType;	
	ULONG		ReturnStatus;
	ULONG		FilterStatus;
	ULONG		DataBufferLength;
	UCHAR		DataBuffer[BLOCK_SIZE];	

} MESSAGE_REPLY_DATA, *PMESSAGE_REPLY_DATA;


extern "C" __declspec(dllexport) 
BOOL 
InstallDriver();

extern "C" __declspec(dllexport) 
BOOL
UnInstallDriver();

extern "C" __declspec(dllexport) 
BOOL
SetRegistrationKey(char* key);

typedef BOOL (__stdcall *Proto_Message_Callback)(
   IN		PMESSAGE_SEND_DATA pSendMessage,
   IN OUT	PMESSAGE_REPLY_DATA pReplyMessage);

typedef VOID (__stdcall *Proto_Disconnect_Callback)();

extern "C" __declspec(dllexport) 
BOOL
RegisterMessageCallback(
	ULONG ThreadCount,
	Proto_Message_Callback MessageCallback,
	Proto_Disconnect_Callback DisconnectCallback );

extern "C" __declspec(dllexport) 
VOID
Disconnect();

extern "C" __declspec(dllexport) 
BOOL
GetLastErrorMessage(WCHAR* Buffer, PULONG BufferLength);

extern "C" __declspec(dllexport)
BOOL
ResetConfigData();

extern "C" __declspec(dllexport)
BOOL
SetIntegerData(ULONG dataControlId, LONGLONG data );

extern "C" __declspec(dllexport)
BOOL
SetStringData(ULONG stringControlId, WCHAR* stringData);

extern "C" __declspec(dllexport)  
BOOL
SetBooleanConfig(ULONG booleanConfig);

extern "C" __declspec(dllexport)  
BOOL
SetConnectionTimeout(ULONG TimeOutInSeconds);

extern "C" __declspec(dllexport) 
BOOL 
AddExcludedProcessId(ULONG ProcessId);

extern "C" __declspec(dllexport) 
BOOL 
RemoveExcludeProcessId(ULONG ProcessId);

extern "C" __declspec(dllexport) 
BOOL 
AddNewFilterRule(ULONG accessFlag, WCHAR* filterMask,BOOL isResident = FALSE);

BOOL 
AddExcludeFileMaskToFilterRule(WCHAR* filterMask,  WCHAR* excludeFileFilterMask);

extern "C" __declspec(dllexport) 
BOOL 
AddExcludeProcessIdToFilterRule(WCHAR* filterMask, ULONG excludePID);

extern "C" __declspec(dllexport) 
BOOL 
AddIncludeProcessIdToFilterRule(WCHAR* filterMask, ULONG includePID);

extern "C" __declspec(dllexport) 
BOOL 
RegisterEventTypeToFilterRule(WCHAR* filterMask, ULONG eventType);

extern "C" __declspec(dllexport) 
BOOL 
AddIncludeProcessNameToFilterRule(WCHAR* filterMask,  WCHAR* processName);

extern "C" __declspec(dllexport) 
BOOL 
AddExcludeProcessNameToFilterRule(WCHAR* filterMask,  WCHAR* processName);

extern "C" __declspec(dllexport) 
BOOL 
AddIncludeUserNameToFilterRule(WCHAR* filterMask,  WCHAR* userName);

extern "C" __declspec(dllexport) 
BOOL 
AddExcludeUserNameToFilterRule(WCHAR* filterMask,  WCHAR* processName);

extern "C" __declspec(dllexport) 
BOOL 
RemoveFilterRule(WCHAR* FilterMask);

extern "C" __declspec(dllexport) 
BOOL 
AddExcludedProcessId(ULONG ProcessId);

extern "C" __declspec(dllexport) 
BOOL 
RemoveExcludeProcessId(ULONG ProcessId);

extern "C" __declspec(dllexport)
BOOL 
AddIncludedProcessId(ULONG processId);

extern "C" __declspec(dllexport) 
BOOL 
RemoveIncludeProcessId(ULONG processId);

#endif //FILTER_API_H
