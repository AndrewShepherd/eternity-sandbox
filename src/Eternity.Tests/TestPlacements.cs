namespace Eternity.Tests
{
	[TestClass]
	public sealed class TestPlacements
	{
		[TestMethod]
		public void TestCreateInitial()
		{
			var source = "xaax,xaba,xcda,xxac,abex,bddb,dfgd,axef,ebex,dhgb,gfhh,exef,ecxx,gcxc,hcxc,exxc";
			var pieces = PieceTextReader.Parse(source);
			Assert.AreEqual(pieces.Length, 16);
			var placements = Placements.CreateInitial(pieces);
			Assert.AreEqual(16, placements.Values.Count);
			var newPlacements = PuzzleSolver.TryAddPiece(
				placements,
				0,
				0
			);
			Assert.IsNotNull(newPlacements);
		}
	}
}
