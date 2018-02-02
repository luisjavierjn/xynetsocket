using System;
using System.Threading;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using XYThreadPoolLib;

namespace XYNetSocketLib
{
	public delegate void ExceptionHandlerDelegate(Exception oBug);
	public delegate void ConnectionFilterDelegate(String sRemoteAddress, int nRemotePort, Socket sock);
	public delegate void BinaryInputHandlerDelegate(String sRemoteAddress, int nRemotePort, Byte[] pData);
	public delegate void StringInputHandlerDelegate(String sRemoteAddress, int nRemotePort, String sData);

	public class XYNetServer
	{
		private delegate void AcceptClientsDelegate();
		private delegate void DetectInputDelegate();
		private delegate void ProcessInputDelegate(Socket sock, IPEndPoint ipe);
		
		private const int m_nServerPause = 25;
		private const int m_nListenBacklog = 32;
		private const int m_nArrayCapacity = 512;
		private int m_nReadTimeout = 30;
		private int m_nMaxDataSize = 4*1024*1024;
		private int m_nMinThreadCount = 7;
		private int m_nMaxThreadCount = 12;
		private String m_sAddress = "";
		private int m_nPort = 0;
		private Socket m_socketServer = null;
		private XYThreadPool m_threadPool = new XYThreadPool();
		private Hashtable m_htSockets = new Hashtable(m_nArrayCapacity);
		private ArrayList m_listSockets = new ArrayList(m_nArrayCapacity);
		private ConnectionFilterDelegate m_delegateConnectionFilter = null;
		private ExceptionHandlerDelegate m_delegateExceptionHandler = null;
		private BinaryInputHandlerDelegate m_delegateBinaryInputHandler = null;
		private StringInputHandlerDelegate m_delegateStringInputHandler = null;
		private Exception m_exception = null;
		
		public XYNetServer(String sAddress, int nPort, int nMinThreadCount, int nMaxThreadCount)
		{
			if(sAddress!=null) m_sAddress = sAddress;
			if(nPort>0) m_nPort = nPort;
			if(nMinThreadCount>0) m_nMinThreadCount = nMinThreadCount+2;
			if(nMinThreadCount>0&&nMaxThreadCount>nMinThreadCount) m_nMaxThreadCount = nMaxThreadCount+2;
			else m_nMaxThreadCount = 2*(m_nMinThreadCount-2)+2;
		}

		~XYNetServer()
		{
			StopServer();
		}

		public Exception GetLastException()
		{
			Monitor.Enter(this);
			Exception exp = m_exception;
			Monitor.Exit(this);
			return exp;
		}

		public void StopServer()
		{
			try
			{
				Monitor.Enter(this);
				m_threadPool.StopThreadPool();
				if(m_socketServer!=null)
				{
					Socket sock = null;
					for(int i=0;i<m_listSockets.Count;i++)
					{
						sock = (Socket)(m_listSockets[i]);
						try
						{
							sock.Shutdown(SocketShutdown.Both);
							sock.Close();
						}
						catch(Exception) {}
					}
					try
					{
						m_socketServer.Shutdown(SocketShutdown.Both);
						m_socketServer.Close();
					}
					catch(Exception) {}
				}
			}
			catch(Exception) {}
			finally 
			{
				try
				{
					m_socketServer = null;
					m_htSockets.Clear();
					m_listSockets.Clear();
				}
				catch(Exception) {}
				Monitor.Exit(this); 
			}
		}

