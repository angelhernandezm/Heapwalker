/* File: Win32Support.h
   Framework version:  .NET Framework version 3.5
   C++ compiler: Microsoft (R) C/C++ Optimizing Compiler Version 15.00.21022.08 for x64
   Creation date: July 21st, 2008
   Developer: Angel J. Hernández
   e-m@ail: angeljesus14@hotmail.com
   Blog: http://msmvps.com/blogs/angelhernandez
   Description: Include file for Win32Support
*/

#pragma once

#include <stdio.h>
#include <stdlib.h>
#include <Windows.h>
#include <Psapi.h>
#include <Tlhelp32.h>

using namespace System;
using namespace System::IO;
using namespace System::Text;
using namespace System::Data;
using namespace System::Xml;
using namespace System::Diagnostics;
using namespace System::Collections;
using namespace System::Collections::Generic;
using namespace System::Runtime;
using namespace System::Runtime::InteropServices;


// Defines
#define PROCESSNODEINDEX 1
#define FIRSTCHILDNODEXINDEX 0
#define BYTESPERLINE 16
#define NONPRINTABLECHAR_1 32
#define NONPRINTABLECHAR_2 127
#define STACKARRAYSIZE(myArray) (sizeof(myArray)/sizeof(myArray[0]))
#define BASEXML "<?xml version='1.0' encoding='UTF-16'?><HeapWalker></HeapWalker>"

namespace Win32Support {
	// Structs
	public ref struct ProcessInformation {
		int ProcessID;
		String^ ProcessName;
		String^ ImagePath;
	};

	// Classes
	public ref class Utilities {
	public:
		static SortedDictionary<int,String^>^ EnumRunningProcesses();
		static ProcessInformation^ GetProcessInformation(int processId);
		static String^ GetProcessDetails(int processId);
		static HANDLE GetThreadToken();
		static BOOL SetPrivilege(HANDLE hToken, LPCTSTR Privilege, BOOL bEnablePrivilege);

	private:

	};

	public ref class HeapUtil	{
	public:
		static XmlDocument^ GetHeapBasedOnProcessID(int processID, int blockAddress, StringBuilder^ heapBlock);

	private:
		static String^ PerformHeapWalk(int processID, int blockAddress, StringBuilder^ heapBlock); 
		static void RetrieveMemoryBlock(HEAPENTRY32& heapEntry, HEAPLIST32& heapNode, StringBuilder^ heapBlock);
		static void PerformHeapWalkHelper(HANDLE& hSnapshot, StringBuilder^ xmlHeaps, int blockAddress, StringBuilder^ heapBlock);
	};
}