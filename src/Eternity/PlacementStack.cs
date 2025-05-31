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

		public static StackEntryTreeNode AsTreeNode(this StackEntry newStackEntry)
		{
			(var nextPosition, _) = newStackEntry.Positioner.GetNext(newStackEntry.Placements!.Constraints);
			var possiblePieces = newStackEntry.Placements!.Constraints.At(nextPosition).Pieces;
			return new StackEntryTreeNode
			{
				ChildNodes = possiblePieces.Select(p => UnexploredTreeNode.Instance).ToImmutableList(),
				StackEntry = newStackEntry,
				NodesExplored = 1,
				TotalNodesEstimate = null,
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
			IPositioner positioner = new DynamicPositionerAdjacentsOnly(initialPlacements.Dimensions);
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

		public static IEnumerable<StackEntryTreeNode> GetStackEntries(StackEntryTreeNode firstElement)
		{
			yield return firstElement;
			var firstChild = firstElement.ChildNodes.OfType<StackEntryTreeNode>().FirstOrDefault();
			if (firstChild != null)
			{
				foreach(var childEntry in GetStackEntries(firstChild))
				{
					yield return childEntry;
				}
			}
		}

		private static TreeNode UpdateLastNode(List<StackEntryTreeNode> l, Func<StackEntryTreeNode, TreeNode> transform)
		{
			var lastNode = l.Last();
			l.RemoveAt(l.Count - 1);
			var transformedNode = transform(lastNode);
			if (l.Count > 0)
			{
				return UpdateLastNode(l, n => n.Replace(lastNode, transformedNode));
			}
			else
			{
				return transformedNode;
			}
		}

		private static TreeNode ProgressTree(TreeNode treeNode, Func<int?, int, int?> progressIndex)
		{
			if (treeNode is StackEntryTreeNode stackEntryTreeNode)
			{
				var stack = GetStackEntries(stackEntryTreeNode).ToList();
				var lastEntry = stack.Last();
				var childNodeCount = lastEntry.ChildNodes.Count;
				var thisConstraints = lastEntry.StackEntry.Placements!.Constraints;
				(var nextPosition, var nextPositioner) = lastEntry.StackEntry.Positioner.GetNext(thisConstraints);
				var pieces = lastEntry.StackEntry.Placements!.Constraints.At(nextPosition).Pieces;
				for(int? index = progressIndex(null, childNodeCount); index != null; index = progressIndex(index, childNodeCount))
				{
					TreeNode childNode = lastEntry.ChildNodes[index.Value];
					if (childNode is UnexploredTreeNode unexploredTreeNode)
					{
						var piece = pieces.ElementAt(index.Value);
						Placements? attempt = PuzzleSolver.TryAddPiece(
							lastEntry.StackEntry.Placements,
							nextPosition,
							piece
						);
						TreeNode? newChild;
						if (attempt == null)
						{
							newChild = UnsuccessfulPlacementTreeNode.Instance;
						}
						else
						{
							var stackEntry = new StackEntry(
								index.Value,
								childNodeCount,
								nextPosition,
								attempt,
								nextPositioner
							);
							newChild = stackEntry.AsTreeNode();
						}
						// Not exactly correct here. We are returning whether it was successful or not
						return UpdateLastNode(stack, e => e.Replace(childNode, newChild));
					}
				}
				throw new Exception("StackEntryTreeNode must have at least one unexplored");
			}
			else
			{
				return treeNode;
			}
		}

		private static ImmutableList<StackEntry> ExtendThroughDefaultSelection(ImmutableList<StackEntry> stack, Func<int?, int, int?> progressIndex)
		{
			while (true)
			{
				var lastEntry = stack[stack.Count - 1];
				if (lastEntry.Placements == null)
				{
					return stack;
				}
				if (stack.Count > lastEntry.Placements.PieceSides.Count)
				{
					throw new Exception("Error: Placed more items than possible on the stack");
				}
				if (stack.Count == lastEntry.Placements.PieceSides.Count)
				{
					return stack;
				}
				var (position, positioner) = lastEntry.Positioner.GetNext(
					lastEntry.Placements.Constraints
				);
				if (position == lastEntry.Position)
				{
					throw new Exception("Returned a position that was already used");
				}
				// Sanity check
				if ((lastEntry.Positioner as DynamicPositionerAdjacentsOnly)!._returnedAlready.Contains(position))
				{
					throw new Exception("Positioner returned a position it had already returned");
				}
				if ((positioner as DynamicPositionerAdjacentsOnly)!._adjacentPositions.Contains(position))
				{
					throw new Exception("Returned positioner has current position in its adjacent positions");
				}
				var availablePieces = lastEntry.Placements.Constraints.At(position).Pieces;
				var pieceIndex = progressIndex(null, availablePieces.Count);
				if (!pieceIndex.HasValue)
				{
					return stack;
				}
				Placements? attempt = PuzzleSolver.TryAddPiece(
					lastEntry.Placements,
					position,
					availablePieces.ElementAt(new Index(pieceIndex.Value))
				);
				stack = stack.Add(
					new(
						PieceIndex: pieceIndex.Value,
						PossiblePieceCount: availablePieces.Count,
						Position: position,
						Placements: attempt,
						Positioner: positioner
					)
				);
			}
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
				treeNode = ProgressTree(treeNode, progressIndex);
			}
			//treeNode = ExtendThroughDefaultSelection(treeNode, progressIndex);
			return treeNode;
		}
	}
}
