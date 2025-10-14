namespace Eternity.Tests;

using System.Numerics;

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
