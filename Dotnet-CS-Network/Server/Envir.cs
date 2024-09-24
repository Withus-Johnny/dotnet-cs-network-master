using Server.Connections;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace Server
{
	public class Envir
	{
		public bool Running { get; set; }
		public double RunningTime { get; set; }
		public long Time { get; set; }
		public DateTime Now => _startTime.AddMilliseconds(Time);

		private TcpListener _listener;
		private DateTime _startTime;
		private Stopwatch _stopWatch;

		private int _sessionID;

		public List<ClientInfo> ClientsList;

		public void Start()
		{
			if (Running) return;

			Running = true;

			_stopWatch = Stopwatch.StartNew();
			Time = _stopWatch.ElapsedMilliseconds;
			_startTime = DateTime.Now;

			ClientsList = new List<ClientInfo>();

			_listener = new TcpListener(IPAddress.Parse(Settings.IPAddress), Settings.Port);
			_listener.Start();
			_listener.BeginAcceptTcpClient(Connection, null);

			WorkLoop();
		}

		private void Connection(IAsyncResult result)
		{
			if (!Running) return;

			try
			{
				var tempTcpClient = _listener.EndAcceptTcpClient(result);
				var ipAddress = tempTcpClient.Client.RemoteEndPoint.ToString().Split(':')[0];

				int connCount = 0;

				// ++i 최적화 테스트 나중에 하기
				for (int i = 0; i < ClientsList.Count; i++)
				{
					var connection = ClientsList[i];

					if (!connection.Connected || connection.IPAddress != ipAddress)
						continue;

					connCount++;
				}

				if (connCount >= Settings.MaxIP)
				{
					var alreadyConn = ClientsList.FirstOrDefault(x => x.IPAddress == ipAddress);
					if (alreadyConn != null)
					{
						Console.WriteLine($"{alreadyConn.IPAddress} 최대 연결 제한으로 기존 연결을 해제합니다.");
						// 미구현
					}
				}

				var tempConnection = new ClientInfo(++_sessionID, tempTcpClient);
				lock (ClientsList)
				{
					ClientsList.Add(tempConnection);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("==============");
				Console.WriteLine(ex.Message);
				Console.WriteLine("==============");
			}
			finally
			{
				if (Running && _listener.Server.IsBound)
					_listener.BeginAcceptTcpClient(Connection, null);
			}
		}

		private void WorkLoop()
		{
			while (Running)
			{
				try
				{
					Time = _stopWatch.ElapsedMilliseconds;
					RunningTime = (Now - _startTime).TotalSeconds;

					lock (ClientsList)
					{
						for (var i = ClientsList.Count - 1; i >= 0; i--)
						{
							ClientsList[i].Process();
						}
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine("==============");
					Console.WriteLine(ex.Message);
					Console.WriteLine("==============");
				}
			}
		}
	}
}
