// This is the main DLL file.


/* File: Win32Support.cpp
   Framework version:  .NET Framework version 3.5
   C++ compiler: Microsoft (R) C/C++ Optimizing Compiler Version 15.00.21022.08 for x64
   Creation date: July 21st, 2008
   Developer: Angel J. Hernández
   e-m@ail: angeljesus14@hotmail.com
   Blog: http://msmvps.com/blogs/angelhernandez
   Description: Win32Support Implementation
*/


#include "stdafx.h"

#include "Win32Support.h"

using namespace Win32Support;


SortedDictionary<int,String^>^ Utilities::EnumRunningProcesses() {
	TCHAR imagePath[MAX_PATH];
	BOOL successOnToken = FALSE;
	HANDLE hProcess = NULL, hToken = NULL;
	SortedDictionary<int, String^>^ retval = gcnew SortedDictionary<int, String^>();
	array<Process^>^ runningProcesses = Process::GetProcesses();

	// Get a token to enable SeDebugPrivilege
	if ((hToken = GetThreadToken()) != NULL) {
		SetPrivilege(hToken, SE_DEBUG_NAME, TRUE); 
		for (int nProcessIndex = 0; nProcessIndex < runningProcesses->Length; nProcessIndex++) {
			// Open process to collect information
			if((hProcess = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, 
				FALSE, runningProcesses[nProcessIndex]->Id)) != NULL) {

					//http://winprogger.com/?p=26  (Issue with Windows Vista => 64-Bit)
					if (WINVER>=0x0600) 
						GetProcessImageFileNameW(hProcess, imagePath, STACKARRAYSIZE(imagePath));
					else GetModuleFileNameEx(hProcess, NULL, imagePath, STACKARRAYSIZE(imagePath));

					// Add process information (Process ID and Process Name to Dictionary)
					retval->Add(runningProcesses[nProcessIndex]->Id,
						Path::GetFileName(gcnew String(imagePath))->ToUpper());

					CloseHandle(hProcess);
			}
		}
		SetPrivilege(hToken, SE_DEBUG_NAME, FALSE); //Disable SeDebugPrivilege
		CloseHandle(hToken);
	}
	return retval;
}

ProcessInformation^ Utilities::GetProcessInformation(int processId) {
	TCHAR imagePath[MAX_PATH];
	BOOL successOnToken = FALSE;
	HANDLE hProcess = NULL, hToken = NULL;
	ProcessInformation^ retval = gcnew ProcessInformation();

	if ((hToken = GetThreadToken()) != NULL) {
		SetPrivilege(hToken, SE_DEBUG_NAME, TRUE); 
		if((hProcess = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, 
			FALSE, processId)) != NULL) {
				// Get Executable path 
				if (WINVER>=0x0600) 
					GetProcessImageFileNameW(hProcess, imagePath, STACKARRAYSIZE(imagePath));
				else GetModuleFileNameEx(hProcess, NULL, imagePath, STACKARRAYSIZE(imagePath));

				// Set values to struct to be returned
				retval->ProcessID = processId;
				retval->ProcessName = Path::GetFileName(gcnew String(imagePath))->ToUpper();
				retval->ImagePath = (gcnew String(imagePath))->ToUpper();

				CloseHandle(hProcess);
		}
		SetPrivilege(hToken, SE_DEBUG_NAME, FALSE);
		CloseHandle(hToken);
	}
	return retval;
}


HANDLE Utilities::GetThreadToken() {
	HANDLE retval;
	int flag = TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY;

	if (!OpenThreadToken(GetCurrentThread(), flag, FALSE, &retval)) {
		if (GetLastError() == ERROR_NO_TOKEN)  {
			if (ImpersonateSelf(SecurityImpersonation) && 
				!OpenThreadToken(GetCurrentThread(), flag, FALSE, &retval)) 
				retval = NULL;
		}
	}
	return retval;
}

