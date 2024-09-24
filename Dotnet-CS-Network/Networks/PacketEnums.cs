namespace Networks
{
	public enum ServerPacketIds : short
	{
		Connected,
		Disconnect,
		KeepAlive
	}

	public enum ClientPacketIds : short
	{
		Disconnect,
		KeepAlive
	}
}
