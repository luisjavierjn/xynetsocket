// ClientObj.h : Declaration of the CClientObj

#ifndef __CLIENTOBJ_H_
#define __CLIENTOBJ_H_

#include "resource.h"       // main symbols

/////////////////////////////////////////////////////////////////////////////
// CClientObj
class ATL_NO_VTABLE CClientObj : 
	public CComObjectRootEx<CComSingleThreadModel>,
	public CComCoClass<CClientObj, &CLSID_ClientObj>,
	public IDispatchImpl<IClientObj, &IID_IClientObj, &LIBID_XYNETCOMCLIENTLib>
{
	friend void WorkerProc(void* pParam);

	BOOL m_bReconnect;
	long m_nReadTimeout;
	long m_nMaxDataSize;
	long m_nSize;
	BOOL m_bIsBinary;
	BYTE* m_pData;
	BSTR m_sError;
	long m_nErrorCode;
	BSTR m_sRemoteAddress;
	long m_nRemotePort;
	SOCKET m_socket;
	BOOL SendRawData(BYTE *pData, long nSize);

public:
	CClientObj()
	{
		m_bReconnect = TRUE;
		m_nReadTimeout = 30;
		m_nMaxDataSize = 4*1024*1024;
		m_pData = NULL;
		m_nSize = 0;
		m_bIsBinary = FALSE;
		m_sError = NULL;
		m_nErrorCode = 0;
		m_sRemoteAddress = NULL;
		m_nRemotePort = 0;
		m_socket = INVALID_SOCKET;
	}
	~CClientObj()
	{
		Reset();
		if(m_sRemoteAddress!=NULL) ::SysFreeString(m_sRemoteAddress);
		if(m_sError!=NULL) ::SysFreeString(m_sError);
		if(m_pData!=NULL) delete []m_pData;
	}

DECLARE_REGISTRY_RESOURCEID(IDR_CLIENTOBJ)

DECLARE_PROTECT_FINAL_CONSTRUCT()

BEGIN_COM_MAP(CClientObj)
	COM_INTERFACE_ENTRY(IClientObj)
	COM_INTERFACE_ENTRY(IDispatch)
END_COM_MAP()

// IClientObj
public:
	STDMETHOD(SetMaxDataSize)(/*[in]*/ long nMaxDataSize);
	STDMETHOD(SetReadTimeout)(/*[in]*/ long nReadTimeout);
	STDMETHOD(GetLastErrorCode)(/*[out, retval]*/ long* pOutput);
	STDMETHOD(GetBinaryData)(/*[out]*/ long* pSize, /*[out, retval]*/ long* pData);
	STDMETHOD(GetStringData)(/*[out, retval]*/ BSTR* pOutput);
	STDMETHOD(GetLastError)(/*[out, retval]*/ BSTR* pOutput);
	STDMETHOD(ReceiveData)(/*[out]*/ long* pSize, /*[out]*/ BOOL* pIsBinary, /*[out, retval]*/ BOOL* pOutput);
	STDMETHOD(SendStringData)(/*[in]*/ BSTR sData, /*[out, retval]*/ BOOL* pOutput);
	STDMETHOD(SendBinaryData)(/*[in]*/ BYTE* pData, /*[in]*/ long nSize, /*[out, retval]*/ BOOL* pOutput);
	STDMETHOD(Reset)();
	STDMETHOD(Connect)(/*[in]*/ BSTR sRemoteAddress, /*[in]*/ long nRemotePort, /*[out, reval]*/ BOOL* pOutput);
};

#endif //__CLIENTOBJ_H_
