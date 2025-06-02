using System.Windows;
using Symbolic11.Pages;
using Wpf.Ui.Controls;

namespace Symbolic11;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow {

    public MainWindow() {
        InitializeComponent();

    }

    private void RootNavigation_Loaded(object sender, RoutedEventArgs e) {
        RootNavigation.Navigate(typeof(CreateLink));
    }
}
