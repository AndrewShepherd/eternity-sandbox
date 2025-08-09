namespace Eternity
{
	using System.Collections.Immutable;
	using System.Numerics;
	using Google.Protobuf;
	using Google.Protobuf.Collections;

	public static class ProtoPieceConversions
	{
		public static Eternity.Proto.Piece ConvertPieceSides(IReadOnlyList<int> pieceSides) =>
			new()
			{
				Sides = (uint)pieceSides[0] << 24
					| (uint)pieceSides[1] << 16
					| (uint)pieceSides[2] << 8
					| (uint)pieceSides[3],
			};

		public static IReadOnlyList<ImmutableArray<int>> ConvertProtoPieces(
			RepeatedField<Proto.Piece> pieces
		) =>
			pieces.Select(
				p =>
					new[] {
						(int)((p.Sides & 0xFF000000) >> 24),
						(int)((p.Sides & 0x00FF0000) >> 16),
						(int)((p.Sides & 0x0000FF00) >> 8),
						(int)(p.Sides & 0x000000FF)
					}.ToImmutableArray()
			).ToArray();
	}

	public static class ProtoConversions
	{
		internal static Proto.BigInteger Convert(BigInteger bi) =>
			new()
			{
				Bytes = Google.Protobuf.ByteString.CopyFrom(bi.ToByteArray()),
			};

		public static BigInteger Convert(Proto.BigInteger bi) =>
			new BigInteger(bi.ToByteArray());

		private static Proto.Placement Convert(Placement p)
		{
			var result = new Proto.Placement
			{
				PieceIndex = p.PieceIndex,
				Position = new() { X = p.Position.X, Y = p.Position.Y }
			};
			result.Rotations.AddRange(
				p.Rotations.Select(
					r => r switch
					{ 
						Rotation.None => Proto.Rotation.None,
						Rotation.Ninety => Proto.Rotation.Ninety,
						Rotation.OneEighty => Proto.Rotation.OneEighty,
						Rotation.TwoSeventy => Proto.Rotation.TwoSeventy,
						_ => throw new Exception($"Unexpected Rotation value of {r}")
					}
				)
			);
			return result;
		}

		private static Placement Convert(Proto.Placement protoPlacement) =>
			new(
				new Position(protoPlacement.Position.X, protoPlacement.Position.Y),
				protoPlacement.PieceIndex,
				protoPlacement.Rotations.Select(
					r =>
						r switch
						{ 
							Proto.Rotation.None => Rotation.None,
							Proto.Rotation.Ninety => Rotation.Ninety,
							Proto.Rotation.OneEighty => Rotation.OneEighty,
							Proto.Rotation.TwoSeventy => Rotation.TwoSeventy,
							_ => throw new Exception($"Unexpected Proto rotation of {r}")
						}
				).ToArray()
			);

		internal static Proto.Placements Convert(Placements placements)
		{
			var result = new Proto.Placements();
			result.Items.AddRange(placements.Values.Select(Convert));
			return result;
		}

		public static Placements Convert(Proto.Placements protoPlacements, IReadOnlyList<ImmutableArray<int>> pieceSides)
		{
			var placements = Placements.CreateInitial(pieceSides);
			foreach(var protoPlacement in protoPlacements.Items)
			{
				placements = placements.SetItem(
					Convert(protoPlacement)
				);
			}
			return placements;
		}
	}

	public static class ProtoTreeNodeConversions
	{
		private static Proto.FullyExploredTreeNode Convert(FullyExploredTreeNode fetn)
		{
			var result = new Proto.FullyExploredTreeNode()
			{
				NodesExplored = ProtoConversions.Convert(fetn.NodesExplored),
			};
			result.Solutions.AddRange(
				fetn.Solutions.Select(ProtoConversions.Convert)
			);
			return result;
		}


		private static Proto.PartiallyExploredTreeNode Convert(PartiallyExploredTreeNode petn)
		{
			var result = new Proto.PartiallyExploredTreeNode();
			result.ChildNodes.AddRange(
				petn.ChildNodes.Select(Convert)
			);
			return result;
		}

		public static Proto.TreeNode Convert(TreeNode treeNode) =>
			treeNode switch
			{
				FullyExploredTreeNode fetn =>
					new()
					{
						FullyExplored = Convert(fetn),
					},
				UnsuccessfulPlacementTreeNode =>
					new()
					{
						UnsuccessfulPlacement = new(),
					},
				UnexploredTreeNode =>
					new()
					{
						Unexplored = new()
					},
				PartiallyExploredTreeNode petn =>
					new()
					{
						PartiallyExplored = Convert(petn)
					},
				_ => throw new Exception($"Unexpected tree node type: {treeNode.GetType().Name}")
			};
	}
}
