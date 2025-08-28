using Microsoft.Win32;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Eternity.WpfApp
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public static RoutedCommand ExitCommand = new();

		public static RoutedCommand SaveRunningStateCommand = new();

		public static RoutedCommand LoadRunningStateCommand = new();


		public MainWindow()
		{
			InitializeComponent();
		}

		protected override void OnClosed(EventArgs e)
		{
			var viewModel = this.Resources["MainWindowViewModel"] as MainWindowViewModel;
			viewModel?.OnClosed();
			base.OnClosed(e);
		}

		private void ExitCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			this.Close();
		}

		private void OpenCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			if (openFileDialog.ShowDialog() == true)
			{
				this.Title = System.IO.Path.GetFileNameWithoutExtension(openFileDialog.FileName);
				var txt = File.ReadAllText(openFileDialog.FileName);
				var pieces = PieceTextReader.Parse(txt);
				if (this.Resources["MainWindowViewModel"] is MainWindowViewModel vm)
				{
					vm.SetPieceSides(pieces);
				}
			}
		}

		private void SaveRunningStateCommand_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			SaveFileDialog saveFileDialog = new();
			if (saveFileDialog.ShowDialog() == true)
			{
				using (var fileStream = File.OpenWrite(saveFileDialog.FileName))
				{
					var vm = this.Resources["MainWindowViewModel"] as MainWindowViewModel;
					if (vm != null)
					{
						vm.SaveRunningState(fileStream);
					}
					fileStream.Flush();
					fileStream.Close();
				}
			}
		}

		private void LoadRunningStateCommand_Executed(object sender, ExecutedRoutedEventArgs args)
		{
			OpenFileDialog openFileDialog = new();
			if (openFileDialog.ShowDialog() == true)
			{
				using (var fileStream = File.OpenRead(openFileDialog.FileName))
				{
					var vm = this.Resources["MainWindowViewModel"] as MainWindowViewModel;
					if (vm != null)
					{
						vm.LoadRunningState(fileStream);
					}
				}
			}
		}
	}
}