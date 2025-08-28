using Microsoft.Win32;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Eternity.Server.WpfApp;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
	public MainWindow()
	{
		InitializeComponent();
	}

	private void Window_Loaded(object sender, RoutedEventArgs e)
	{
		if (this.Resources["MainWindowViewModel"] is MainWindowViewModel vm)
		{
			vm.OpenListener();
		}
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
				Task unawaitedTask = vm.SetPieceSides(pieces);
			}
		}
	}
}