// http://support.microsoft.com/kb/q131065/
BOOL Utilities::SetPrivilege(HANDLE hToken, LPCTSTR Privilege, BOOL bEnablePrivilege) {
	LUID luid; 
	BOOL retval = FALSE;
	TOKEN_PRIVILEGES tp = {0}; 
	DWORD cb=sizeof(TOKEN_PRIVILEGES); 

	if(LookupPrivilegeValue(NULL, Privilege, &luid )) {
		tp.PrivilegeCount = 1; 
		tp.Privileges[0].Luid = luid;
		tp.Privileges[0].Attributes = bEnablePrivilege ? SE_PRIVILEGE_ENABLED : 0;
		AdjustTokenPrivileges( hToken, FALSE, &tp, cb, NULL, NULL ); 

		if (GetLastError() == ERROR_SUCCESS)
			retval = TRUE;
	}
	return retval;
}

String^ Utilities::GetProcessDetails(int processId) {
	return String::Empty;
}

// Heaputil Class
XmlDocument^ HeapUtil::GetHeapBasedOnProcessID(int processID, int blockAddress, StringBuilder^ heapBlock) {
	MemoryStream^ xmlMS;
	XmlDocumentFragment^ docFrag;
	XmlDocument^ retval = gcnew XmlDocument();

	// Should we select a specific memory block or not?
	if (blockAddress == 0) {
		xmlMS = gcnew MemoryStream(Text::ASCIIEncoding::Unicode->GetBytes(BASEXML)); 
		retval->Load(xmlMS);
		// Add node containing process information
		ProcessInformation^ info = Utilities::GetProcessInformation(processID);
		docFrag = retval->CreateDocumentFragment();
		docFrag->InnerXml = "<Process id='"+info->ProcessID.ToString()+"' imageName='"+info->ProcessName+"'></Process>";
		retval->ChildNodes[PROCESSNODEINDEX]->AppendChild(docFrag);
		// Add node containing details about the process
		docFrag->InnerXml = "<Details>"+Utilities::GetProcessDetails(processID)+"</Details>";
		retval->ChildNodes[PROCESSNODEINDEX]->ChildNodes[FIRSTCHILDNODEXINDEX]->AppendChild(docFrag);
		// Add nodes containing information about Heaps
		docFrag->InnerXml = PerformHeapWalk(processID, blockAddress, heapBlock);
		retval->ChildNodes[PROCESSNODEINDEX]->ChildNodes[FIRSTCHILDNODEXINDEX]->AppendChild(docFrag);
	} else {
		try {
			xmlMS = gcnew MemoryStream(Text::ASCIIEncoding::Unicode->GetBytes(PerformHeapWalk(processID,
				blockAddress, heapBlock)));
			retval->Load(xmlMS);
		} catch (Exception^ e) {
			EventLog^ eventVwr = gcnew EventLog();
			eventVwr->Source = "HeapWalker";
			eventVwr->WriteEntry("There's been an exception in 'HeapWalker' due to: " + e->Message,
				EventLogEntryType::Information);	
		}
	}
	return retval;
}

String^ HeapUtil::PerformHeapWalk(int processID, int blockAddress, StringBuilder^ heapBlock) {
	HANDLE hToken = NULL, hSnapshot = NULL;
	StringBuilder^ retval = gcnew StringBuilder();

	if ((hToken = Utilities::GetThreadToken()) != NULL) {
		Utilities::SetPrivilege(hToken, SE_DEBUG_NAME, TRUE); 
		if ((hSnapshot = CreateToolhelp32Snapshot(TH32CS_SNAPALL, processID)) != INVALID_HANDLE_VALUE) {
			PerformHeapWalkHelper(hSnapshot, retval, blockAddress, heapBlock);
			CloseHandle(hSnapshot);
		}

		Utilities::SetPrivilege(hToken, SE_DEBUG_NAME, FALSE); 
		CloseHandle(hToken);
	}
	// Return XML document depending on blockAddress argument
	return (blockAddress == 0 ? retval->ToString() : heapBlock->ToString());
}


