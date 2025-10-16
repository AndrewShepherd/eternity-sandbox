namespace Eternity;

using System.Collections.Immutable;

public record class StackEntry(
	int PieceIndex,
	int PossiblePieceCount, // the count of items that could go in that slot
	Position Position, // The position that this stack entry represents
	Placements? Placements,
	IPositioner Positioner
);


public static class StackEntryExtensions
{
	private enum ProgressionState
	{
		Initial,
		AfterFirstSuccess,
		AfterFirstFailure
	};

	public static TreeNode AsTreeNode(this StackEntry newStackEntry)
	{
		if (newStackEntry.Placements!.Values.Count == newStackEntry.Placements.PieceSides.Count)
		{
			return new FullyExploredTreeNode
			{
				NodesExplored = 1,
				Solutions = [newStackEntry.Placements]
			};
		}	
		(var nextPosition, _) = newStackEntry.Positioner.GetNext(newStackEntry.Placements!.Constraints);
		var possiblePieces = newStackEntry.Placements!.Constraints.At(nextPosition).Pieces;
		return new PartiallyExploredTreeNode
		{
			ChildNodes = possiblePieces.Select(p => UnexploredTreeNode.Instance).ToImmutableList(),
			StackEntry = newStackEntry,
			NodesExplored = 1,
			TotalNodesEstimate = null,
			Solutions = [],
		};
	}

	public static TreeNode CreateInitialStack(
		IReadOnlyList<IReadOnlyList<ulong>> pieceSides
	)
	{
		Placements initialPlacements = Placements.CreateInitial(
			pieceSides
		);
		IPositioner positioner = DefaultPositioner.Generate(initialPlacements.Dimensions);
		// Push the first value
		(var firstPosition, positioner) = positioner.GetNext(initialPlacements.Constraints);
		var availablePieces = initialPlacements.Constraints.At(firstPosition).Pieces;
		int? pieceIndex = availablePieces.Count > 0 ? 0 : null;
		if (pieceIndex.HasValue)
		{
			Placements? attempt = PuzzleSolver.TryAddPiece(
				initialPlacements,
				firstPosition,
				availablePieces.ElementAt(new Index(pieceIndex.Value))
			);
			if (attempt == null)
			{
				return new UnsuccessfulPlacementTreeNode();
			}
			var newStackEntry = new StackEntry(
				PieceIndex: pieceIndex.Value,
				PossiblePieceCount: availablePieces.Count,
				Position: firstPosition,
				Placements: attempt,
				Positioner: positioner
			);
			return newStackEntry.AsTreeNode();
		}
		return UnsuccessfulPlacementTreeNode.Instance;
	}

	public static IEnumerable<PartiallyExploredTreeNode> GetStackEntries(
		PartiallyExploredTreeNode firstElement,
		IEnumerable<int> initialPath
	)
	{
		yield return firstElement;
		var firstChild = initialPath.Any()
			? firstElement.ChildNodes.ElementAt(initialPath.FirstOrDefault()) as PartiallyExploredTreeNode
			: firstElement.ChildNodes.OfType<PartiallyExploredTreeNode>().FirstOrDefault();
		if (firstChild != null)
		{
			foreach(var childEntry in GetStackEntries(firstChild, initialPath.Skip(1)))
			{
				yield return childEntry;
			}
		}
	}

	private static TreeNode UpdateLastNode(
		List<PartiallyExploredTreeNode> l,
		Func<PartiallyExploredTreeNode, TreeNode> transform
	)
	{
		var lastNode = l.Last();
		l.RemoveAt(l.Count - 1);
		var transformedNode = transform(lastNode);
		if (l.Count > 0)
		{
			var index = l.Last().ChildNodes.IndexOf(lastNode);
			return UpdateLastNode(l, n => n.ReplaceAt(index, transformedNode));
		}
		else
		{
			return transformedNode;
		}
	}

	public static TreeNode GenerateChildNode(PartiallyExploredTreeNode setn, int index)
	{
		var thisConstraints = setn.StackEntry.Placements!.Constraints;
		(var nextPosition, var nextPositioner) = setn.StackEntry.Positioner.GetNext(thisConstraints);
		var pieces = thisConstraints.At(nextPosition).Pieces;
		var piece = pieces.ElementAt(index);
		Placements? attempt = PuzzleSolver.TryAddPiece(
			setn.StackEntry.Placements,
			nextPosition,
			piece
		);
		if (attempt == null)
		{
			return UnsuccessfulPlacementTreeNode.Instance;
		}
		else
		{
			var stackEntry = new StackEntry(
				index,
				setn.ChildNodes.Count,
				nextPosition,
				attempt,
				nextPositioner
			);
			return stackEntry.AsTreeNode();
		}
	}

	private record class ProgressToFirstSuccessResult(TreeNode treeNode, bool foundSuccess); 
	private static ProgressToFirstSuccessResult ProgressToFirstSuccess(
		PartiallyExploredTreeNode petn,
		IEnumerable<int> fixedPath
	)
	{
		int index = fixedPath.Any()
			? fixedPath.First()
			: petn.ChildNodes.Select((node, index) => (node, index))
				.Where(t => t.node is UnexploredTreeNode or PartiallyExploredTreeNode)
				.Select(t => t.index)
				.First();
		var childNode = petn[index];
		ProgressToFirstSuccessResult result = petn[index] switch
		{
			UnexploredTreeNode => GenerateChildNode(petn, index) switch
			{
				UnsuccessfulPlacementTreeNode uptn => new(uptn, false),
				TreeNode tn => new(tn, true)
			},
			PartiallyExploredTreeNode petn2 => ProgressToFirstSuccess(
				petn2,
				fixedPath.Skip(1)
			),
			TreeNode t => new(t, false)
		};
		return new(
			petn.ReplaceAt(index, result.treeNode),
			result.foundSuccess
		);
	}

	private static TreeNode ExtendThroughDefaultSelection(
		TreeNode treeNode,
		IEnumerable<int> fixedPath
	)
	{
		if (treeNode is PartiallyExploredTreeNode setn)
		{
			for (
				int index = 0;
				index < setn.ChildNodes.Count;
				++index
			)
			{
				TreeNode childNode = setn.ChildNodes[index];
				if (childNode is UnexploredTreeNode)
				{
					childNode = GenerateChildNode(setn, index);
					childNode = ExtendThroughDefaultSelection(
						childNode,
						fixedPath.Skip(1)
					);
					return setn.ReplaceAt(index, childNode);
				}
				if (childNode is PartiallyExploredTreeNode)
				{
					return setn.ReplaceAt(
						index,
						ExtendThroughDefaultSelection(
							childNode,
							fixedPath.Skip(1)
						)
					);
				}
			}
		}
		return treeNode;
	}

	public static TreeNode Progress(
		this TreeNode treeNode,
		IReadOnlyList<IReadOnlyList<ulong>> pieceSides,
		IEnumerable<int> initialFixedPath
	)
	{
		if (treeNode is UnexploredTreeNode)
		{
			treeNode = CreateInitialStack(pieceSides);
		}
		else if (treeNode is PartiallyExploredTreeNode petn)
		{
			(treeNode, bool success) = ProgressToFirstSuccess(
				petn,
				initialFixedPath
			);
		}
		return treeNode;
	}
}