		public bool StartServer()
		{
			try
			{
				Monitor.Enter(this);
				XYNetCommon.SetSocketPermission();
				StopServer();
				m_threadPool.SetThreadErrorHandler(new ThreadErrorHandlerDelegate(ThreadErrorHandler));
				m_threadPool.StartThreadPool(m_nMinThreadCount, m_nMaxThreadCount);
				m_socketServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				m_socketServer.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
				IPEndPoint myEnd = (m_sAddress=="")?(new IPEndPoint(Dns.GetHostByName(Dns.GetHostName()).AddressList[0], m_nPort)):(new IPEndPoint(IPAddress.Parse(m_sAddress), m_nPort));
				m_socketServer.Bind(myEnd);
				m_socketServer.Listen(m_nListenBacklog);
				m_threadPool.InsertWorkItem("Accept Clients", new AcceptClientsDelegate(AcceptClients), null, false);
				m_threadPool.InsertWorkItem("Detect Input", new DetectInputDelegate(DetectInput), null, false);
				return true;
			}
			catch(Exception oBug) 
			{ 
				m_exception = oBug; 
				return false;
			}
			finally { Monitor.Exit(this); }
		}

		public void SetReadTimeout(int nReadTimeout)
		{
			Monitor.Enter(this);
			if(nReadTimeout>=5&&nReadTimeout<=1200) m_nReadTimeout = nReadTimeout;
			Monitor.Exit(this);
		}

		public void SetMaxDataSize(int nMaxDataSize)
		{
			Monitor.Enter(this);
			if(nMaxDataSize>=1024) m_nMaxDataSize = nMaxDataSize;
			Monitor.Exit(this);
		}

		public void SetConnectionFilter(ConnectionFilterDelegate pMethod)
		{
			Monitor.Enter(this);
			if(m_delegateConnectionFilter==null) m_delegateConnectionFilter = pMethod;
			Monitor.Exit(this);
		}

		public void SetExceptionHandler(ExceptionHandlerDelegate pMethod)
		{
			Monitor.Enter(this);
			if(m_delegateExceptionHandler==null) m_delegateExceptionHandler = pMethod;
			Monitor.Exit(this);
		}

		public void SetBinaryInputHandler(BinaryInputHandlerDelegate pMethod)
		{
			Monitor.Enter(this);
			if(m_delegateBinaryInputHandler==null) m_delegateBinaryInputHandler = pMethod;
			Monitor.Exit(this);
		}

		public void SetStringInputHandler(StringInputHandlerDelegate pMethod)
		{
			Monitor.Enter(this);
			if(m_delegateStringInputHandler==null) m_delegateStringInputHandler = pMethod;
			Monitor.Exit(this);
		}

		private void ThreadErrorHandler(ThreadPoolWorkItem oWorkItem, Exception oBug)
		{
			try
			{
				Monitor.Enter(this);
				if(m_delegateExceptionHandler!=null)
				{
					m_threadPool.InsertWorkItem("Handle Exception", m_delegateExceptionHandler, new Object[1]{oBug}, false);
				}
				else m_exception = oBug;
			}
			catch(Exception) { }
			finally { Monitor.Exit(this); }
		}

		private void AcceptClients()
		{
			while(true)
			{
				bool bHasNewClient = false;
				Socket sock = null;
				try
				{
					if(m_socketServer.Poll(m_nServerPause, SelectMode.SelectRead))
					{
						bHasNewClient = true;
						sock = m_socketServer.Accept();
						IPEndPoint ipe = (IPEndPoint)(sock.RemoteEndPoint);
						if(m_delegateConnectionFilter!=null)
						{
							m_delegateConnectionFilter.DynamicInvoke(new Object[3]{ipe.Address.ToString(), ipe.Port, sock});
						}
						if(sock.Connected)
						{
							String sKey = ipe.Address.ToString() + ":" + ipe.Port.ToString();
							Monitor.Enter(this);
							m_htSockets.Add(sKey, sock);
							m_listSockets.Add(sock);
							Monitor.Exit(this);
						}
					}
				}
				catch(Exception oBug)
				{
					if(sock!=null)
					{
						try
						{
							sock.Shutdown(SocketShutdown.Both);
							sock.Close();
						}
						catch(Exception) {}
					}
					if(m_delegateExceptionHandler!=null)
					{
						m_threadPool.InsertWorkItem("Handle Exception", m_delegateExceptionHandler, new Object[1]{oBug}, false);
					}
					else 
					{
						Monitor.Enter(this);
						m_exception = oBug;
						Monitor.Exit(this);
					}
				}
				if(bHasNewClient) Thread.Sleep(m_nServerPause);
				else Thread.Sleep(10*m_nServerPause);
			}
		}

