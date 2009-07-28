using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Security.Cryptography;

namespace UdpClientTest
{
	class Program
	{
		public const double ClientErr = 0.0001;
		public const double ServerErr = 0.0001;
		static void Main(string[] args)
		{
			foreach (ushort port in args
				.Where(arg => arg.StartsWith("/server="))
				.Select(arg => ushort.Parse(arg.Substring("/server=".Length))))
			{
				new Thread((serverPort) =>
				{
					using (Listener l = new Listener((int)serverPort))
						l.Loop();
				}).Start((int)port);
			}

			int clientCount = Math.Max(1,
						args
						.Where(arg => arg.StartsWith("/clientcount="))
						.Select(arg => int.Parse(arg.Substring("/clientcount=".Length)))
						.FirstOrDefault());

			foreach (IPEndPoint serverLocation in
				from arg in args
				where arg.StartsWith("/connect=")
				let endPointStr = arg.Substring("/connect=".Length)
				let portSplitterIdx = endPointStr.LastIndexOf(':')
				let ipAddrStr = endPointStr.Substring(0, portSplitterIdx)
				let portStr = endPointStr.Substring(portSplitterIdx + 1)
				let ipAddr = IPAddress.Parse(ipAddrStr)
				let port = ushort.Parse(portStr)
				select new IPEndPoint(ipAddr, port)
			)
				for (int i = 0; i < clientCount; i++)
					new Thread((object endPoint) =>
					{
						UdpClientProc((IPEndPoint)endPoint);
					}).Start(serverLocation);
		}

		static void UdpClientProc(IPEndPoint endPoint)
		{
			Console.WriteLine("Connecting to: {0}", endPoint);
			byte[] intB = new byte[4];

			Random r = new Random((Thread.CurrentThread.ManagedThreadId << 24) ^ (int)DateTime.Now.Ticks);
			for (int i = 0; i < int.MaxValue; i++)
			{
				try
				{
					UdpClient client = new UdpClient();
					client.Ttl = 5;
					client.Client.SendTimeout = 500;
					client.Client.ReceiveTimeout = 500;

					client.Connect(endPoint);

					int v = r.Next();
					var data = BitConverter.GetBytes(v).Concat(BitConverter.GetBytes(-v)).ToArray();
					IPEndPoint remote = null;
					if (r.NextDouble() < ClientErr)
						Console.WriteLine("Client: not sending SIM");
					else
						client.Send(data, data.Length);

					var recData = client.Receive(ref remote);
					if (recData.Length != 8
						|| BitConverter.ToInt32(recData, 0) + BitConverter.ToInt32(recData, 4) != 0
						|| BitConverter.ToInt32(recData, 4) != v
						)
						throw new Exception("Inconsistent packet");
					client.Close();
				}
				catch (SocketException se)
				{
					if (se.ErrorCode == 10060)
						Console.WriteLine("Client receive time-out");
					else
						throw;
				}
			}
		}

	}

	sealed class Listener : IDisposable
	{
		Socket serverSocket;
		int port;
		public int Port { get { return port; } }
		public Listener() : this(0) { }
		public Listener(int param_port)
		{
			try
			{
				serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
				port = param_port == 0 ? (new Random().Next() % 60000) + 5000 : param_port;
				IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, port);
				serverSocket.Bind(endPoint);
				Console.WriteLine("Using port:{0}", port);
			}
			catch
			{
				serverSocket.Close();
				((IDisposable)serverSocket).Dispose();
				throw;
			}
		}
		StringBuilder sb = new StringBuilder();
		long count = 0;
		public void Loop()
		{
			Random r = new Random((Thread.CurrentThread.ManagedThreadId << 24) ^ (int)DateTime.Now.Ticks);
			byte[] buffer = new byte[65536];
			while (true)
			{
				EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
				int recBytes = serverSocket.ReceiveFrom(buffer, ref remoteEP);
				if (recBytes != 8) throw new Exception("Should have received 8 bytes");
				if (BitConverter.ToInt32(buffer, 0) + BitConverter.ToInt32(buffer, 4) != 0)
					throw new Exception("Inconsistent packet");
				if (r.NextDouble() < Program.ServerErr)
					Console.WriteLine("Server: not sending SIM");
				else
					serverSocket.SendTo(buffer.Skip(4).Take(4).Concat(buffer.Take(4)).ToArray(), recBytes, SocketFlags.None, remoteEP); //mirror
				count++;
			}
		}

		public void Dispose()
		{
			serverSocket.Close();
			Console.WriteLine("Received {0} packets", count);
		}
	}
}
