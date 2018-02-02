import java.net.*;
import java.io.*;
//import java.util.*;

public class XYNetJavaClientTest
{
	public static void main (String[] args)
	{
		try
		{
			if(args.length<2) throw new Exception("Invalid number of parameters");
			int nSize = 100;
			int nPause = 10;
			XYNetJavaClient[] pClients = new XYNetJavaClient[nSize];
			for(int i=0;i<nSize;i++)
			{
				pClients[i] = new XYNetJavaClient();
				if(pClients[i].Connect(args[0], new Integer(args[1]).intValue())==false)
					throw pClients[i].GetLastException();
				Thread.currentThread().sleep(nPause);
			}
			for(int i=0;i<nSize;i++)
			{
				if(pClients[i].SendStringData("Text sent as string data: 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789")==false)
					throw pClients[i].GetLastException();
				Thread.currentThread().sleep(nPause);
			}
			for(int i=0;i<nSize;i++)
			{
				if(pClients[i].ReceiveData(null,null)==false)
					throw pClients[i].GetLastException();
				System.out.println("String data from server: " + pClients[i].GetStringData());
				Thread.currentThread().sleep(nPause);
			}
			for(int i=0;i<nSize;i++)
			{
				if(pClients[i].SendBinaryData("Text sent as binary data: 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789 123456789".getBytes())==false)
					throw pClients[i].GetLastException();
				Thread.currentThread().sleep(nPause);
			}
			for(int i=0;i<nSize;i++)
			{
				if(pClients[i].ReceiveData(null,null)==false)
					throw pClients[i].GetLastException();
				System.out.println("Binary data from server: " + new String(pClients[i].GetBinaryData()));
				Thread.currentThread().sleep(nPause);
			}
			for(int i=0;i<nSize;i++)
			{
				pClients[i].Reset();
			}
		}
		catch(Exception oBug)
		{
			System.out.println(oBug.getMessage());
		}
	}
}