		private void DetectInput()
		{
			int nCounter = 0;
			while(true)
			{
				nCounter++;
				bool bNoData = true;
				try
				{
					for(int i=m_listSockets.Count-1;i>=0;i--)
					{
						Socket sock = null;
						IPEndPoint ipe = null;
						try
						{
							sock = (Socket)(m_listSockets[i]);
							ipe = (IPEndPoint)(sock.RemoteEndPoint);
							if(!sock.Connected) throw new Exception("Connection to client closed");
							if(nCounter%1000==0) 
							{
								if(sock.Send(new Byte[4]{2,0,0,0})!=4)
									throw new Exception("Failed to ping client socket");
							}
							if(sock.Available>0)
							{
								Monitor.Enter(this);
								m_listSockets.RemoveAt(i);
								Monitor.Exit(this);
								bNoData = false;
								m_threadPool.InsertWorkItem("Process Input", new ProcessInputDelegate(ProcessInput), new Object[2] {sock, ipe}, false);
							}
						}
						catch(Exception oBug)
						{
							if(i>=0&&sock!=null)
							{
								Monitor.Enter(this);
								m_listSockets.RemoveAt(i);
								m_htSockets.Remove(ipe.Address.ToString() + ":" + ipe.Port.ToString());
								Monitor.Exit(this);
								try
								{
									sock.Shutdown(SocketShutdown.Both);
									sock.Close();
								}
								catch(Exception) {}
							}
							if(m_delegateExceptionHandler!=null)
							{
								m_threadPool.InsertWorkItem("Handle Exception", m_delegateExceptionHandler, new Object[1]{oBug}, false);
							}
						}
					}	
					if(bNoData) Thread.Sleep(10*m_nServerPause);
					else Thread.Sleep(m_nServerPause);
				}
				catch(Exception) {}
			}
		}

		private void ProcessInput(Socket sock, IPEndPoint ipe)
		{
			try
			{
				Byte[] pHeader = new Byte[4];
				int nPos = 0;
				long nStart = DateTime.Now.Ticks;
				while(nPos<4)
				{
					if(sock.Available>0)
					{
						nPos += sock.Receive(pHeader, nPos, Math.Min(sock.Available,(4-nPos)), SocketFlags.None);
						if((pHeader[0]&0x000000FF)==255) 
						{
							sock.Shutdown(SocketShutdown.Both);
							sock.Close();
							Monitor.Enter(this);
							m_htSockets.Remove(ipe.Address.ToString() + ":" + ipe.Port.ToString());
							Monitor.Exit(this);
							return;
						}
					}
					else Thread.Sleep(m_nServerPause);
					if(nPos<4&&((DateTime.Now.Ticks-nStart)/10000)>m_nReadTimeout*1000) throw new Exception("Timeout while receiving incoming data");
				}
				if((pHeader[0]&0x0000000F)!=2)
				{
					int nSize = pHeader[1] + pHeader[2]*256 + pHeader[3]*65536+(pHeader[0]/16)*16777216;
					if(nSize>m_nMaxDataSize) throw new Exception("Data size too large");
					Byte[] pData = new Byte[nSize];
					nPos = 0;
					nStart = DateTime.Now.Ticks;
					while(nPos<nSize)
					{
						if(sock.Available>0)
						{
							nPos += sock.Receive(pData, nPos, Math.Min(sock.Available, (nSize-nPos)), SocketFlags.None);
						}
						else Thread.Sleep(m_nServerPause);
						if(nPos<nSize&&((DateTime.Now.Ticks-nStart)/10000)>m_nReadTimeout*1000) throw new Exception("Timeout while receiving incoming data");
					}
					Monitor.Enter(this);
					m_listSockets.Add(sock);
					Monitor.Exit(this);
					if((pHeader[0]&0x0000000F)==1)
					{
						if(m_delegateBinaryInputHandler!=null) 
						{
							m_threadPool.InsertWorkItem("Handle Binary Input", new BinaryInputHandlerDelegate(m_delegateBinaryInputHandler), new Object[3] {ipe.Address.ToString(), ipe.Port, pData}, false);
						}
						else throw new Exception("No binary input handler");
					}
					else if((pHeader[0]&0x0000000F)==0)
					{
						if(m_delegateStringInputHandler!=null) 
						{
							m_threadPool.InsertWorkItem("Handle String Input", new StringInputHandlerDelegate(m_delegateStringInputHandler), new Object[3] {ipe.Address.ToString(), ipe.Port, XYNetCommon.BinaryToString(pData)}, false);
						}
						else throw new Exception("No string input handler");
					}
					else throw new Exception("Invalid string data size");
				}
			}
			catch(Exception oBug)
			{
				Monitor.Enter(this);
				m_htSockets.Remove(ipe.Address.ToString() + ":" + ipe.Port.ToString());
				Monitor.Exit(this);
				try
				{
					sock.Shutdown(SocketShutdown.Both);
					sock.Close();
				}
				catch(Exception) {}
				if(m_delegateExceptionHandler!=null)
				{
					m_threadPool.InsertWorkItem("Handle Exception", m_delegateExceptionHandler, new Object[1]{oBug}, false);
				}
				else 
				{
					Monitor.Enter(this);
					m_exception = oBug;
					Monitor.Exit(this);
				}
			}
		}

