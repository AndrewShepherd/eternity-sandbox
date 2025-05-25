using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Printing;
using System.Text;
using System.Threading.Tasks;

namespace Eternity.WpfApp
{
	internal sealed class SolutionState
	{
		public ImmutableList<StackEntry> _placementStack = ImmutableList<StackEntry>.Empty;

		public readonly IReadOnlyList<ImmutableArray<int>> _pieceSides;


		public SolutionState(IReadOnlyList<ImmutableArray<int>> pieceSides)
		{
			_pieceSides = pieceSides;
		}
	}
}
