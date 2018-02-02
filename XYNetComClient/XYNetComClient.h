

/* this ALWAYS GENERATED file contains the definitions for the interfaces */


 /* File created by MIDL compiler version 7.00.0500 */
/* at Tue Sep 17 22:30:51 2013
 */
/* Compiler settings for .\XYNetComClient.idl:
    Oicf, W1, Zp8, env=Win32 (32b run)
    protocol : dce , ms_ext, c_ext, robust
    error checks: allocation ref bounds_check enum stub_data 
    VC __declspec() decoration level: 
         __declspec(uuid()), __declspec(selectany), __declspec(novtable)
         DECLSPEC_UUID(), MIDL_INTERFACE()
*/
//@@MIDL_FILE_HEADING(  )

#pragma warning( disable: 4049 )  /* more than 64k source lines */


/* verify that the <rpcndr.h> version is high enough to compile this file*/
#ifndef __REQUIRED_RPCNDR_H_VERSION__
#define __REQUIRED_RPCNDR_H_VERSION__ 475
#endif

#include "rpc.h"
#include "rpcndr.h"

#ifndef __RPCNDR_H_VERSION__
#error this stub requires an updated version of <rpcndr.h>
#endif // __RPCNDR_H_VERSION__

#ifndef COM_NO_WINDOWS_H
#include "windows.h"
#include "ole2.h"
#endif /*COM_NO_WINDOWS_H*/

#ifndef __XYNetComClient_h__
#define __XYNetComClient_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

/* Forward Declarations */ 

#ifndef __IClientObj_FWD_DEFINED__
#define __IClientObj_FWD_DEFINED__
typedef interface IClientObj IClientObj;
#endif 	/* __IClientObj_FWD_DEFINED__ */


#ifndef __ClientObj_FWD_DEFINED__
#define __ClientObj_FWD_DEFINED__

#ifdef __cplusplus
typedef class ClientObj ClientObj;
#else
typedef struct ClientObj ClientObj;
#endif /* __cplusplus */

#endif 	/* __ClientObj_FWD_DEFINED__ */


/* header files for imported files */
#include "oaidl.h"
#include "ocidl.h"

