// ClientObj.cpp : Implementation of CClientObj
#include "stdafx.h"
#include "XYNetComClient.h"
#include "ClientObj.h"
#include <process.h>

CRITICAL_SECTION cs;

//void WCharToChar(char* pTarget, const unsigned short* pSource)
void WCharToChar(char* pTarget, BSTR pSource)
{
	while(*pSource)
	{
		*pTarget = (char)(*pSource);
		pSource++;
		pTarget++;
	}
	*pTarget = 0;
}

//void StringToBinary(BYTE* pTarget, const unsigned short* pSource, int nSize)
void StringToBinary(BYTE* pTarget, BSTR pSource, int nSize)
{
	for(int i=0;i<nSize;i++)
	{
		pTarget[2*i] = (pSource[i]&0x000000FF)%256;
		pTarget[2*i+1] = (pSource[i]&0x0000FF00)/256;
	}
}

//void BinaryToString(unsigned short* pTarget, BYTE* pSource, int nSize)
void BinaryToString(WCHAR* pTarget, BYTE* pSource, int nSize)
{
	for(int i=0;i<nSize;i++)
	{
		pTarget[i] = (pSource[2*i]&0x000000FF)+(pSource[2*i+1]&0x000000FF)*256;
	}
}

/////////////////////////////////////////////////////////////////////////////
// CClientObj


STDMETHODIMP CClientObj::Connect(BSTR sRemoteAddress, long nRemotePort, BOOL *pOutput)
{
	if(m_sError!=NULL)
	{
		::SysFreeString(m_sError);
		m_sError = NULL;
		m_nErrorCode = 0;
	}
	::EnterCriticalSection(&cs);
	static long* pThreads= NULL;
	const int nBufferSize = 64*1024;
	if(pThreads==NULL) 
	{
		pThreads = new long[nBufferSize];
		::memset(pThreads, 0, nBufferSize*sizeof(long));
	}
	long nInitIndex = -1;
	for(int i=0;i<nBufferSize;i++)
	{
		if(pThreads[i]==0)
		{
			nInitIndex = i;
			break;
		}
		else if(pThreads[i]==long(::GetCurrentThreadId())) break;
	}
	if(nInitIndex>=0)
	{
		pThreads[nInitIndex] = ::GetCurrentThreadId();
		WORD wVersionRequested = MAKEWORD(2, 0);
		WSADATA wsaData;
		if(::WSAStartup(wVersionRequested,&wsaData)!=0)
		{
			m_sError = ::SysAllocString(L"Failed to call WSAStartup");
			m_nErrorCode = ::GetLastError();
			::LeaveCriticalSection(&cs);
			*pOutput = FALSE;
			return S_OK;
		}
		if(LOBYTE(wsaData.wVersion)<2)
		{
			m_sError = ::SysAllocString(L"Invalid winsock version");
			m_nErrorCode = ::GetLastError();
			*pOutput = FALSE;
			::LeaveCriticalSection(&cs);
			return S_OK;
		}
	}
	::LeaveCriticalSection(&cs);
	Reset();
	if(sRemoteAddress!=NULL)
	{
		if(m_sRemoteAddress!=NULL) ::SysFreeString(m_sRemoteAddress);
		m_sRemoteAddress = ::SysAllocString(sRemoteAddress);
	}
	if(nRemotePort>0) m_nRemotePort = nRemotePort;
	m_socket = ::socket(AF_INET,SOCK_STREAM,0);
	if(m_socket==INVALID_SOCKET)
	{
		m_sError = ::SysAllocString(L"Failed to create socket");
		m_nErrorCode = ::GetLastError();
		*pOutput = FALSE;
		return S_OK;
	}
	else
	{
		int nLen = wcslen(m_sRemoteAddress);
		char* pServerAddress;
		if(nLen>0) 
		{
			pServerAddress = new char[nLen+1];
			WCharToChar(pServerAddress,m_sRemoteAddress);
		}
		else
		{
			nLen = MAX_COMPUTERNAME_LENGTH+1;
			pServerAddress = new char[nLen];
			::GetComputerNameA(pServerAddress,(DWORD*)&nLen);
		}
		SOCKADDR_IN sockAddr;
		memset(&sockAddr,0,sizeof(sockAddr));
		sockAddr.sin_family = AF_INET;
		DWORD lResult = inet_addr(pServerAddress);
		if(lResult==INADDR_NONE)
		{
			LPHOSTENT lphost;
			lphost = gethostbyname(pServerAddress);
			if(lphost!=NULL)
			{
				sockAddr.sin_addr.s_addr = ((LPIN_ADDR)lphost->h_addr)->s_addr;
			}
			else
			{
				m_sError = ::SysAllocString(L"Failed to get host name");
				m_nErrorCode = ::GetLastError();
				delete []pServerAddress;
				*pOutput = FALSE;
				return S_OK;
			}		
		}
		else
		{
			sockAddr.sin_addr.s_addr = lResult;
		}
		delete []pServerAddress;
		sockAddr.sin_port = htons((u_short)m_nRemotePort);
		if(::connect(m_socket,(SOCKADDR*)&sockAddr,sizeof(sockAddr))==SOCKET_ERROR)
		{
			m_sError = ::SysAllocString(L"Failed to connect to server");
			m_nErrorCode = ::GetLastError();
			*pOutput = FALSE;
			return S_OK;
		}
	}
	*pOutput = TRUE;
	return S_OK;
}