		private bool SendRawData(String sRemoteAddress, int nRemotePort, Byte[] pData)
		{
			if(sRemoteAddress==null||pData==null) return false;
			Socket sock = null;
			try
			{
				Monitor.Enter(this);
				sock = (Socket)(m_htSockets[sRemoteAddress+":"+nRemotePort.ToString()]);
				if(sock==null) throw new Exception("No client connection at the given address and port");
				if(m_listSockets.Contains(sock)==false)
				{
					sock = null;
                    m_threadPool.InsertWorkItem("Handle Exception", m_delegateExceptionHandler, new Object[1] { new Exception("Client socket in use") }, false);
                    return false;
				}
				return sock.Send(pData)==pData.Length;
			}
			catch(Exception oBug)
			{
				try
				{
					if(sock!=null)
					{
						sock.Shutdown(SocketShutdown.Both);
						sock.Close();
					}
				}
				catch(Exception) {}
				if(m_delegateExceptionHandler!=null)
				{
					m_threadPool.InsertWorkItem("Handle Exception", m_delegateExceptionHandler, new Object[1]{oBug}, false);
				}
				else m_exception = oBug;
				return false;
			}
			finally { Monitor.Exit(this); }
		}

		public bool SendBinaryData(String sRemoteAddress, int nRemotePort, Byte[] pData)
		{
			Byte[] pData2 = new Byte[pData.Length+4];
			pData2[0] = (Byte)(1+(pData.Length/16777216)*16);
			pData2[1] = (Byte)(pData.Length%256);
			pData2[2] = (Byte)((pData.Length%65536)/256);
			pData2[3] = (Byte)((pData.Length/65536)%256);
			pData.CopyTo(pData2,4);
			return SendRawData(sRemoteAddress, nRemotePort, pData2);
		}

		public bool SendStringData(String sRemoteAddress, int nRemotePort, String sData)
		{
			Byte[] pData = new Byte[sData.Length*2+4];
			pData[0] = (Byte)(((2*sData.Length)/16777216)*16);
			pData[1] = (Byte)((2*sData.Length)%256);
			pData[2] = (Byte)(((2*sData.Length)%65536)/256);
			pData[3] = (Byte)(((2*sData.Length)/65536)%256);
			XYNetCommon.StringToBinary(sData).CopyTo(pData, 4);
			return SendRawData(sRemoteAddress, nRemotePort, pData);
		}

