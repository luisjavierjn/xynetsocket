using System;
using System.Net.Sockets;
using System.Threading;
using XYNetSocketLib;

namespace XYNetSocketTest
{
	class Test
	{	
		private static XYNetServer myServer = null;

		private static void ExceptionHandler(Exception oBug)
		{
			System.Console.Out.WriteLine("Error: " + oBug.Message);
			Exception oEx = myServer.GetLastException();
			if(oEx!=null) System.Console.Out.WriteLine("Error: " + oEx.Message);
		}

		private static void ConnectionFilter(String sRemoteAddress, int nRemotePort, Socket sock)
		{
			System.Console.Out.WriteLine("Connection request from " + sRemoteAddress + ":" + nRemotePort.ToString());
		}
	
		private static void BinaryInputHandler(String sRemoteAddress, int nRemotePort, Byte[] pData)
		{
			System.Console.Out.WriteLine("Thread count = " + myServer.GetThreadCount().ToString());
			System.Console.Out.WriteLine("Client count = " + myServer.GetClientCount().ToString());
			System.Console.Out.WriteLine("Server received binary data from " + sRemoteAddress + ":" + nRemotePort.ToString());
			// System.Console.Out.WriteLine(XYNetCommon.BinaryToString(pData));
			if(myServer.SendBinaryData(sRemoteAddress, nRemotePort, pData))
				System.Console.Out.WriteLine("Binary reply sent");
		}

		private static void StringInputHandler(String sRemoteAddress, int nRemotePort, String sData)
		{
			System.Console.Out.WriteLine("Thread count = " + myServer.GetThreadCount().ToString());
			System.Console.Out.WriteLine("Client count = " + myServer.GetClientCount().ToString());
			System.Console.Out.WriteLine("Server received string data from " + sRemoteAddress + ":" + nRemotePort.ToString());
			System.Console.Out.WriteLine(sData);
			if(myServer.SendStringData(sRemoteAddress, nRemotePort, sData))
				System.Console.Out.WriteLine("String reply sent");
		}

		[STAThread]
		static void Main(string[] args)
		{
			try
			{
				if(args.Length==1)
				{
					myServer = new XYNetServer("", Convert.ToInt32(args[0]), 5, 10);
					myServer.SetConnectionFilter(new ConnectionFilterDelegate(Test.ConnectionFilter));
					myServer.SetExceptionHandler(new ExceptionHandlerDelegate(Test.ExceptionHandler));
					myServer.SetBinaryInputHandler(new BinaryInputHandlerDelegate(Test.BinaryInputHandler));
					myServer.SetStringInputHandler(new StringInputHandlerDelegate(Test.StringInputHandler));
					if(myServer.StartServer()==false) throw myServer.GetLastException();
                    System.Console.Out.WriteLine("Waiting for connections on Port {0}", args[0]);
					Thread.Sleep(600000);
					System.Console.Out.WriteLine("Thread count: " + myServer.GetThreadCount().ToString());
					System.Console.Out.WriteLine("Client count: " + myServer.GetClientCount().ToString());
					myServer.StopServer();
					System.Console.Out.WriteLine("Done!");
				}
				else if(args.Length==2)
				{
					const int nSize = 100;
					const int nPause = 10;
					XYNetClient[] pClients = new XYNetClient[nSize];
					for(int i=0;i<nSize;i++)
					{
						pClients[i] = new XYNetClient(args[0], Convert.ToInt32(args[1]));
						if(pClients[i].Connect()==false) throw pClients[i].GetLastException();
						Thread.Sleep(nPause);
					}
					for(int i=0;i<nSize;i++)
					{
						if(pClients[i].SendStringData("Text sent as string data")==false)
							throw pClients[i].GetLastException();
						Thread.Sleep(nPause);
					}
					for(int i=0;i<nSize;i++)
					{
						Object[] pData = pClients[i].ReceiveData();
						if(pData==null) throw pClients[i].GetLastException();
						String sData = (String)(pData[1]);
						System.Console.Out.WriteLine("String data from server: " + sData);
						Thread.Sleep(nPause);
					}
					for(int i=0;i<nSize;i++)
					{
						if(pClients[i].SendBinaryData(XYNetCommon.StringToBinary("Text sent as binary data"))==false)
							throw pClients[i].GetLastException();
						Thread.Sleep(nPause);
					}
					for(int i=0;i<nSize;i++)
					{
						Object[] pData = pClients[i].ReceiveData();
						if(pData==null) throw pClients[i].GetLastException();
						Byte[] pData2 = (Byte[])(pData[0]);
						System.Console.Out.WriteLine("Binary data from server: " + XYNetCommon.BinaryToString(pData2));
						Thread.Sleep(nPause);
					}
					for(int i=0;i<nSize;i++)
					{
						pClients[i].Reset();
					}
				}
				else throw new Exception("Invalid number of arguments");
			}
			catch(Exception oBug)
			{
				System.Console.Out.WriteLine("Error Type: " + oBug.GetType().Name);
				System.Console.Out.WriteLine("Error Message: " + oBug.Message);
				System.Console.Out.WriteLine("Error Source: " + oBug.Source);
				System.Console.Out.WriteLine("Error StackTrace: " + oBug.StackTrace);
				if(args.Length>0) System.Console.Out.WriteLine("arg0 = " + args[0]);
				if(args.Length>1) System.Console.Out.WriteLine("arg1 = " + args[1]);
			}
		}
	}
}