STDMETHODIMP CClientObj::Reset()
{
	m_bReconnect = FALSE;
	if(m_socket!=INVALID_SOCKET)
	{
		BYTE pData[4] = {255, 0, 0, 0};
		SendRawData(pData, 4);
		::closesocket(m_socket);
		m_socket = INVALID_SOCKET;
	}
	m_bReconnect = TRUE;
	return S_OK;
}

BOOL CClientObj::SendRawData(BYTE *pData, long nSize)
{
	if(m_sError!=NULL)
	{
		::SysFreeString(m_sError);
		m_sError = NULL;
		m_nErrorCode = 0;
	}
	if(::send(m_socket,(char*)pData,nSize,0)==SOCKET_ERROR)
	{
		long nErrorCode = ::GetLastError();
		BOOL bRet = FALSE;
		if(m_bReconnect) Connect(NULL, 0, &bRet);
		if(m_sError!=NULL) ::SysFreeString(m_sError);
		m_sError = ::SysAllocString(L"Failed to send data");
		m_nErrorCode = nErrorCode;
		return FALSE;
	}	
	else return TRUE;
}

STDMETHODIMP CClientObj::SendBinaryData(BYTE *pData, long nSize, BOOL *pOutput)
{
	BYTE* pData2 = new BYTE[nSize+4];
	::memcpy(pData2, pData, nSize);
	pData2[0] = 1+(nSize/16777216)*16;
	pData2[1] = (BYTE)(nSize%256);
	pData2[2] = (BYTE)((nSize%65536)/256);
	pData2[3] = (BYTE)(nSize/65536);
	*pOutput = SendRawData(pData2, nSize+4);
	delete []pData2;
	return S_OK;
}

STDMETHODIMP CClientObj::SendStringData(BSTR sData, BOOL *pOutput)
{
	int nLen = ::wcslen(sData);
	BYTE* pData = new BYTE[2*nLen+4];
	StringToBinary(pData+4, sData, nLen);
	pData[0] = ((2*nLen)/16777216)*16;
	pData[1] = (BYTE)((2*nLen)%256);
	pData[2] = (BYTE)(((2*nLen)%65536)/256);
	pData[3] = (BYTE)((2*nLen)/65536);
	*pOutput = SendRawData(pData, 2*nLen+4);
	delete []pData;
	return S_OK;
}

struct ReceiveStruct
{
	BOOL* pOutput;
	CClientObj* pClient;
	BOOL bDone;
};

void WorkerProc(void* pParam)
{
	struct ReceiveStruct* pInput = (ReceiveStruct*)pParam;
	if(pInput->pClient->m_pData!=NULL)
	{
		delete [](pInput->pClient->m_pData);
		(pInput->pClient->m_pData) = NULL;
	}
	BYTE pHeader[4];
	int nTotal = 0;
	while(true)
	{
		int nRead = ::recv(pInput->pClient->m_socket, (char*)(pHeader+nTotal), 4-nTotal, 0);
		if(nRead==SOCKET_ERROR)
		{
			pInput->pClient->m_sError = ::SysAllocString(L"Failed to call recv");
			pInput->pClient->m_nErrorCode = ::GetLastError();
			*(pInput->pOutput) = FALSE;
			pInput->bDone = TRUE;
			return;
		}
		nTotal += nRead;
		if(nTotal==4) 
		{
			if(pHeader[0]==2) nTotal = 0;
			else break;
		}
		::Sleep(50);
	}
	if(pHeader[0]%16>1)
	{
		pInput->pClient->m_sError = ::SysAllocString(L"Invalid data type byte");
		*(pInput->pOutput) = FALSE;
		pInput->bDone = TRUE;
		return;
	}
	pInput->pClient->m_bIsBinary = ((pHeader[0]%16)==1);
	pInput->pClient->m_nSize = (pHeader[1]&0x000000FF)+(pHeader[2]&0x000000FF)*256+(pHeader[3]&0x000000FF)*65536+((pHeader[0]&0x000000FF)/16)*16777216;
	if((pInput->pClient->m_nSize)>(pInput->pClient->m_nMaxDataSize))
	{
		pInput->pClient->m_nSize = 0;
		pInput->pClient->m_sError = ::SysAllocString(L"Data size too large");
		*(pInput->pOutput) = FALSE;
		pInput->bDone = TRUE;
		return;
	}
	if(pInput->pClient->m_bIsBinary==FALSE&&((pInput->pClient->m_nSize)%2)!=0)
	{
		pInput->pClient->m_nSize = 0;
		pInput->pClient->m_sError = ::SysAllocString(L"Invalid string data size");
		*(pInput->pOutput) = FALSE;
		pInput->bDone = TRUE;
		return;
	}
	pInput->pClient->m_pData = new BYTE[pInput->pClient->m_nSize];
	nTotal = 0;
	while(true)
	{
		int nRead = ::recv((pInput->pClient->m_socket), (char*)(pInput->pClient->m_pData+nTotal), (pInput->pClient->m_nSize)-nTotal, 0);
		if(nRead==SOCKET_ERROR)
		{
			pInput->pClient->m_nSize = 0;
			pInput->pClient->m_sError = ::SysAllocString(L"Failed to call recv");
			pInput->pClient->m_nErrorCode = ::GetLastError();
			*(pInput->pOutput) = FALSE;
			pInput->bDone = TRUE;
			return;
		}
		nTotal += nRead;
		if(nTotal==pInput->pClient->m_nSize) break;
		::Sleep(50);
	}
	*(pInput->pOutput) = TRUE;
	pInput->bDone = TRUE;
	return;
}

