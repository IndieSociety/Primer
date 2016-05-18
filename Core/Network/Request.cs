﻿using System;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Primer
{
	public class RequestException : SystemException
	{
		public RequestException(int n)
			: base(string.Format("Request parse error {0}", n)) { }
	}

	public abstract class Request
	{
		/// <summary>
		/// 请求接收缓冲区
		/// </summary>
		private class ByteBuffer
		{
			private byte[] _array;
			public byte[] array
			{
				get { return _array; }
			}
			private int _length;
			public int length
			{
				get { return _length; }
			}
			private int _offset;
			public int offset
			{
				get { return _offset; }
			}

			private ByteBuffer()
			{
				_array = new byte[1024];
				_length = 0;
				_offset = 0;
			}

			public void Write(byte[] bytes)
			{
				Write(bytes, 0, bytes.Length);
			}

			public void Write(byte[] bytes, int length)
			{
				Write(bytes, 0, length);
			}

			private void checksize(int length)
			{
				if (_offset + _length + length > _array.Length)
				{
					int need = _length + length;
					if (need <= _array.Length && _offset >= (_array.Length + 1) >> 1)
					{
						Array.Copy(_array, _offset, _array, 0, _length);
						_offset = 0;
					}
					else
					{
						int size = _array.Length << 1;
						while (size < need)
						{
							size = size << 1;
						}
						byte[] array = new byte[size];
						Array.Copy(_array, _offset, array, 0, _length);
						_offset = 0;
						_array = array;
					}
				}
			}

			public void Write(byte[] bytes, int offset, int length)
			{
				checksize(length);
				Array.Copy(bytes, offset, _array, _offset + _length, length);
				_length += length;
			}

			public void Pop(int length)
			{
				if (length > _length)
					length = _length;
				_offset += length;
				_length -= length;
				if (_length == 0)
				{
					Reset();
				}
			}

			public void Reset()
			{
				_offset = 0;
				_length = 0;
			}

			public static ConcurrentStack<ByteBuffer> freelist = new ConcurrentStack<ByteBuffer>();

			public static ByteBuffer New()
			{
				ByteBuffer result;
				if (!freelist.TryPop(out result))
					result = new ByteBuffer();
				return result;
			}

			public void Release()
			{
				freelist.Push(this);
			}
		}

		private readonly ByteBuffer buffer;

		protected Request()
		{
			buffer = ByteBuffer.New();
		}

		~Request()
		{
			buffer.Release();
		}

		internal bool Input(byte[] bytes, int offset, ref int length)
		{
			int len = length;
			while (len > 0)
			{
				int l = PreTest(bytes, offset, len);
				buffer.Write(bytes, offset, l);
				offset += l;
				len -= l;
				int tl = Test(buffer.array, buffer.offset, buffer.length);
				if (tl > 0)
				{
					buffer.Pop(tl);
					length -= len;
					return true;
				}
				if (tl < 0)
				{
					length = tl;
					return true;
				}
			}
			return false;
		}

		protected virtual int PreTest(byte[] bytes, int offset, int length)
		{
			return length;
		}

		protected abstract int Test(byte[] bytes, int offset, int length);
		internal abstract void Execute();
		public abstract bool Send(Session session);
		public virtual void Reset() { }
	}

	public abstract class Request<T> : Request
	{
		public T Data;
		internal override void Execute()
		{
			Handle(Data);
		}

		protected virtual void Handle(T t)
		{
			if (DefaultHandler != null)
				DefaultHandler(t);
		}

		public static event Action<T> DefaultHandler;
	}

	public abstract class Request<TKey, T> : Request
	{
		public TKey ID;
		public T Data;
		internal override void Execute()
		{
			Handle(ID, Data);
		}

		protected virtual void Handle(TKey k, T t)
		{
			Action<T> action;
			if (_Handlers.TryGetValue(k, out action))
			{
				action(t);
			}
			else
			{
				if (DefaultHandler != null)
					DefaultHandler(k, t);
			}
		}

		private static readonly Dictionary<TKey, Action<T>> _Handlers = new Dictionary<TKey, Action<T>>();
		public static Dictionary<TKey, Action<T>> Handlers
		{
			get
			{
				return _Handlers;
			}
		}
		public static event Action<TKey, T> DefaultHandler;
	}

	public class UTF8StringRequest : Request<string>
	{
		protected override int PreTest(byte[] bytes, int offset, int length)
		{
			for (int i = offset; i < length; ++i)
			{
				if (bytes[i] == 0)
					return i - offset + 1;
			}
			return length;
		}

		protected override int Test(byte[] bytes, int offset, int length)
		{
			if (bytes[offset + length - 1] == 0)
			{
				Data = System.Text.Encoding.UTF8.GetString(bytes, offset, length - 1);
				return length;
			}
			return 0;
		}

		public override bool Send(Session session)
		{
			if (!session.Write(System.Text.Encoding.UTF8.GetBytes(Data)))
				return false;
			if (!session.Write(0))
				return false;
			return true;
		}

		public override void Reset()
		{
			Data = null;
		}
	}
}