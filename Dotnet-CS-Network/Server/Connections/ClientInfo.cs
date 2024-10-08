﻿using Networks;
using System.Collections.Concurrent;
using System.Net.Sockets;
using S = ServerPackets;
using C = ClientPackets;

namespace Server.Connections
{
	public class ClientInfo
	{
		public DateTime ConnectedDateTime;
		public long TimeDisconnected, TimeOutTime;
		public DateTime LastKeepAliveDateTime;
		public readonly int SessionID;
		public readonly string IPAddress;

		public bool Connected;

		private bool _disconnecting;

		private TcpClient _client;
		private ConcurrentQueue<Packet> _receiveList;
		private ConcurrentQueue<Packet> _sendList;
		private Queue<Packet> _retryList;

		private DateTime _dataCounterReset;
		private int _dataCounter;
		private FixedSizedQueue<Packet> _lastPackets;

		byte[] _rawData = new byte[0];
		byte[] _rawBytes = new byte[8 * 1024];

		public bool Disconnecting
		{
			get { return _disconnecting; }
			set
			{
				if (_disconnecting == value) return;
				_disconnecting = value;

				TimeOutTime = Program.Envir.Time + Settings.TimeOut;
			}
		}

		public ClientInfo(int sessionID, TcpClient client)
		{
			SessionID = sessionID;
			IPAddress = client.Client.RemoteEndPoint.ToString().Split(':')[0];

			Console.WriteLine($"[{DateTime.Now:yy.MM.dd HH:mm:ss}] > [ CONNECTED ] < [SESSION:{SessionID}] [IP:{IPAddress}]");

			_client = client;
			_client.NoDelay = true;

			ConnectedDateTime = DateTime.Now;
			TimeOutTime = Program.Envir.Time + Settings.TimeOut;

			_lastPackets = new FixedSizedQueue<Packet>(10);

			_receiveList = new ConcurrentQueue<Packet>();
			_sendList = new ConcurrentQueue<Packet>();
			_sendList.Enqueue(new S.Connected());

			_retryList = new Queue<Packet>();
			Connected = true;
			BeginReceive();
		}

		#region RECEIVE

		private void BeginReceive()
		{
			if (!Connected) return;

			try
			{
				_client.Client.BeginReceive(_rawBytes, 0, _rawBytes.Length, SocketFlags.None, ReceiveData, _rawBytes);
			}
			catch
			{
				Disconnecting = true;
			}
		}

		private void ReceiveData(IAsyncResult result)
		{
			if (!Connected) return;

			int dataRead;

			try
			{
				dataRead = _client.Client.EndReceive(result);
			}
			catch
			{
				Disconnecting = true;
				return;
			}

			if (dataRead == 0)
			{
				Disconnecting = true;
				return;
			}

			if (_dataCounterReset < Program.Envir.Now)
			{
				_dataCounterReset = Program.Envir.Now.AddSeconds(5);
				_dataCounter = 0;
			}

			_dataCounter++;

			try
			{
				byte[] rawBytes = result.AsyncState as byte[];

				byte[] temp = _rawData;
				_rawData = new byte[dataRead + temp.Length];
				Buffer.BlockCopy(temp, 0, _rawData, 0, temp.Length);
				Buffer.BlockCopy(rawBytes, 0, _rawData, temp.Length, dataRead);

				Packet p;

				while ((p = Packet.ReceivePacket(_rawData, out _rawData)) != null)
					_receiveList.Enqueue(p);
			}
			catch
			{
				Console.WriteLine($"{IPAddress} 연결 해제 | 알 수 없는 패킷 감지");
				Disconnecting = true;
				return;
			}

			if (_dataCounter > Settings.MaxPacket)
			{
				List<string> packetList = new List<string>();

				while (_lastPackets.Count > 0)
				{
					_lastPackets.TryDequeue(out Packet pkt);

					Enum.TryParse<ClientPacketIds>((pkt?.Index ?? 0).ToString(), out ClientPacketIds cPacket);

					packetList.Add(cPacket.ToString());
				}

				Console.WriteLine($"{IPAddress} 연결 해제 | 패킷 수용 범위를 초과 했음 | {string.Join(",", packetList.Distinct())}");
				Disconnecting = true;
				return;
			}

			BeginReceive();
		}
		#endregion

