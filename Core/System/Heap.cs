using System.Threading;
using System.Collections.Generic;

namespace Primer
{
	public class Heap<T>
	{
		private struct Data
		{
			public T instance;
			public int serial;

			public static int index = 0;
		}
		private class DataCompare : IComparer<Data>
		{
			private readonly IComparer<T> comparer;

			public DataCompare(IComparer<T> comparer)
			{
				this.comparer = comparer;
			}

			public int Compare(Data x, Data y)
			{
				int result = comparer.Compare(x.instance, y.instance);
				if (result != 0)
					return result;
				return x.serial - y.serial;
			}

			public static readonly DataCompare Default = new DataCompare(Comparer<T>.Default);
		}

		private readonly SortedSet<Data> list;
		private readonly Dictionary<T, int> serials;

		public Heap()
		{
			list = new SortedSet<Data>(DataCompare.Default);
			serials = new Dictionary<T, int>();
		}

		public Heap(IComparer<T> comparer)
		{
			list = new SortedSet<Data>(new DataCompare(comparer));
			serials = new Dictionary<T, int>();
		}

		public T Top
		{
			get
			{
				return list.Min.instance;
			}
		}

		public int Count
		{
			get
			{
				return list.Count;
			}
		}

		public void Add(T t)
		{
			int serial;
			if (serials.TryGetValue(t, out serial))
			{
				list.Remove(new Data { instance = t, serial = serial });
			}
			else
			{
				serial = Interlocked.Increment(ref Data.index);
				serials.Add(t, serial);
			}
			list.Add(new Data { instance = t, serial = serial });
		}

		public T Pop()
		{
			Data data = list.Min;
			list.Remove(data);
			serials.Remove(data.instance);
			return data.instance;
		}

		public void Remove(T t)
		{
			int serial;
			if (serials.TryGetValue(t, out serial))
			{
				list.Remove(new Data { instance = t, serial = serial });
				serials.Remove(t);
			}
		}
	}
}
