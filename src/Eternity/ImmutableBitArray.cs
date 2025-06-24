namespace Eternity
{
	using System.Collections;

	public sealed class ImmutableBitArray : IEnumerable<int>
	{
		private int? _singleValue;
		private BitArray? _bitArray;
		private int _count;

		public static ImmutableBitArray SingleValue(int value) =>
			 new ImmutableBitArray
			{
				_singleValue = value,
				_bitArray = null,
				_count = 1
			};

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
				return (_bitArray.Length > index) && _bitArray[index];
			}
			return false;
		}

		public ImmutableBitArray Add(int index)
		{
			if (Contains(index))
			{
				return this;
			}
			if (IsEquivalentTo(Empty))
			{
				return new ImmutableBitArray
				{
					_singleValue = index,
					_count = 1
				};
			}
			if (_singleValue.HasValue)
			{
				var newBitArray = new BitArray(Math.Max(_singleValue.Value, index) + 1);
				newBitArray[index] = true;
				newBitArray[_singleValue.Value] = true;
				return new ImmutableBitArray
				{
					_bitArray = newBitArray,
					_count = 2
				};
			}
			if (_bitArray == null)
			{
				throw new Exception("Invalid state of ImmutableBitArray");
			}
			if (_bitArray.Length <= index)
			{
				var newBitArray = new BitArray(index + 1);
				newBitArray.Set(index, true);
				for(int i = 0; i <_bitArray.Length; ++i)
				{
					if (_bitArray[i])
					{
						newBitArray.Set(i, true);
					}
				}
				return new ImmutableBitArray
				{
					_bitArray = newBitArray,
					_count = this._count + 1
				};
			}
			else
			{
				var newBitArray = (BitArray)this._bitArray.Clone();
				newBitArray.Set(index, true);
				return new ImmutableBitArray
				{
					_bitArray = newBitArray,
					_count = this._count + 1
				};
			}
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
					var newBitArray = (BitArray)_bitArray.Clone();
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

		public ImmutableBitArray Intersect(ImmutableBitArray other)
		{
			if (IsEquivalentTo(other))
			{
				return this;
			}
			if (this._singleValue.HasValue)
			{
				return other.Contains(this._singleValue.Value)
					? this
					: Empty;
			}
			if (other._singleValue.HasValue)
			{
				return this.Contains(other._singleValue.Value)
					? other
					: Empty;
			}
			if ((this._bitArray != null) && (other._bitArray != null))
			{
				if (this._bitArray.Length == other._bitArray.Length)
				{
					var newBitArray = (BitArray)this._bitArray.Clone();
					newBitArray.And(other._bitArray);
					var newCount = 0;
					for (int i = 0; i < newBitArray.Length; ++i)
					{
						if (newBitArray[i])
						{
							++newCount;
						}
					}
					return new ImmutableBitArray
					{
						_bitArray = newBitArray,
						_count = newCount
					};
				}
				else
				{
					(var larger, var smaller) = this._bitArray.Length > other._bitArray.Length
						? (this._bitArray, other._bitArray)
						: (other._bitArray, this._bitArray);
					var newBitArray = (BitArray)smaller.Clone();
					int newCount = 0;
					int lastTrueIndex = -1;
					for(int i = 0; i < newBitArray.Length; ++i)
					{
						if (!larger[i])
						{
							newBitArray[i] = false;
						}
						else
						{
							if (newBitArray[i])
							{
								++newCount;
								lastTrueIndex = i;
							}
						}
					}
					return newCount switch
					{
						0 => Empty,
						1 => new ImmutableBitArray { _singleValue = lastTrueIndex, _count = 1 },
						_ => new ImmutableBitArray
						{
							_bitArray = newBitArray,
							_count = newCount
						}
					};
				}
			}
			return Empty;
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
			if (this._count == 0 && other._count == 0)
			{
				return true;
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
				for(int i = 0; i < Math.Max(this._bitArray.Length, other._bitArray.Length); ++i)
				{
					var l = i < this._bitArray.Length && this._bitArray[i];
					var r = i < other._bitArray.Length && other._bitArray[i];
					if (l != r)
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