		#region SEND
		private void BeginSend(List<byte> data)
		{
			if (!Connected || data.Count == 0) return;

			//Interlocked.Add(ref Network.Sent, data.Count);

			try
			{
				_client.Client.BeginSend(data.ToArray(), 0, data.Count, SocketFlags.None, SendData, Disconnecting);
			}
			catch
			{
				Disconnecting = true;
			}
		}

		private void SendData(IAsyncResult result)
		{
			try
			{
				if (_client != null)
				{
					_client.Client.EndSend(result);
				}
			}
			catch
			{ }
		}
		#endregion

		public void Process()
		{
			if (_client == null || !_client.Connected)
			{
				Disconnect(DisconnectReason.ClientShutDown);
				return;
			}

			while (!_receiveList.IsEmpty && !Disconnecting)
			{
				Packet p;
				if (!_receiveList.TryDequeue(out p)) continue;

				_lastPackets.Enqueue(p);

				TimeOutTime = Program.Envir.Time + Settings.TimeOut;
				ProcessPacket(p);

				if (_receiveList == null)
					return;
			}

			while (_retryList.Count > 0)
				_receiveList.Enqueue(_retryList.Dequeue());

			if (Program.Envir.Time > TimeOutTime)
			{
				Disconnect(DisconnectReason.TimeOut);
				return;
			}

			if (_sendList == null || _sendList.Count <= 0) return;

			List<byte> data = new List<byte>();

			while (!_sendList.IsEmpty)
			{
				Packet p;
				if (!_sendList.TryDequeue(out p) || p == null) continue;
				data.AddRange(p.GetPacketBytes());
			}

			BeginSend(data);
		}

		private void ProcessPacket(Packet p)
		{
			if (p == null || Disconnecting) return;

			switch (p.Index)
			{
				case (short)ClientPacketIds.Disconnect:
					Disconnect(DisconnectReason.ClientExit);
					break;
				case (short)ClientPacketIds.KeepAlive:
					ClientKeepAlive((C.KeepAlive)p);
					break;
			}
		}

		private void ClientKeepAlive(C.KeepAlive p)
		{
			Console.WriteLine($"[{DateTime.Now:yy.MM.dd HH:mm:ss}] > [ KEEPALIVE ] < [SESSION:{SessionID}] [IP:{IPAddress}]");
			LastKeepAliveDateTime = DateTime.Now;
			Enqueue(new S.KeepAlive
			{
				Time = p.Time
			});
		}

		public void Disconnect(DisconnectReason reasonType)
		{
			if (!Connected) return;

			string outReason = string.Empty;

			switch (reasonType)
			{
				case DisconnectReason.ClientShutDown:
					outReason = "SHUTDOWN";
					break;
				case DisconnectReason.TimeOut:
					outReason = "TIMEOUT";
					break;
				case DisconnectReason.ClientExit:
					outReason = "CLOSED";
					break;
				case DisconnectReason.MaxLoginAttempts:
					outReason = "MaxLoginAttempts";
					break;
				default:
					outReason = $"DEFAULT({reasonType})";
					break;
			}

			string outMsg = $"[{DateTime.Now:yy.MM.dd HH:mm:ss}] > [ DISCONNECT({outReason}) ] < [SESSION:{SessionID}] [IP:{IPAddress}]";
			Connected = false;
			TimeDisconnected = Program.Envir.Time;


			lock (Program.Envir.ClientsList)
			{
				Program.Envir.ClientsList.Remove(this);
			}

			_sendList = null;
			_receiveList = null;
			_retryList = null;
			_rawData = null;

			if (_client != null) _client.Client.Dispose();
			_client = null;

			Console.WriteLine(outMsg);
		}

		public void Enqueue(Packet p)
		{
			if (p == null) return;
			if (_sendList != null && p != null)
			{
				_sendList.Enqueue(p);
			}
		}
	}
}