void HeapUtil::PerformHeapWalkHelper(HANDLE& hSnapshot, StringBuilder^ xmlHeaps, int blockAddress,  StringBuilder^ heapBlock) {
	HEAPLIST32 heapList;
	HEAPENTRY32 heapEntry;
	bool retrieveMemoryBlock = false;
	int heapIndex = 1, blockIndex = 1;
	heapList.dwSize = sizeof(HEAPLIST32);
	heapEntry.dwSize = sizeof(HEAPENTRY32);

	if (Heap32ListFirst(hSnapshot, &heapList)) {
		do {
			xmlHeaps->Append("<Heap index='"+heapIndex.ToString()+"' baseAddress='"+ 
				String::Format("0x{0:x8}",heapList.th32HeapID) +
				"' blocks='{0}'"+ ">");
			if (Heap32First(&heapEntry, heapList.th32ProcessID, heapList.th32HeapID)) {
				do {
					xmlHeaps->Append("<Block index='"+blockIndex.ToString()+"' blockAddress='"+
						String::Format("0x{0:x8}",heapEntry.dwAddress) + "' "+
						"blockAddressAsDecimal='"+heapEntry.dwAddress.ToString() + "' "+
						"blockSize='"+String::Format("0x{0:x8}",heapEntry.dwBlockSize)+"' " +
						"blockSizeAsDecimal='"+heapEntry.dwBlockSize.ToString()+"'/>"); 

					// Should retrieve a given memory block?
					if (heapEntry.dwAddress == blockAddress) {
						retrieveMemoryBlock = true;
						RetrieveMemoryBlock(heapEntry, heapList, heapBlock);
					}
					blockIndex++;
				} while(!retrieveMemoryBlock && Heap32Next(&heapEntry));
			}
			xmlHeaps->Replace("{0}", (blockIndex - 1).ToString()); 
			xmlHeaps->Append("</Heap>");
			heapIndex++;
			blockIndex = 1;
		} while(!retrieveMemoryBlock && Heap32ListNext(hSnapshot, &heapList));
		// Should the generated XML contain heaps or a specific heap block information?
		if (retrieveMemoryBlock) 
			xmlHeaps = heapBlock;
	}
}

void HeapUtil::RetrieveMemoryBlock(HEAPENTRY32& heapEntry, HEAPLIST32& heapNode, StringBuilder^ heapBlock) {
	SIZE_T cbBytesRead;
	BYTE selectedByte;
	LPBYTE pBuffer = new BYTE[heapEntry.dwBlockSize];
	StringBuilder^ decValues = gcnew StringBuilder();
	StringBuilder^ hexValues = gcnew StringBuilder();

	if (Toolhelp32ReadProcessMemory(heapEntry.th32ProcessID, (LPCVOID) heapEntry.dwAddress, 
		pBuffer, heapEntry.dwBlockSize, &cbBytesRead)) {
			heapBlock->Append("<?xml version='1.0' encoding='UTF-16'?>");
			heapBlock->Append("<HeapWalker>");
			heapBlock->Append("<Block nodeAddress='"+String::Format("0x{0:x8}",heapNode.th32HeapID)+
				"' blockAddress='"+String::Format("0x{0:x8}",heapEntry.dwAddress)+
				"' size='"+String::Format("0x{0:x8}",heapEntry.dwSize) +"'>");
			for (int cbBlock = 0; cbBlock < heapEntry.dwBlockSize; cbBlock+= BYTESPERLINE) {
				heapBlock->Append("<Node id='"+String::Format("{0:x8}h",cbBlock)+"' "+
					"ContentAsHex='{0}' ContentAsDec='{1}'/>");

				for(int offset = 0; offset < BYTESPERLINE; offset++) {
					selectedByte = *(pBuffer + cbBlock + offset);
					hexValues->Append(String::Format("{0:x2}", selectedByte)->ToUpper() + " ");

					decValues->AppendFormat("{0} ",(selectedByte >= NONPRINTABLECHAR_1 &&
						selectedByte < NONPRINTABLECHAR_2 ? 
						Convert::ToString(Convert::ToChar(selectedByte)) : "."));  
				}
				// Add Hex & Dec values to XML Node
				heapBlock->Replace("{0}", hexValues->ToString());
				heapBlock->Replace("{1}", decValues->ToString());
				// Initialize Hex & Dec StringBuilders to reuse
				hexValues->Length = 0;
				decValues->Length = 0;
			}
			heapBlock->Append("</Block>");
			heapBlock->Append("</HeapWalker>");
	}
	delete[] pBuffer;
}