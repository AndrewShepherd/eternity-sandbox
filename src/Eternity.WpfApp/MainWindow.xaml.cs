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
				var txt = File.ReadAllText(openFileDialog.FileName);
				var pieces = PieceTextReader.Parse(txt);
				var vm = this.Resources["MainWindowViewModel"] as MainWindowViewModel;
				if (vm != null)
				{
					vm.SetPieceSides(pieces);
				}

			}
		}
	}
}