using System;

namespace Primer
{
    public class SimpleServer
    {
		static Server s;
		public static void Load()
		{
			Console.WriteLine("load");
			s = new Server<UTF8StringRequest>(new Server.Settings(12306));
			UTF8StringRequest.DefaultHandler += delegate(Session session, string str)
			{
				Console.WriteLine(str);
				
			};
			s.Start();
		}

		public static void Unload()
		{
			Console.WriteLine("unload");
			s.Stop();
		}
    }
}
