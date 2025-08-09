namespace Eternity
{
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
		public static int? ProgressForwards(int? startIndex, int count) =>
			startIndex switch
			{
				int n when n >= count - 1 => null,
				int n => n + 1,
				_ => 0
			};

		public static int? ProgressBackwards(int? startIndex, int count) =>
			startIndex switch
			{
				int n when n > 0 => n - 1,
				int n => null,
				_ => count - 1
			};

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
			Func<int?, int, int?> progressIndex,
			IReadOnlyList<ImmutableArray<int>> pieceSides
		)
		{
			Placements initialPlacements = Placements.CreateInitial(
				pieceSides
			);
			IPositioner positioner = DefaultPositioner.Generate(initialPlacements.Dimensions);
			// Push the first value
			(var firstPosition, positioner) = positioner.GetNext(initialPlacements.Constraints);
			var availablePieces = initialPlacements.Constraints.At(firstPosition).Pieces;
			var pieceIndex = progressIndex(null, availablePieces.Count);
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

		public static IEnumerable<PartiallyExploredTreeNode> GetStackEntries(PartiallyExploredTreeNode firstElement)
		{
			yield return firstElement;
			var firstChild = firstElement.ChildNodes.OfType<PartiallyExploredTreeNode>().FirstOrDefault();
			if (firstChild != null)
			{
				foreach(var childEntry in GetStackEntries(firstChild))
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
		private static ProgressToFirstSuccessResult ProgressToFirstSuccess(TreeNode treeNode, Func<int?, int, int?> progressIndex)
		{
			if (treeNode is PartiallyExploredTreeNode setn)
			{
				var childNodeCount = setn.ChildNodes.Count;
				var thisConstraints = setn.StackEntry.Placements!.Constraints;
				(var nextPosition, var nextPositioner) = setn.StackEntry.Positioner.GetNext(thisConstraints);
				var pieces = thisConstraints.At(nextPosition).Pieces;
				for (
					int? index = progressIndex(null, childNodeCount);
					index != null;
					index = progressIndex(index, childNodeCount)
				)
				{
					TreeNode childNode = setn.ChildNodes[index.Value];
					if (childNode is UnexploredTreeNode)
					{
						var newChild = GenerateChildNode(setn, index.Value);
						var newSetn = setn.ReplaceAt(index.Value, newChild);
						if (newChild is PartiallyExploredTreeNode)
						{
							return new(newSetn, true);
						}
						else
						{
							return ProgressToFirstSuccess(newSetn, progressIndex);
						}
					}
					else if (childNode is PartiallyExploredTreeNode childSetn)
					{
						(var newChildNode, bool success) = ProgressToFirstSuccess(childNode, progressIndex);
						var newSetn = setn.ReplaceAt(index.Value, newChildNode);
						if (success)
						{
							return new(newSetn, true);
						}
						else
						{
							return ProgressToFirstSuccess(newSetn, progressIndex);
						}
					}
				}
				throw new Exception("StackEntryTreeNode must have at least one unexplored");
			}
			else
			{
				return new (treeNode, false);
			}
		}

		private static TreeNode ExtendThroughDefaultSelection(
			TreeNode treeNode,
			Func<int?, int, int?> progressIndex
		)
		{
			if (treeNode is PartiallyExploredTreeNode setn)
			{
				var childNodeCount = setn.ChildNodes.Count;
				for (
					int? index = progressIndex(null, childNodeCount);
					index != null;
					index = progressIndex(index, childNodeCount)
				)
				{
					TreeNode childNode = setn.ChildNodes[index.Value];
					if (childNode is UnexploredTreeNode)
					{
						childNode = GenerateChildNode(setn, index.Value);
						childNode = ExtendThroughDefaultSelection(childNode, progressIndex);
						return setn.ReplaceAt(index.Value, childNode);
					}
					if (childNode is PartiallyExploredTreeNode)
					{
						return setn.ReplaceAt(
							index.Value,
							ExtendThroughDefaultSelection(childNode, progressIndex)
						);
					}
				}
			}
			return treeNode;
		}

		public static TreeNode Progress(
			this TreeNode treeNode,
			Func<int?, int, int?> progressIndex,
			IReadOnlyList<ImmutableArray<int>> pieceSides
		)
		{
			if (treeNode is UnexploredTreeNode)
			{
				treeNode = CreateInitialStack(progressIndex, pieceSides);
			}
			else
			{
				(treeNode, bool success) = ProgressToFirstSuccess(treeNode, progressIndex);
			}
			treeNode = ExtendThroughDefaultSelection(treeNode, progressIndex);
			return treeNode;
		}
	}
}