#ifdef __cplusplus
extern "C"{
#endif 


#ifndef __IClientObj_INTERFACE_DEFINED__
#define __IClientObj_INTERFACE_DEFINED__

/* interface IClientObj */
/* [unique][helpstring][dual][uuid][object] */ 


EXTERN_C const IID IID_IClientObj;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("B1F4A34C-524B-4FD0-B8FE-7A793EB307B0")
    IClientObj : public IDispatch
    {
    public:
        virtual /* [helpstring][id] */ HRESULT STDMETHODCALLTYPE Connect( 
            /* [in] */ BSTR sRemoteAddress,
            /* [in] */ long nRemotePort,
            /* [retval][out] */ BOOL *pOutput) = 0;
        
        virtual /* [helpstring][id] */ HRESULT STDMETHODCALLTYPE Reset( void) = 0;
        
        virtual /* [helpstring][id] */ HRESULT STDMETHODCALLTYPE SendBinaryData( 
            /* [in] */ BYTE *pData,
            /* [in] */ long nSize,
            /* [retval][out] */ BOOL *pOutput) = 0;
        
        virtual /* [helpstring][id] */ HRESULT STDMETHODCALLTYPE SendStringData( 
            /* [in] */ BSTR sData,
            /* [retval][out] */ BOOL *pOutput) = 0;
        
        virtual /* [helpstring][id] */ HRESULT STDMETHODCALLTYPE ReceiveData( 
            /* [out] */ long *pSize,
            /* [out] */ BOOL *pIsBinary,
            /* [retval][out] */ BOOL *pOutput) = 0;
        
        virtual /* [helpstring][id] */ HRESULT STDMETHODCALLTYPE GetLastError( 
            /* [retval][out] */ BSTR *pOutput) = 0;
        
        virtual /* [helpstring][id] */ HRESULT STDMETHODCALLTYPE GetStringData( 
            /* [retval][out] */ BSTR *pOutput) = 0;
        
        virtual /* [helpstring][id] */ HRESULT STDMETHODCALLTYPE GetBinaryData( 
            /* [out] */ long *pSize,
            /* [retval][out] */ long *pData) = 0;
        
        virtual /* [helpstring][id] */ HRESULT STDMETHODCALLTYPE GetLastErrorCode( 
            /* [retval][out] */ long *pOutput) = 0;
        
        virtual /* [helpstring][id] */ HRESULT STDMETHODCALLTYPE SetReadTimeout( 
            /* [in] */ long nReadTimeout) = 0;
        
        virtual /* [helpstring][id] */ HRESULT STDMETHODCALLTYPE SetMaxDataSize( 
            /* [in] */ long nMaxDataSize) = 0;
        
    };
    
#else 	/* C style interface */

    typedef struct IClientObjVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            IClientObj * This,
            /* [in] */ REFIID riid,
            /* [iid_is][out] */ 
            __RPC__deref_out  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            IClientObj * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            IClientObj * This);
        
        HRESULT ( STDMETHODCALLTYPE *GetTypeInfoCount )( 
            IClientObj * This,
            /* [out] */ UINT *pctinfo);
        
        HRESULT ( STDMETHODCALLTYPE *GetTypeInfo )( 
            IClientObj * This,
            /* [in] */ UINT iTInfo,
            /* [in] */ LCID lcid,
            /* [out] */ ITypeInfo **ppTInfo);
        
        HRESULT ( STDMETHODCALLTYPE *GetIDsOfNames )( 
            IClientObj * This,
            /* [in] */ REFIID riid,
            /* [size_is][in] */ LPOLESTR *rgszNames,
            /* [range][in] */ UINT cNames,
            /* [in] */ LCID lcid,
            /* [size_is][out] */ DISPID *rgDispId);
        
        /* [local] */ HRESULT ( STDMETHODCALLTYPE *Invoke )( 
            IClientObj * This,
            /* [in] */ DISPID dispIdMember,
            /* [in] */ REFIID riid,
            /* [in] */ LCID lcid,
            /* [in] */ WORD wFlags,
            /* [out][in] */ DISPPARAMS *pDispParams,
            /* [out] */ VARIANT *pVarResult,
            /* [out] */ EXCEPINFO *pExcepInfo,
            /* [out] */ UINT *puArgErr);
        
        /* [helpstring][id] */ HRESULT ( STDMETHODCALLTYPE *Connect )( 
            IClientObj * This,
            /* [in] */ BSTR sRemoteAddress,
            /* [in] */ long nRemotePort,
            /* [retval][out] */ BOOL *pOutput);
        
        /* [helpstring][id] */ HRESULT ( STDMETHODCALLTYPE *Reset )( 
            IClientObj * This);
        
        /* [helpstring][id] */ HRESULT ( STDMETHODCALLTYPE *SendBinaryData )( 
            IClientObj * This,
            /* [in] */ BYTE *pData,
            /* [in] */ long nSize,
            /* [retval][out] */ BOOL *pOutput);
        
        /* [helpstring][id] */ HRESULT ( STDMETHODCALLTYPE *SendStringData )( 
            IClientObj * This,
            /* [in] */ BSTR sData,
            /* [retval][out] */ BOOL *pOutput);
        
        /* [helpstring][id] */ HRESULT ( STDMETHODCALLTYPE *ReceiveData )( 
            IClientObj * This,
            /* [out] */ long *pSize,
            /* [out] */ BOOL *pIsBinary,
            /* [retval][out] */ BOOL *pOutput);
        
        /* [helpstring][id] */ HRESULT ( STDMETHODCALLTYPE *GetLastError )( 
            IClientObj * This,
            /* [retval][out] */ BSTR *pOutput);
        
        /* [helpstring][id] */ HRESULT ( STDMETHODCALLTYPE *GetStringData )( 
            IClientObj * This,
            /* [retval][out] */ BSTR *pOutput);
        
        /* [helpstring][id] */ HRESULT ( STDMETHODCALLTYPE *GetBinaryData )( 
            IClientObj * This,
            /* [out] */ long *pSize,
            /* [retval][out] */ long *pData);
        
        /* [helpstring][id] */ HRESULT ( STDMETHODCALLTYPE *GetLastErrorCode )( 
            IClientObj * This,
            /* [retval][out] */ long *pOutput);
        
        /* [helpstring][id] */ HRESULT ( STDMETHODCALLTYPE *SetReadTimeout )( 
            IClientObj * This,
            /* [in] */ long nReadTimeout);
        
        /* [helpstring][id] */ HRESULT ( STDMETHODCALLTYPE *SetMaxDataSize )( 
            IClientObj * This,
            /* [in] */ long nMaxDataSize);
        
        END_INTERFACE
    } IClientObjVtbl;

    interface IClientObj
    {
        CONST_VTBL struct IClientObjVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define IClientObj_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define IClientObj_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define IClientObj_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#define IClientObj_GetTypeInfoCount(This,pctinfo)	\
    ( (This)->lpVtbl -> GetTypeInfoCount(This,pctinfo) ) 

#define IClientObj_GetTypeInfo(This,iTInfo,lcid,ppTInfo)	\
    ( (This)->lpVtbl -> GetTypeInfo(This,iTInfo,lcid,ppTInfo) ) 

#define IClientObj_GetIDsOfNames(This,riid,rgszNames,cNames,lcid,rgDispId)	\
    ( (This)->lpVtbl -> GetIDsOfNames(This,riid,rgszNames,cNames,lcid,rgDispId) ) 

#define IClientObj_Invoke(This,dispIdMember,riid,lcid,wFlags,pDispParams,pVarResult,pExcepInfo,puArgErr)	\
    ( (This)->lpVtbl -> Invoke(This,dispIdMember,riid,lcid,wFlags,pDispParams,pVarResult,pExcepInfo,puArgErr) ) 


#define IClientObj_Connect(This,sRemoteAddress,nRemotePort,pOutput)	\
    ( (This)->lpVtbl -> Connect(This,sRemoteAddress,nRemotePort,pOutput) ) 

#define IClientObj_Reset(This)	\
    ( (This)->lpVtbl -> Reset(This) ) 

#define IClientObj_SendBinaryData(This,pData,nSize,pOutput)	\
    ( (This)->lpVtbl -> SendBinaryData(This,pData,nSize,pOutput) ) 

#define IClientObj_SendStringData(This,sData,pOutput)	\
    ( (This)->lpVtbl -> SendStringData(This,sData,pOutput) ) 

#define IClientObj_ReceiveData(This,pSize,pIsBinary,pOutput)	\
    ( (This)->lpVtbl -> ReceiveData(This,pSize,pIsBinary,pOutput) ) 

#define IClientObj_GetLastError(This,pOutput)	\
    ( (This)->lpVtbl -> GetLastError(This,pOutput) ) 

#define IClientObj_GetStringData(This,pOutput)	\
    ( (This)->lpVtbl -> GetStringData(This,pOutput) ) 

#define IClientObj_GetBinaryData(This,pSize,pData)	\
    ( (This)->lpVtbl -> GetBinaryData(This,pSize,pData) ) 

#define IClientObj_GetLastErrorCode(This,pOutput)	\
    ( (This)->lpVtbl -> GetLastErrorCode(This,pOutput) ) 

#define IClientObj_SetReadTimeout(This,nReadTimeout)	\
    ( (This)->lpVtbl -> SetReadTimeout(This,nReadTimeout) ) 

#define IClientObj_SetMaxDataSize(This,nMaxDataSize)	\
    ( (This)->lpVtbl -> SetMaxDataSize(This,nMaxDataSize) ) 

#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __IClientObj_INTERFACE_DEFINED__ */



#ifndef __XYNETCOMCLIENTLib_LIBRARY_DEFINED__
#define __XYNETCOMCLIENTLib_LIBRARY_DEFINED__

/* library XYNETCOMCLIENTLib */
/* [helpstring][version][uuid] */ 


EXTERN_C const IID LIBID_XYNETCOMCLIENTLib;

EXTERN_C const CLSID CLSID_ClientObj;

#ifdef __cplusplus

class DECLSPEC_UUID("78EDFBDF-196A-4158-A091-01E068B77F58")
ClientObj;
#endif
#endif /* __XYNETCOMCLIENTLib_LIBRARY_DEFINED__ */

/* Additional Prototypes for ALL interfaces */

unsigned long             __RPC_USER  BSTR_UserSize(     unsigned long *, unsigned long            , BSTR * ); 
unsigned char * __RPC_USER  BSTR_UserMarshal(  unsigned long *, unsigned char *, BSTR * ); 
unsigned char * __RPC_USER  BSTR_UserUnmarshal(unsigned long *, unsigned char *, BSTR * ); 
void                      __RPC_USER  BSTR_UserFree(     unsigned long *, BSTR * ); 

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif


