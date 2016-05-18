using System;
using System.Threading;
using System.Collections.Concurrent;

namespace Primer
{
	public class Loop
	{
		private static readonly ThreadLocal<Loop> _current = new ThreadLocal<Loop>();

		public static Loop Current
		{
			get
			{
				Loop loop = _current.Value;
				if (loop == null)
				{
					loop = new Loop();
					_current.Value = loop;
				}
				return loop;
			}
		}

		private readonly int threadId;
		private int taskcount;
		private readonly ConcurrentStack<Schedule> newschedules;
		private readonly Heap<Schedule> schedules;
		private readonly Schedule[] popschedules;
		private readonly AutoResetEvent signal;
		private readonly ConcurrentQueue<Action> actions;

		private Loop()
		{
			threadId = Thread.CurrentThread.ManagedThreadId;
			taskcount = 0;
			newschedules = new ConcurrentStack<Schedule>();
			schedules = new Heap<Schedule>();
			popschedules = new Schedule[256];
			signal = new AutoResetEvent(false);
			actions = new ConcurrentQueue<Action>();
		}

		public void Execute(Action action)
		{
			if (Thread.CurrentThread.ManagedThreadId == threadId)
			{
				action();
			}
			else
			{
				actions.Enqueue(action);
				signal.Set();
			}
		}

		public void Retain()
		{
			Interlocked.Increment(ref taskcount);
		}

		public void Release()
		{
			Interlocked.Decrement(ref taskcount);
		}

		public void Run()
		{
			if (Thread.CurrentThread.ManagedThreadId != threadId)
				return;//todo throw new 
			while (true)
			{
				Action action;
				while (actions.TryDequeue(out action))
				{
					try
					{
						action();
					}
					catch (Exception e)
					{
						Log.Error(e);
					}
				}
				while (true)
				{
					int nums = newschedules.TryPopRange(popschedules);
					for (int i = 0; i < nums; ++i)
						schedules.Add(popschedules[i]);
					if (nums != popschedules.Length)
						break;
				}
				if (taskcount == 0 && schedules.Count == 0)
					break;
				int time = Timeout.Infinite;
				if (schedules.Count > 0)
					time = schedules.Top.GetHashCode();
				signal.WaitOne(time);
			}
		}

		public void Update()
		{
			if (Thread.CurrentThread.ManagedThreadId == threadId)
			{
				Action action;
				while (actions.TryDequeue(out action))
				{
					try
					{
						action();
					}
					catch (Exception e)
					{
						Log.Error(e);
					}
				}
			}
		}
	}

	public class Schedule
	{
		//public int
	}
}
