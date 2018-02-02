import java.net.*;
import java.io.*;
//import java.util.*;

public class XYNetJavaClient implements Runnable
{
	Exception m_except;
	Socket m_sock;
	boolean m_bReconnect;
	int m_nReadTimeout;
	int m_nMaxDataSize;
	String m_sRemoteAddress;
	int m_nRemotePort;
	byte[] m_pData;
	boolean m_bIsBinary;
	Thread m_threadRun;
	boolean SendRawData(byte[] pData)
	{
		m_except = null;
		try
		{
			m_sock.getOutputStream().write(pData);
			return true;
		}
		catch(Exception oBug)
		{
			Exception oExcept = oBug;
			if(m_bReconnect) Connect(null, 0);
			m_except = oExcept;
			return false;
		}
	}
	final public void run()
	{
		try
		{
			m_pData = null;
			byte[] pHeader = new byte[4];
			int nTotal = 0;
			while(true)
			{
				int nRead = m_sock.getInputStream().read(pHeader, nTotal, 4-nTotal);
				if(nRead>0) nTotal += nRead;
				if(nTotal==4) 
				{
					if((pHeader[0]&0x0000000F)==2) nTotal = 0;
					else break;
				}
				if(nRead<0) throw new Exception("Failed to read incoming data");
				Thread.currentThread().sleep(50);
			}
			if((pHeader[0]&0x0000000F)>1) throw new Exception("Invalid data type byte: "+(pHeader[0]&0x0000000F));
			m_bIsBinary = ((pHeader[0]&0x0000000F)==1);
			int nSize = (pHeader[1]&0x000000FF)+(pHeader[2]&0x000000FF)*256+(pHeader[3]&0x000000FF)*65536+((pHeader[0]&0x000000FF)/16)*16777216;
			if(nSize>m_nMaxDataSize) throw new Exception("Data size too large");
			if(m_bIsBinary==false&&(nSize%2)!=0) throw new Exception("Invalid string data size");
			m_pData = new byte[nSize];
			nTotal = 0;
			while(nSize>0)
			{
				int nRead = m_sock.getInputStream().read(m_pData, nTotal, nSize-nTotal);
				if(nRead>0) nTotal += nRead;
				if(nTotal==nSize) break;
				if(nRead<0) throw new Exception("Failed to read incoming data");
				Thread.currentThread().sleep(50);
			}
		}
		catch(Exception oBug)
		{
			m_except = oBug;
			m_pData = null;	
		}
		m_threadRun = null;	
	}
	public XYNetJavaClient()
	{
		m_except = null;
		m_sock = null;
		m_bReconnect = true;
		m_nReadTimeout = 30;
		m_nMaxDataSize = 4*1024*1024;
		m_sRemoteAddress = "";
		m_nRemotePort = 0;
		m_pData = null;
		m_bIsBinary = true;
	}
	public boolean Connect(String sRemoteAddress, int nRemotePort)
	{
		m_except = null;
		Reset();
		if(sRemoteAddress!=null) m_sRemoteAddress = sRemoteAddress;
		if(nRemotePort>0) m_nRemotePort = nRemotePort;
		try
		{
			m_sock = new Socket(m_sRemoteAddress, m_nRemotePort);
			return true;
		}
		catch(Exception oBug)
		{
			m_except = oBug;
			return false;
		}
	}
	final public void Reset()
	{
		m_bReconnect = false;
		try
		{
			if(m_sock!=null)
			{
				byte[] pData = new byte[4];
				pData[0] =  (byte)255;
				pData[1] = pData[2] = pData[3] = 0;
				m_sock.getOutputStream().write(pData);
				m_sock.close();
			}
		}
		catch(Exception oBug)
		{
		}
		m_bReconnect = true;
	}
	public boolean SendBinaryData(byte[] pData)
	{
		byte[] pData2 = new byte[pData.length+4];
		pData2[0] = (byte)(1+(pData.length/16777216)*16);
		pData2[1] = (byte)(pData.length%256);
		pData2[2] = (byte)((pData.length%65536)/256);
		pData2[3] = (byte)((pData.length/65536)%256);
		System.arraycopy(pData, 0, pData2, 4, pData.length);
		return SendRawData(pData2);
	}
	public boolean SendStringData(String sData)
	{
		int nSize = sData.length();
		byte[] pData = new byte[2*nSize+4];
		pData[0] = (byte)(((2*nSize)/16777216)*16);
		pData[1] = (byte)((2*nSize)%256);
		pData[2] = (byte)(((2*nSize)%65536)/256);
		pData[3] = (byte)(((2*nSize)/65536)%256);
		for(int i=0;i<nSize;i++)
		{
			pData[4+2*i] = (byte)(sData.charAt(i)&0x000000FF);
			pData[4+2*i+1] = (byte)((sData.charAt(i)&0x0000FF00)/256);
		}
		return SendRawData(pData);
	}
	public boolean ReceiveData(int[] pSize, boolean[] pIsBinary)
	{
		m_except = null;
		try
		{
			m_threadRun = new Thread(this);
			m_threadRun.start();
			long nStart = System.currentTimeMillis();
			while(m_threadRun!=null)
			{
				Thread.currentThread().sleep(50);
				if((System.currentTimeMillis()-nStart)>m_nReadTimeout*1000)
					throw new Exception("Timeout while receiving incoming data");
			}
			if(m_pData!=null)
			{
				if(pSize!=null) pSize[0] = m_pData.length;
				if(pIsBinary!=null) pIsBinary[0] = m_bIsBinary;
				return true;
			}
			if(m_except!=null) throw m_except;
		}
		catch(Exception oBug)
		{
			Exception oExcept = oBug;
			if(m_threadRun!=null)
			{
				try
				{
					m_threadRun.interrupt();
					Thread.currentThread().sleep(50);
				}
				catch(Exception oError) {}
			}
			if(m_bReconnect) Connect(null, 0);
			m_except = oExcept;
			m_threadRun = null;
		}
		return false;
	}
	public String GetStringData()
	{
		if(m_pData==null) return null;
		int nSize = m_pData.length/2;
		char[] pData = new char[nSize];
		for(int i=0;i<nSize;i++)
		{
			pData[i] = (char)((m_pData[2*i]&0x000000FF)+(m_pData[2*i+1]&0x000000FF)*256);
		}
		return new String(pData);
	}
	public byte[] GetBinaryData()
	{
		return m_pData;
	} 
	public void SetReadTimeout(int nReadTimeout)
	{
		if(nReadTimeout>=5&&nReadTimeout<=120) m_nReadTimeout = nReadTimeout;
	}
	public void SetMaxDataSize(int nMaxDataSize)
	{
		if(nMaxDataSize>=1024) m_nMaxDataSize = nMaxDataSize;
	}
	public Exception GetLastException()
	{
		return m_except;
	}
}