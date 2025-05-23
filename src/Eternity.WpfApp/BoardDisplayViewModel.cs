﻿using System;
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

		private static IEnumerable<CanvasItem> GenerateCanvasItems(
			double bitmapWidth,
			double bitmapHeight,
			ImmutableArray<BitmapImage?> triangleImages,
			Placements placements,
			int selectedSequenceIndex)
		{
			var pieceItems = CanvasItemExtensions.GenerateCanvasPieceItems(
				bitmapWidth,
				bitmapHeight,
				triangleImages,
				placements
			);

			var constraintItems = CanvasItemExtensions.GenerateCanvasConstraintNumber(
				bitmapWidth,
				bitmapHeight,
				placements.Constraints,
				placements.Dimensions
			);

			var canvasItems = pieceItems.Cast<CanvasItem>().Concat(constraintItems).ToList();

			if (selectedSequenceIndex >= 0)
			{
				if (selectedSequenceIndex < placements.Values.Count)
				{
					var position = placements.Values[selectedSequenceIndex].Position;
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


		private ImmutableArray<BitmapImage?>? _triangles;

		private static BitmapImage?[] LoadTriangles()
		{
			string?[] imageIds = new string?[45];
			for(int i = 0; i < imageIds.Length; ++i)
			{
				imageIds[i] = $"triangle_{i:d2}.png";
			}
			BitmapImage?[] result = new BitmapImage?[imageIds.Length];
			for(int i = 0; i < imageIds.Length; ++i)
			{
				string? imageId = imageIds[i];
				if (imageId != null)
				{
					var stream = ImageProvider.Load(imageId);
					if (stream != null)
					{
						result[i] = CreateFromStream(stream);
					}
				}
			}
			return result;
		}

		private double CalculateSquareSideLength(CanvasItemGenerationParameters canvasItemGenerationParameters)
		{
			var boardSideLength = Math.Min(
				canvasItemGenerationParameters.CanvasSize.Width,
				canvasItemGenerationParameters.CanvasSize.Height
			);
			var boardSquares = canvasItemGenerationParameters.Placements.Dimensions.PositionCount();
			var squaresPerSide = Math.Sqrt(boardSquares);
			return boardSideLength / squaresPerSide;
		}

		private void GenerateCanvasItems(CanvasItemGenerationParameters canvasItemGenerationParameters)
		{
			_triangles = _triangles ?? LoadTriangles().ToImmutableArray();
			var triangleImages = _triangles ?? LoadTriangles().ToImmutableArray();

			var squareSideLength = CalculateSquareSideLength(canvasItemGenerationParameters);
			var canvasItems = GenerateCanvasItems(
				squareSideLength,
				squareSideLength,
				triangleImages,
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
			if (placements.PieceSides.Count == 0)
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
					bool countChanged = (_placements?.PieceSides?.Count ?? 0) != (value?.PieceSides?.Count ?? 0);
					SetProperty(ref _placements, value);
					UpdateCanvasItems();
					if (countChanged)
					{
						this._propChangedNotifier.PropertyChanged(nameof(SquareSize));
					}
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
					_propChangedNotifier.PropertyChanged(nameof(BoardSize));
					_propChangedNotifier.PropertyChanged(nameof(SquareSize));
				}
			}
		}

		public Size _boardSize;
		public Size BoardSize
		{
			get
			{
				var minValue = Math.Min(_canvasSize.Width, _canvasSize.Height);
				return new Size(minValue, minValue);
			}
		}

		public Rect SquareSize
		{
			get
			{
				if (this.Placements != null)
				{
					var parameters = new CanvasItemGenerationParameters(
						this.Placements,
						this.CanvasSize,
						this.SelectedSequenceIndex
					);
					var squareSize = CalculateSquareSideLength(parameters);
					return new Rect(0, 0, squareSize, squareSize);
				}
				else
				{
					var canvasSize = this.CanvasSize;
					return new Rect(0, 0, canvasSize.Width, canvasSize.Height);
				}

			}
		}
	}
}