STDMETHODIMP CClientObj::ReceiveData(long *pSize, BOOL *pIsBinary, BOOL *pOutput)
{
	if(m_sError!=NULL)
	{
		::SysFreeString(m_sError);
		m_sError = NULL;
		m_nErrorCode = 0;
	}
	long hThread = -1;
	try
	{
		struct ReceiveStruct input = {pOutput, this, FALSE};
		hThread = _beginthread(WorkerProc, 0, &input);
		if(hThread==-1)
		{
			m_sError = ::SysAllocString(L"Failed to create thread");
			*pOutput = FALSE;
		}
		else
		{
			long nStart = ::GetTickCount();
			while(input.bDone==FALSE)
			{
				::Sleep(50);
				if(long(::GetTickCount()-nStart)>m_nReadTimeout*1000)
				{
					::TerminateThread((HANDLE)hThread, 1);
					hThread = -1;
					if(m_sError!=NULL) ::SysFreeString(m_sError);
					m_sError = ::SysAllocString(L"Timeout while receiving incoming data");
					break;
				}
			}
			if(input.bDone==FALSE) *pOutput = FALSE;
			else if(*pOutput) 
			{
				*pSize = m_nSize;
				*pIsBinary = m_bIsBinary;
			}
		}
	}
	catch(...)
	{
		if(hThread!=-1) ::TerminateThread((HANDLE)hThread, 2);
		if(m_sError!=NULL) ::SysFreeString(m_sError);
		m_sError = ::SysAllocString(L"Unexpected exception while receiving incoming data");
		m_nErrorCode = ::GetLastError();
		*pOutput = FALSE;
	}
	if(*pOutput==FALSE)
	{
		long nErrorCode = m_nErrorCode;
		BSTR sError = m_sError;
		m_sError = NULL;
		BOOL bRet = FALSE;
		if(m_bReconnect) Connect(NULL, 0, &bRet);
		if(m_sError!=NULL) ::SysFreeString(m_sError);
		m_sError = sError;
		m_nErrorCode = nErrorCode;
	}
	return S_OK;
}

STDMETHODIMP CClientObj::GetLastError(BSTR *pOutput)
{
	if(m_sError==NULL) *pOutput = ::SysAllocString(L"");
	else *pOutput = m_sError;
	return S_OK;
}

STDMETHODIMP CClientObj::GetStringData(BSTR *pOutput)
{
	if(m_bIsBinary||(m_nSize%2!=0)||m_pData==NULL) return S_FALSE;
	WCHAR* pData = new WCHAR[(m_nSize/2)+1];
	BinaryToString(pData, m_pData, m_nSize/2);
	pData[m_nSize/2] = 0;
	*pOutput = ::SysAllocString(pData);
	delete []pData;
	return S_OK;
}

STDMETHODIMP CClientObj::GetBinaryData(long* pSize, long* pData)
{
	*pSize = m_nSize;
	*pData = long(m_pData);
	return S_OK;
}

STDMETHODIMP CClientObj::GetLastErrorCode(long *pOutput)
{
	*pOutput = m_nErrorCode;
	return S_OK;
}

STDMETHODIMP CClientObj::SetReadTimeout(long nReadTimeout)
{
	if(nReadTimeout>=5&&nReadTimeout<=120) m_nReadTimeout = nReadTimeout;
	return S_OK;
}

STDMETHODIMP CClientObj::SetMaxDataSize(long nMaxDataSize)
{
	if(nMaxDataSize>=1024) m_nMaxDataSize = nMaxDataSize;
	return S_OK;
}