		public int GetThreadCount()
		{
			int nCount = m_threadPool.GetThreadCount()-2;
			return nCount>0?nCount:0;
		}

		public int GetClientCount()
		{
			Monitor.Enter(this);
			int nCount = m_htSockets.Count;
			Monitor.Exit(this);
			return nCount;
		}
	}

	public class XYNetClient
	{
		private const int m_nClientPause = 50;
		private String m_sRemoteAddress = "";
		private int m_nRemotePort = 0;
		private int m_nMaxDataSize = 4*1024*1024;
		private int m_nReadTimeout = 30;
		private Exception m_exception = null;
		private Socket m_socketClient = null;

		public XYNetClient(String sRemoteAddress, int nRemotePort)
		{
			if(sRemoteAddress!=null) m_sRemoteAddress = sRemoteAddress;
			if(nRemotePort>0) m_nRemotePort = nRemotePort;
		}

		~XYNetClient()
		{
			Reset();
		}

		public Exception GetLastException() 
		{
			Monitor.Enter(this);
			Exception exp = m_exception;
			Monitor.Exit(this);
			return exp;
		}

		public void SetMaxDataSize(int nMaxDataSize)
		{
			if(nMaxDataSize>=1024) m_nMaxDataSize = nMaxDataSize;
		}

		protected Socket GetSocket()
		{
			Monitor.Enter(this);
			Socket sock = m_socketClient;
			Monitor.Exit(this);
			return sock;
		}

		public virtual bool Connect()
		{
			try
			{
				Monitor.Enter(this);
				XYNetCommon.SetSocketPermission();
				Reset();
				m_socketClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				IPEndPoint myEnd = null;
				try
				{
					myEnd = (m_sRemoteAddress=="")?(new IPEndPoint(Dns.GetHostByName(Dns.GetHostName()).AddressList[0], m_nRemotePort)):(new IPEndPoint(IPAddress.Parse(m_sRemoteAddress), m_nRemotePort));
				}
				catch(Exception) {}
				if(myEnd==null)
				{
					myEnd = new IPEndPoint(Dns.GetHostByName(m_sRemoteAddress).AddressList[0], m_nRemotePort);
				}
				m_socketClient.Connect(myEnd);
				return true;
			}
			catch(Exception oBug)
			{
				m_exception = oBug;
				try
				{
					m_socketClient.Shutdown(SocketShutdown.Both);
					m_socketClient.Close();
				}
				catch(Exception) {}
				return false;
			}
			finally { Monitor.Exit(this); }
		}

		private bool SendRawData(Byte[] pData)
		{
			try
			{
				Monitor.Enter(this);
				return m_socketClient.Send(pData)==pData.Length;
			}
			catch(Exception oBug)
			{
				Connect();
				m_exception = oBug;
				return false;
			}
			finally { Monitor.Exit(this); }
		}

		public bool SendBinaryData(Byte[] pData)
		{
			Byte[] pData2 = new Byte[pData.Length+4];
			pData2[0] = (Byte)(1+(pData.Length/16777216)*16);
			pData2[1] = (Byte)(pData.Length%256);
			pData2[2] = (Byte)((pData.Length%65536)/256);
			pData2[3] = (Byte)((pData.Length/65536)%256);
			pData.CopyTo(pData2,4);
			return SendRawData(pData2);
		}

		public bool SendStringData(String sData)
		{
			Byte[] pData = new Byte[sData.Length*2+4];
			pData[0] = (Byte)(((2*sData.Length)/16777216)*16);
			pData[1] = (Byte)((2*sData.Length)%256);
			pData[2] = (Byte)(((2*sData.Length)%65536)/256);
			pData[3] = (Byte)(((2*sData.Length)/65536)%256);
			XYNetCommon.StringToBinary(sData).CopyTo(pData, 4);
			return SendRawData(pData);
		}

