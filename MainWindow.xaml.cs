using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Symbolic11;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{

    public string linkFileType = "folder";

    public MainWindow()
    {
        Wpf.Ui.Appearance.Theme.Apply(
            Wpf.Ui.Appearance.ThemeType.Dark,
            Wpf.Ui.Controls.WindowBackdropType.Mica,
            true);
        InitializeComponent();
    }

    private void FileType_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {

        if (DestinationPath == null) { return; }

        var symbFileType = FileType.SelectedItem.ToString()?.Split([": "], StringSplitOptions.None).Last();

        DestinationPath.Text = ""; //Clear path
        CreateLink.IsEnabled = false;

        switch (symbFileType)
        {
            case "File":
                linkFileType = "file";
                DestinationText.Content = "Destination File";
                HardLink.IsEnabled = true;
                JunctionLink.IsEnabled = false;
                if (SymbolicType.Text.Equals(JunctionLink.Content))
                {
                    SymbolicType.SelectedIndex = 0;
                }
                break;
            case "Folder":
                linkFileType = "folder";
                DestinationText.Content = "Destination Folder";
                HardLink.IsEnabled = false;
                JunctionLink.IsEnabled = true;
                if (SymbolicType.Text.Equals(HardLink.Content))
                {
                    SymbolicType.SelectedIndex = 0;
                }
                break;
            default:
                linkFileType = "none";
                break;
        }

        if (linkFileType != "none")
        {
            DestinationExplore.IsEnabled = true;
            LinkExplore.IsEnabled = true;
        }
        else
        {
            DestinationExplore.IsEnabled = false;
            LinkExplore.IsEnabled = false;
        }
    }

    private void LinkExplore_Click(object sender, RoutedEventArgs e)
    {
        OpenFolderDialog folderDialog = new OpenFolderDialog() {
            DereferenceLinks = true,
            Multiselect = false
        };
        bool? result = folderDialog.ShowDialog(this);

        if (result == true &&
            !string.IsNullOrEmpty(folderDialog.FolderName))
        {
            LinkFolderPath.Text = folderDialog.FolderName;
        }
        validate_CreateLink();
    }

    private void DestinationExplore_Click(object sender, RoutedEventArgs e)
    {
        if (linkFileType == "folder")
        {
            OpenFolderDialog folderDialog = new OpenFolderDialog() {
                DereferenceLinks = true,
                Multiselect = false
            };
            bool? result = folderDialog.ShowDialog(this);

            if (result == true &&
                !string.IsNullOrEmpty(folderDialog.FolderName))
            {
                DestinationPath.Text = folderDialog.FolderName;
            }
        }
        else if (linkFileType == "file")
        {
            OpenFileDialog fileDialog = new OpenFileDialog() {
                CheckFileExists = true,
                DereferenceLinks = true,
                Multiselect = false
            };
            bool? result = fileDialog.ShowDialog();

            if (result == true &&
                !string.IsNullOrWhiteSpace(fileDialog.FileName))
            {
                DestinationPath.Text = fileDialog.FileName;
            }
        }
        validate_CreateLink();
    }
    private void useDestinationName(object sender, RoutedEventArgs e) {
        if (DestinationPath.Text != null) {
            string fileName = Path.GetFileName(DestinationPath.Text);
            LinkName.Text = Path.GetFileNameWithoutExtension(fileName);
        }
    }

    private void validate_CreateLink()
    {
        if (!string.IsNullOrEmpty(LinkFolderPath.Text) && !string.IsNullOrEmpty(DestinationPath.Text))
        {
            CreateLink.IsEnabled = true;
        }
        else
        {
            CreateLink.IsEnabled = false;
        }
    }

    private void CreateLink_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(LinkName.Text) || string.IsNullOrWhiteSpace(LinkName.Text))
        {
            //Invalid name
            InfoBar.Severity = Wpf.Ui.Controls.InfoBarSeverity.Error;
            InfoBar.Title = "Invalid Name!";
            InfoBar.Message = "";
            InfoBar.IsOpen = true;
        }
        else
        {
            //Create link
            InfoBar.IsOpen = false;
            string linkName = LinkName.Text;
            string linkFolderPath = LinkFolderPath.Text;

            // Combine the link path and name to get the complete link path
            string linkPath = System.IO.Path.Combine(linkFolderPath, linkName);

            string targetPath = DestinationPath.Text;

            // Determine additional arguments based on conditions
            string additionalArguments = "";

            if (SymbolicType.Text == "Symbolic Link" && linkFileType == "folder")
            {
                additionalArguments = "/D";
            }
            else if (SymbolicType.Text == "Hard Link" && linkFileType == "file")
            {
                additionalArguments = "/H";
                linkPath += System.IO.Path.GetExtension(targetPath);
            }
            else if (SymbolicType.Text == "Junction Link" && linkFileType == "folder")
            {
                additionalArguments = "/J";
            }

            try 
            {
                using (Process process = new Process()) 
                {
                    process.StartInfo.FileName = "cmd.exe";
                    process.StartInfo.RedirectStandardInput = true;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.Start();

                    // Run the mklink command with additional arguments
                    process.StandardInput.WriteLine($"mklink {additionalArguments} \"{linkPath}\" \"{targetPath}\"");
                    process.StandardInput.WriteLine("exit");

                    process.WaitForExit();
                }

                // Symbolic link created successfully
                InfoBar.Severity = Wpf.Ui.Controls.InfoBarSeverity.Success;
                InfoBar.Title = "Link " + linkName + " Created!";
                InfoBar.Message = "";
                InfoBar.IsOpen = true;
                Debug.WriteLine($"mklink {additionalArguments} \"{linkPath}\" \"{targetPath}\"");
            }
            catch (Exception ex) 
            {
                // An error occurred
                InfoBar.Severity = Wpf.Ui.Controls.InfoBarSeverity.Error;
                InfoBar.Title = "Error Creating Link";
                InfoBar.Message = ex.Message;
                InfoBar.IsOpen = true;
            }
        }
    }

    private void SymbolicType_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        string? SymbolicTypeText = SymbolicType.SelectedItem.ToString()?.Split([": "], StringSplitOptions.None).Last();

        if (String.Equals(SymbolicTypeText, HardLink.Content))
        {
            LinkText.Content = "Link File";
        }
        else if (LinkText != null)
        {
            LinkText.Content = "Link Folder";
        }
    }
}
