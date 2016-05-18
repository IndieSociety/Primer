using System;
using System.Net;

namespace Primer
{
	public static class Tools
	{
		public static IPAddress ToIPAddress(this string ip)
		{
			string[] parts = ip.Split('.');
			byte[] result = new byte[parts.Length];
			for (int i = 0; i < parts.Length; ++i)
			{
				result[i] = Convert.ToByte(parts[i]);
			}
			return new IPAddress(result);
		}
	}
}
