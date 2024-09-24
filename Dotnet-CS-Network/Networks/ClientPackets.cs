using Networks;

namespace ClientPackets
{
	public sealed class Disconnect : Packet
	{
		public override short Index
		{
			get { return (short)ClientPacketIds.Disconnect; }
		}
		protected override void ReadPacket(BinaryReader reader)
		{
		}
		protected override void WritePacket(BinaryWriter writer)
		{
		}
	}

	public sealed class KeepAlive : Packet
	{
		public override short Index
		{
			get { return (short)ClientPacketIds.KeepAlive; }
		}
		public long Time;
		protected override void ReadPacket(BinaryReader reader)
		{
			Time = reader.ReadInt64();
		}
		protected override void WritePacket(BinaryWriter writer)
		{
			writer.Write(Time);
		}
	}
}
