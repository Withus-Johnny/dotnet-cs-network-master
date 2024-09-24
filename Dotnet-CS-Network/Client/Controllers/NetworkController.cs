using Networks;
using System.Diagnostics;
using S = ServerPackets;
using C = ClientPackets;

namespace Client.Controllers
{
	public class NetworkController
	{
		private static NetworkController instance = new NetworkController();
		public static NetworkController Instance { get { return instance; } }

		private Thread _networkThread;
		public bool isRunning { get; set; }

		public Stopwatch Timer = Stopwatch.StartNew();
		public DateTime StartTime = DateTime.UtcNow;
		public DateTime Now { get { return StartTime.AddMilliseconds(Time); } }
		public static long Time;

		public static double BytesSent, BytesReceived;

		public void InitializeNetwork()
		{
			_networkThread = new Thread(new ThreadStart(Running));
			_networkThread.IsBackground = true;
			_networkThread.Start();
		}

		private void Running()
		{
			Timer = Stopwatch.StartNew();
			StartTime = DateTime.UtcNow;

			isRunning = true;
			Network.Connect();

			try
			{
				while (isRunning)
				{
					Time = Timer.ElapsedMilliseconds;
					Network.Process();
				}

				Time = 0;
				BytesSent = 0;
				BytesReceived = 0;
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
		}

		public void Stop()
		{
			try
			{
				if (isRunning)
				{
					Network.Enqueue(new C.Disconnect());
				}

				isRunning = false;
			}
			catch (Exception ex) { Console.WriteLine(ex.Message); }
		}

		public void ProcessPacket(Packet p)
		{
			switch (p.Index)
			{
				case (short)ServerPacketIds.Connected:
					Network.Connected = true;
					Console.WriteLine("Network Connected");
					break;
				case (short)ServerPacketIds.KeepAlive:
					KeepAlive((S.KeepAlive)p);
					break;
				default:
					Console.WriteLine($"미개발 : {p.Index}");
					break;
			}
		}

		private void KeepAlive(S.KeepAlive p)
		{
			if (p.Time == 0) return;
			Network.PingTime = (Time - p.Time);
			Console.WriteLine($"S.KeepAlive: {Network.PingTime}");
		}
	}
}
