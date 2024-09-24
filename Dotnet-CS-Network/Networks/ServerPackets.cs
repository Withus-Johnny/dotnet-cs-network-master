using Networks;

namespace ServerPackets
{
	public sealed class Connected : Packet
	{
		public override short Index
		{
			get { return (short)ServerPacketIds.Connected; }
		}

		protected override void ReadPacket(BinaryReader reader)
		{
		}

		protected override void WritePacket(BinaryWriter writer)
		{
		}
	}

	public sealed class Disconnect : Packet
	{
		public override short Index
		{
			get { return (short)ServerPacketIds.Disconnect; }
		}

		public byte Reason;

		protected override void ReadPacket(BinaryReader reader)
		{
			Reason = reader.ReadByte();
		}

		protected override void WritePacket(BinaryWriter writer)
		{
			writer.Write(Reason);
		}
	}

	public sealed class KeepAlive : Packet
	{
		public override short Index
		{
			get { return (short)ServerPacketIds.KeepAlive; }
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
