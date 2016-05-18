using System;

namespace Primer
{
    public class SimpleClient
    {
		static Session s;
		public static void Load()
		{
			Console.WriteLine("load");
			var task = Session<UTF8StringRequest>.Connect(new Session.Settings("localhost", 12306), 0);
			task.Wait();
			s = task.Result;
			UTF8StringRequest request = new UTF8StringRequest();
			Console.OnInput += (text) =>
			{
				request.Data = text;
				request.Send(s);
				s.Flush();
			};
		}

		public static void Unload()
		{
			Console.WriteLine("unload");
			s.Close();
		}
    }
}
