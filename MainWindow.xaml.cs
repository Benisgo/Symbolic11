using System.Windows;
using Symbolic11.Pages;
using Wpf.Ui.Controls;

namespace Symbolic11;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    private static CreateLink createLinkPage;
    private static LinkView linkViewPage;
    public MainWindow()
    {
        InitializeComponent();
        createLinkPage = new CreateLink();
        linkViewPage = new LinkView();
        Loaded += (_, _) => RootNavigation.Navigate(typeof(CreateLink));
    }
    private void NavigationView_SelectionChanged(NavigationView sender, RoutedEventArgs e)
    {
        NavigationViewItem selectedItem = e.OriginalSource as NavigationViewItem;

        if (selectedItem != null)
        {
            switch (selectedItem.Content)
            {
                case "Create Links":
                    ContentFrame.Navigate(createLinkPage);
                    break;
                case "View Links":
                    ContentFrame.Navigate(linkViewPage);
                    break;
            }
        }
    }

}