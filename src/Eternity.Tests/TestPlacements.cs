namespace Eternity.Tests
{
	[TestClass]
	public sealed class TestPlacements
	{
		const string source_4x4 = "xaax,xaba,xcda,xxac,abex,bddb,dfgd,axef,ebex,dhgb,gfhh,exef,ecxx,gcxc,hcxc,exxc";
		[TestMethod]
		public void TestCreateInitial()
		{
			var pieces = PieceTextReader.Parse(source_4x4);
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

		[TestMethod]
		public void InitialConstraints()
		{
			var pieces = PieceTextReader.Parse(source_4x4);
			Placements? placements = Placements.CreateInitial(pieces);
			var topRightIndex = 3;
			var topRightPosition = placements.Positioner.PositionLookup[topRightIndex];
			Assert.AreEqual(topRightPosition, new Position(3, 0));
			var constraint = placements.Constraints[topRightIndex];
			var patternConstraint = constraint.PatternConstraints;
			Assert.AreEqual(1, patternConstraint.Right.Count);

			var rightMiddle = placements.Positioner.ReversePositionLookup[new Position(3, 2)];
			int i = 0;
			while(placements != null)
			{
				placements = PuzzleSolver.TryAddPiece(
					placements,
					i,
					i
				);
				if (placements != null)
				{
					constraint = placements.Constraints[rightMiddle];
					patternConstraint = constraint.PatternConstraints;
					Assert.AreEqual(
						1,
						patternConstraint.Right.Count,
						$"Fails after piece {i}"
					);
				}
				i += 1;
			}
		}
	}
}
