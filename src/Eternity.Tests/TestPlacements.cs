namespace Eternity.Tests;

using static Eternity.Tests.Samples;

[TestClass]
public sealed class TestPlacements
{

	[TestMethod]
	public void TestCreateInitial()
	{
		var pieces = PieceTextReader.Parse(source_6x6);
		Assert.AreEqual(pieces.Length, 36);

		PartiallyExploredTreeNode treeNode = (PartiallyExploredTreeNode)StackEntryExtensions.CreateInitialStack(pieces);
		Assert.IsTrue(treeNode.ChildNodes[0] is UnexploredTreeNode);
		var pathOne = treeNode.Progress(pieces, [0]);
		var pathTwo = treeNode.Progress(pieces, [1]);
		Assert.IsTrue((pathOne as PartiallyExploredTreeNode)!.ChildNodes[0] is PartiallyExploredTreeNode);
		Assert.IsTrue((pathOne as PartiallyExploredTreeNode)!.ChildNodes[1] is UnexploredTreeNode);
		Assert.IsTrue((pathTwo as PartiallyExploredTreeNode)!.ChildNodes[0] is UnexploredTreeNode);
		Assert.IsTrue((pathTwo as PartiallyExploredTreeNode)!.ChildNodes[1] is PartiallyExploredTreeNode);

	}

	//[TestMethod]
	//public void Solve_6x6()
	//{
	//	var pieces = PieceTextReader.Parse(source_6x6);
	//	var sequenceSpecs = new SequenceSpecs(36);
	//	IReadOnlyList<int> sequence = [
	//		0x00, 0x00, 0x00, 0x00,
	//		0x00, 0x05, 0x0a, 0x0F,
	//		0x14, 0x19, 0x18, 0x17,
	//		0x16, 0x15, 0x14, 0x0F,
	//		0x0A, 0x05, 0x00, 0x00,
	//		0x00, 0x00, 0x00, 0x00,
	//		0x00, 0x00, 0x00, 0x00,
	//		0x00, 0x00, 0x00, 0x00,
	//		0x00, 0x00, 0x00
	//	];
	//	var pieceOrder = Sequence.GeneratePieceIndexes(sequence).ToArray();
	//	Placements? placements = Placements.CreateInitial(pieces);
	//	for(int i = 0; i < pieceOrder.Length; ++i)
	//	{
	//		var pieceIndex = pieceOrder[i];
	//		if (placements != null)
	//		{
	//			var newPlacements = PuzzleSolver.TryAddPiece(
	//				placements,
	//				placements.Positioner.PositionLookup[i],
	//				pieceIndex
	//			);
	//			if (newPlacements == null)
	//			{
	//				Assert.AreEqual(24, i);
	//				var count = placements.Values.Where(v => v != null).Count();
	//				Assert.AreEqual(36, count);
	//				// Doing it again
	//				newPlacements = PuzzleSolver.TryAddPiece(
	//					placements,
	//					placements.Positioner.PositionLookup[i],
	//					pieceIndex
	//				);
	//				break;
	//			}
	//			placements = newPlacements;


	//		}

	//	}
	//}

	//[TestMethod]
	//public void InitialConstraints()
	//{
	//	var pieces = PieceTextReader.Parse(source_4x4);
	//	Placements? placements = Placements.CreateInitial(pieces);
	//	var topRightIndex = 3;
	//	var topRightPosition = placements.Positioner.PositionLookup[topRightIndex];
	//	Assert.AreEqual(topRightPosition, new Position(3, 0));
	//	var constraint = placements.Constraints.At(topRightPosition);
	//	var patternConstraint = constraint.PatternConstraints;
	//	Assert.AreEqual(1, patternConstraint.Right.Count);

	//	var rightMiddlePosition = new Position(3, 2);
	//	int i = 0;
	//	while(placements != null)
	//	{
	//		placements = PuzzleSolver.TryAddPiece(
	//			placements,
	//			placements.Positioner.PositionLookup[i],
	//			i
	//		);
	//		if (placements != null)
	//		{
	//			constraint = placements.Constraints.At(rightMiddlePosition);
	//			patternConstraint = constraint.PatternConstraints;
	//			Assert.AreEqual(
	//				1,
	//				patternConstraint.Right.Count,
	//				$"Fails after piece {i}"
	//			);
	//		}
	//		i += 1;
	//	}
	//}
}
