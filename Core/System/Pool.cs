using System.Collections.Concurrent;

namespace Primer
{
	public sealed class Pool<T> where T : class, new()
	{
		private readonly ConcurrentStack<T> stack;

		public Pool()
		{
			stack = new ConcurrentStack<T>();
		}

		public T Acquire()
		{
			T result;
			if (!stack.TryPop(out result))
				result = new T();
			return result;
		}

		public void Release(T item)
		{
			stack.Push(item);
		}
	}
}
