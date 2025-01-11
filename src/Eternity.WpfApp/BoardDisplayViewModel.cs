using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using static Eternity.Sequence;

namespace Eternity.WpfApp
{

	record class CanvasItemGenerationParameters(Placements Placements, Size CanvasSize, int SelectedSequenceIndex);

	abstract class CanvasItemGenerationState { }

	sealed class CigsIdle() : CanvasItemGenerationState;

	sealed class CigsGenerating() : CanvasItemGenerationState;

	sealed class CigsStale(CanvasItemGenerationParameters parameters) : CanvasItemGenerationState
	{
		public CanvasItemGenerationParameters Parameters => parameters;
	}



	public sealed class BoardDisplayViewModel : BindableBase
	{
		readonly ThreadSafePropertyChangedNotifier _propChangedNotifier;
		public BoardDisplayViewModel()
		{
			_propChangedNotifier = new(OnPropertyChanged);
		}
		private Dispatcher _uiDispatcher = Dispatcher.CurrentDispatcher;

		private IEnumerable<CanvasItem> _canvasItems = Enumerable.Empty<CanvasItem>();

		private readonly object _syncLock = new();

		private CanvasItemGenerationState _generationState = new CigsIdle();

		private CanvasItemGenerationState Transition(Func<CanvasItemGenerationState, CanvasItemGenerationState> f)
		{
			lock (_syncLock)
			{
				_generationState = f(_generationState);
				return _generationState;
			}
		}


		private static BitmapImage CreateFromStream(Stream stream)
		{
			var bitmap = new BitmapImage();
			bitmap.BeginInit();
			bitmap.StreamSource = stream;
			bitmap.CacheOption = BitmapCacheOption.OnLoad;
			bitmap.EndInit();
			bitmap.Freeze();
			return bitmap;
		}

		private Task<ImmutableList<BitmapImage>> _fetchBitmapImages =
			PuzzleProvider.LoadPieces().ContinueWith(
				t =>
					t.Result.Select(
						p =>
						{
							using (var stream = ImageProvider.Load(p.ImageId))
								return CreateFromStream(stream!);
						}
					).ToImmutableList()
			);


		private static IEnumerable<CanvasItem> GenerateCanvasItems(
			double bitmapWidth,
			double bitmapHeight,
			ImmutableList<BitmapImage> bitmapImages,
			Placements placements,
			int selectedSequenceIndex)
		{
			var pieceItems = CanvasItemExtensions.GenerateCanvasPieceItems(
				bitmapWidth,
				bitmapHeight,
				bitmapImages,
				placements.Values
			);

			var constraintItems = CanvasItemExtensions.GenerateCanvasConstraintItem(
				bitmapWidth,
				bitmapHeight,
				placements.Constraints
			);

			var canvasItems = pieceItems.Cast<CanvasItem>().Concat(constraintItems).ToList();

			if (selectedSequenceIndex >= 0)
			{
				var highlightedPositionIndexes = SequenceIndexToPositionIndexes(selectedSequenceIndex);
				var highlightedPositions = highlightedPositionIndexes
					.Select(i => Positions.PositionLookup[i])
					.ToArray();
				foreach (var position in highlightedPositions)
				{
					canvasItems.Add(
						new CanvasHighlightItem
						{
							Top = position.Y * bitmapHeight,
							Left = position.X * bitmapWidth,
							Width = bitmapWidth,
							Height = bitmapHeight,
						}
					);
				}
			}
			return canvasItems;
		}

		private async Task GenerateCanvasItems(CanvasItemGenerationParameters canvasItemGenerationParameters)
		{
			var bitmapImages = await _fetchBitmapImages;

			var boardSideLength = Math.Min(
				canvasItemGenerationParameters.CanvasSize.Width,
				canvasItemGenerationParameters.CanvasSize.Height
			);
			var squareSideLength = boardSideLength / 16.0;
			var canvasItems = GenerateCanvasItems(
				squareSideLength,
				squareSideLength,
				bitmapImages,
				canvasItemGenerationParameters.Placements,
				canvasItemGenerationParameters.SelectedSequenceIndex
			);
			_uiDispatcher?.BeginInvoke(
				() => this.CanvasItems = canvasItems
			);
			this.Transition(
				state =>
				{
					if (state is CigsStale stale)
					{
						Task.Factory.StartNew(() => GenerateCanvasItems(stale.Parameters));
						return new CigsGenerating();
					}
					else
					{
						return new CigsIdle();
					}
				}
			);
		}

		private void UpdateCanvasItems()
		{

			var placements = this.Placements;
			if (placements == null)
			{
				return;
			}
			var parameters = new CanvasItemGenerationParameters(
				placements,
				this.CanvasSize, 
				this.SelectedSequenceIndex
			);

			Transition(
				state =>
				{
					if (state is CigsIdle)
					{
						Task t = Task.Factory.StartNew(
							() => GenerateCanvasItems(parameters)
						);
						return new CigsGenerating();
					}
					else
					{
						return new CigsStale(parameters);
					}
				}
			);
		}




		public IEnumerable<CanvasItem> CanvasItems
		{
			get => _canvasItems;
			set
			{
				_canvasItems = value;
				_propChangedNotifier.PropertyChanged(nameof(CanvasItems));
			}
		}

		private int _selectedSequenceIndex = -1;
		public int SelectedSequenceIndex
		{
			get => _selectedSequenceIndex;
			set
			{
				if (_selectedSequenceIndex != value)
				{
					SetProperty(ref _selectedSequenceIndex, value);
					UpdateCanvasItems();
				}
			}
		}

		private Placements? _placements = null;
		public Placements? Placements
		{
			get => _placements;
			set
			{
				if (_placements != value)
				{
					SetProperty(ref _placements, value);
					UpdateCanvasItems();
				}
			}
		}

		private Size _canvasSize;
		public Size CanvasSize
		{
			get => _canvasSize;
			set
			{
				if (_canvasSize != value)
				{
					_canvasSize = value;
					UpdateCanvasItems();
				}
			}
		}
	}
}
