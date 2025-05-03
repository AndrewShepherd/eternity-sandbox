using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eternity
{
	public sealed class ImmutableBitArray : IEnumerable<int>
	{
		private int? _singleValue;
		private BitArray? _bitArray;
		private int _count;

		public static ImmutableBitArray SingleValue(int value)
		{
			return new ImmutableBitArray
			{
				_singleValue = value,
				_bitArray = null,
				_count = 1
			};
		}

		public static ImmutableBitArray AllPieces(int length)
		{
			var bitArray = new BitArray(length);
			bitArray.SetAll(true);
			return new ImmutableBitArray
			{
				_bitArray = bitArray,
				_count = length
			};
		}

		public static ImmutableBitArray Empty =>
			new ImmutableBitArray
			{
				_count = 0
			};

		public IEnumerator<int> GetEnumerator()
		{
			if (_singleValue.HasValue)
			{
				yield return _singleValue.Value;
			}
			else if (_bitArray != null)
			{
				for(int i = 0; i < _bitArray.Length; ++i)
				{
					if (_bitArray[i])
					{
						yield return i;
					}
				}
			}
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public int Count => _count;

		public bool Contains(int index)
		{
			if (_singleValue.HasValue)
			{
				return _singleValue == index;
			}
			if (_bitArray != null)
			{
				if (_bitArray.Length <= index)
				{
					return false;
				}
				if (_bitArray[index])
				{
					return true;
				}
			}
			return false;

		}

		public ImmutableBitArray Remove(int index)
		{
			if (_singleValue.HasValue)
			{
				return (_singleValue.Value == index)
					? Empty
					: this;
			}
			if (_bitArray != null)
			{
				if (_bitArray.Length <= index)
				{
					return this;
				}
				if (_bitArray[index])
				{
					var newBitArray = new BitArray(_bitArray);
					newBitArray.Set(index, false);
					return new ImmutableBitArray
					{ 
						_bitArray = newBitArray,
						_count = this._count - 1,
					};
				}
			}
			return this;
		}
	}

	public static class ImmutableBitArrayExtensions
	{
		public static bool IsEquivalentTo(this ImmutableBitArray a1, ImmutableBitArray a2)
		{
			if (object.ReferenceEquals(a1, a2))
			{
				return true;
			}
			var e1 = a1.GetEnumerator();
			var e2 = a2.GetEnumerator();
			while(true)
			{
				bool e1Next = e1.MoveNext();
				bool e2Next = e2.MoveNext();
				if (e1Next != e2Next)
				{
					return false;
				}
				if (!e1Next)
				{
					break;
				}
				if (e1.Current != e2.Current)
				{
					return false;
				}
			}
			return true;
		}
	}
}
