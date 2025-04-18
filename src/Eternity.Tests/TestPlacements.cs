namespace Eternity.Tests
{
	[TestClass]
	public sealed class TestPlacements
	{
		const string source_4x4 = "xaax,xaba,xcda,xxac,abex,bddb,dfgd,axef,ebex,dhgb,gfhh,exef,ecxx,gcxc,hcxc,exxc";
		const string source_6x6 = "xaax,xaba,xcba,xadc,xeba,xxce,afex,bdgf,bfhd,dbbf,bgdb,cxcg,edcx,ghhd,hgfh,bdfg,dhgd,cxch,cfex,hhff,fgdh,fbbg,gbdb,cxeb,efex,ffhf,dhhf,bdgh,dggd,excg,ecxx,haxc,hexa,gcxe,gaxc,cxxa";

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
		public void Solve_6x6()
		{
			var pieces = PieceTextReader.Parse(source_6x6);
			var sequenceSpecs = new SequenceSpecs(36);
			IReadOnlyList<int> sequence = [
				0x00, 0x00, 0x00, 0x00,
				0x00, 0x05, 0x0a, 0x0F,
				0x14, 0x19, 0x18, 0x17,
				0x16, 0x15, 0x14, 0x0F,
				0x0A, 0x05, 0x00, 0x00,
				0x00, 0x00, 0x00, 0x00,
				0x00, 0x00, 0x00, 0x00,
				0x00, 0x00, 0x00, 0x00,
				0x00, 0x00, 0x00
			];
			var pieceOrder = Sequence.GeneratePieceIndexes(sequence).ToArray();
			Placements? placements = Placements.CreateInitial(pieces);
			for(int i = 0; i < pieceOrder.Length; ++i)
			{
				var pieceIndex = pieceOrder[i];
				if (placements != null)
				{
					var newPlacements = PuzzleSolver.TryAddPiece(placements, i, pieceIndex);
					if (newPlacements == null)
					{
						Assert.AreEqual(24, i);
						var count = placements.Values.Where(v => v != null).Count();
						Assert.AreEqual(36, count);
						// Doing it again
						newPlacements = PuzzleSolver.TryAddPiece(placements, i, pieceIndex);
						break;
					}
					placements = newPlacements;


				}

			}
		}

		[TestMethod]
		public void InitialConstraints()
		{
			var pieces = PieceTextReader.Parse(source_4x4);
			Placements? placements = Placements.CreateInitial(pieces);
			var topRightIndex = 3;
			var topRightPosition = placements.Positioner.PositionLookup[topRightIndex];
			Assert.AreEqual(topRightPosition, new Position(3, 0));
			var constraint = placements.Constraints.At(topRightPosition);
			var patternConstraint = constraint.PatternConstraints;
			Assert.AreEqual(1, patternConstraint.Right.Count);

			var rightMiddlePosition = new Position(3, 2);
			var rightMiddle = placements.Positioner.ReversePositionLookup[rightMiddlePosition];
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
					constraint = placements.Constraints.At(rightMiddlePosition);
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
