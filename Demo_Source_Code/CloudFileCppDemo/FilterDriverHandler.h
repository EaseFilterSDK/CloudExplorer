///////////////////////////////////////////////////////////////////////////////
//
//    (C) Copyright 2011 EaseClouds Technologies Inc.
//    All Rights Reserved
//
//    This software is part of a licensed software product and may
//    only be used or copied in accordance with the terms of that license.
//
///////////////////////////////////////////////////////////////////////////////

#pragma once

// Defines for NTSTATUS
typedef long NTSTATUS;

VOID
CreateTestFiles();

BOOL
GetFileList(
	IN		PMESSAGE_SEND_DATA pSendMessage,
	IN OUT	PMESSAGE_REPLY_DATA pReplyMessage);

void 
SendConfigSettingsToFilter();

BOOL
GetRequestedBlockData(
	IN		PMESSAGE_SEND_DATA pSendMessage,
	IN OUT	PMESSAGE_REPLY_DATA pReplyMessage);


BOOL
DownloadCacheFile(  
	IN		PMESSAGE_SEND_DATA pSendMessage,
	IN OUT	PMESSAGE_REPLY_DATA pReplyMessage,
	IN		BOOL toOriginalFolder );

