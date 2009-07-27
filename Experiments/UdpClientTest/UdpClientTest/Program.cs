using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace UdpClientTest
{
	class Program
	{
		static void Main(string[] args)
		{
			using (Listener l = new Listener())
			{
				int portNr = l.Port;
				new Thread(() =>
				{
					while (true)
					{
						UdpClient client = new UdpClient();
						client.Connect(new IPEndPoint(IPAddress.Loopback, portNr));
						var data = Encoding.UTF8.GetBytes( Console.ReadKey(true).KeyChar.ToString());
						IPEndPoint remote = null;
						client.Send(data, data.Length);


						var recData = client.Receive(ref remote);
						if (!recData.SequenceEqual(data))
							throw new Exception("bad mirror!");
					}

				}) { IsBackground = true, Priority = ThreadPriority.BelowNormal }.Start();

				l.Loop();
			}
		}

	}

	sealed class Listener : IDisposable
	{
		Socket serverSocket;
		int port;
		public int Port { get { return port; } }
		public Listener()
		{
			try
			{
				serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
				port = (new Random().Next() % 60000)+5000;
				IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, port);
				serverSocket.Bind(endPoint);
			}
			catch
			{
				serverSocket.Close();
				((IDisposable)serverSocket).Dispose();
				throw;
			}
		}
		StringBuilder sb =new StringBuilder();
		public void Loop()
		{
			byte[] buffer = new byte[65536];
			while(true) {
				EndPoint remoteEP = new IPEndPoint(IPAddress.Any,0) ;
				int recBytes = serverSocket.ReceiveFrom(buffer, ref remoteEP);
				serverSocket.SendTo(buffer, recBytes, SocketFlags.None, remoteEP); //mirror

				string message = Encoding.UTF8.GetString(buffer, 0, recBytes);
				foreach (char c1 in message)
				{
					var c = c1 == 13?'\n':c1;
					sb.Append(c);
					if (c == '\n')
					{
						Console.Write(sb.ToString());
						if (sb.ToString() == "EXIT\n")
							return;
						sb.Length = 0;
					}
				}
			}
			
		}

		public void Dispose()
		{
			serverSocket.Close();
		}
	}
}