		public void SetReadTimeout(int nReadTimeout)
		{
			Monitor.Enter(this);
			if(nReadTimeout>=5&&nReadTimeout<=1200) m_nReadTimeout = nReadTimeout;
			Monitor.Exit(this);
		}

		public Object[] ReceiveData()
		{
			try
			{
				Monitor.Enter(this);
				m_socketClient.Blocking = false;
				long nStart = DateTime.Now.Ticks;
				int nRead = 0;
				int nTotal = 4;
				Byte[] pData = new Byte[4];
				Byte[] pHeader = null;
				while(true)
				{
					try
					{
						Thread.Sleep(m_nClientPause);
						if(m_socketClient.Available>0)
						{
							nRead += m_socketClient.Receive(pData, nRead, nTotal-nRead, SocketFlags.None);
							if((pData[0]&0x0000000F)==2) nRead = 0;
						}
					}
					catch(Exception) {}
					if(pHeader==null&&nRead==4)
					{
						nTotal = (pData[1]&0x000000FF) + (pData[2]&0x000000FF)*256 + (pData[3]&0x000000FF)*65536+((pData[0]&0x000000FF)/16)*16777216;
						if((pData[0]&0x0000000F)>1) throw new Exception("Invalid input data type byte");
						if(nTotal>m_nMaxDataSize) throw new Exception("Data size too large");
						pHeader = pData;
						nRead = 0;
						pData = new Byte[nTotal];
					}
					if(pHeader!=null&&nRead==nTotal) break;
					if(((DateTime.Now.Ticks-nStart)/10000)>m_nReadTimeout*1000) throw new Exception("Timeout while receiving incoming data");
				}
				if((pHeader[0]&0x0000000F)==1)
				{
					return new Object[2] { pData, null };
				}
				else
				{
					if(pData.Length%2!=0) throw new Exception("Invalid string data size");
					return new Object[2] { null, XYNetCommon.BinaryToString(pData) };
				}
			}
			catch(Exception oBug)
			{
				Connect();
				m_exception = oBug;
				return null;
			}
			finally 
			{ 
				m_socketClient.Blocking = true; 
				Monitor.Exit(this); 
			}
		}

		public void Reset()
		{
			try
			{
				Monitor.Enter(this);
				if(m_socketClient!=null)
				{
					m_socketClient.Send(new Byte[4]{255, 0, 0, 0}, SocketFlags.None);
					m_socketClient.Shutdown(SocketShutdown.Both);
					m_socketClient.Close();
				}
			}
			catch(Exception) {}
			finally 
			{
				m_socketClient = null;
				Monitor.Exit(this); 
			}
		}
	}

	public class XYNetCommon
	{
		public static SocketPermission m_permissionSocket = null;
		private static bool m_bPermissionSet = false;		
		
		public static void SetSocketPermission()
		{
			lock(typeof(XYNetCommon))
			{
				if(m_bPermissionSet==false)
				{
					if(m_permissionSocket!=null)
					{
						m_permissionSocket.Demand();
					}
					m_bPermissionSet = true;
				}
			}
		}

		public static String BinaryToString(Byte[] pData)
		{
			if((pData.Length%2)!=0) throw new Exception("Invalid string data size");
			Char[] pChar = new Char[pData.Length/2];
			for(int i=0;i<pChar.Length;i++)
			{
				pChar[i] = (Char)((pData[2*i]&0x000000FF) + (pData[2*i+1]&0x000000FF)*256);
			}
			return new String(pChar);
		}

		public static Byte[] StringToBinary(String sData)
		{
			Byte[] pData = new Byte[sData.Length*2];
			for(int i=0;i<sData.Length;i++)
			{
				pData[2*i] = (Byte)((sData[i]&0x000000FF)%256);
				pData[2*i+1] = (Byte)((sData[i]&0x0000FF00)/256);
			}
			return pData;
		}
	}
}
