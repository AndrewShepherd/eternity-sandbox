using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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

		public bool IsEquivalentTo(ImmutableBitArray other)
		{
			if (ReferenceEquals(this, other))
			{
				return true;
			}
			if (this._count != other._count)
			{
				return false;
			}
			if (this._singleValue.HasValue && other._singleValue.HasValue)
			{
				return this._singleValue.Value == other._singleValue.Value;
			}
			if (this._singleValue.HasValue && other._bitArray != null)
			{
				return other._bitArray[this._singleValue.Value];
			}
			if (other._singleValue.HasValue && this._bitArray != null)
			{
				return this._bitArray[other._singleValue.Value];
			}
			if ((this._bitArray != null) && (other._bitArray != null))
			{
				for(int i = 0; i < this._bitArray.Length; ++i)
				{
					if (this._bitArray[i] != other._bitArray[i])
					{
						return false;
					}
				}
				return true;
			}
			else
			{
				return false;
			}

		}
	}
}
