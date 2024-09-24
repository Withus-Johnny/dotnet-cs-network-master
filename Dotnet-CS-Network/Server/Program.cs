using Networks;

namespace Server
{
	internal class Program
	{
		public static Envir Envir { get; private set; }
		static void Main(string[] args)
		{
			Packet.IsServer = true;

			Console.WriteLine("SERVER START");
			Envir = new Envir();
			Envir.Start();
			Console.WriteLine("SERVER CLOSED");
			Console.ReadKey();
		}
	}
}
