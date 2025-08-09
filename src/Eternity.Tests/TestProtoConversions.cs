using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Eternity.Tests
{
	[TestClass]
	public sealed class TestProtoConversions
	{
		[TestMethod]
		public void TestBigInt()
		{
			var bigInt = new BigInteger(98765432);
			var proto = ProtoConversions.Convert(bigInt);
			var bigInt2 = ProtoConversions.Convert(proto);
			Assert.AreEqual(bigInt, bigInt2);
		}
	}
}
