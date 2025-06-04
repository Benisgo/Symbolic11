using System.Diagnostics;
using System.Security.Principal;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Wpf.Ui.Controls;
using Path = System.IO.Path;

namespace Symbolic11.Pages;

/// <summary>
/// Interaction logic for CreateLink.xaml
/// </summary>
public partial class CreateLink : Page
{
    public string linkFileType = "folder";
    public CreateLink()
    {
        InitializeComponent();

        if (!IsDeveloperModeEnabled() && !IsUserAdministrator()) {
            InfoBar.Severity = Wpf.Ui.Controls.InfoBarSeverity.Warning;
            InfoBar.Title = "Insufficient Permissions";
            InfoBar.Message = "The application is not running with administrative privileges. Link creation may fail.";
            InfoBar.IsOpen = true;
        }

    }
    private bool IsUserAdministrator()
    {
        using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
        {
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
    private bool IsDeveloperModeEnabled()
    {
        const string developerModeKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock";
        const string developerModeValue = "AllowDevelopmentWithoutDevLicense";

        using (RegistryKey key = Registry.LocalMachine.OpenSubKey(developerModeKey))
        {
            if (key != null)
            {
                object value = key.GetValue(developerModeValue);
                if (value != null && (int)value == 1)
                {
                    return true;
                }
            }
        }
        return false;
    }
    private void FileType_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {

        if (DestinationPath == null) { return; }

        var symbFileType = FileType.SelectedItem.ToString()?.Split(new string[] { ": " }, StringSplitOptions.None).Last();

        DestinationPath.Text = ""; //Clear path
        CreateLinkButton.IsEnabled = false;

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

        OpenFolderDialog folderDialog = new OpenFolderDialog {
            Multiselect = false,
            DereferenceLinks = true
        };

        if (folderDialog.ShowDialog() == true &&
            !string.IsNullOrEmpty(folderDialog.FolderName))
        {
            LinkFolderPath.Text = folderDialog.FolderName;
        }
        validate_CreateLink();
    }

    private void DestinationExplore_Click(object sender, RoutedEventArgs e) {
        if (linkFileType == "folder") {
            OpenFolderDialog folderDialog = new OpenFolderDialog {
                Multiselect = false,
                DereferenceLinks = true
            };

            if (folderDialog.ShowDialog() == true
                && !string.IsNullOrEmpty(folderDialog.FolderName)) {
                DestinationPath.Text = folderDialog.FolderName;
            }
        } else if (linkFileType == "file") {
            var fileDialog = new OpenFileDialog() {
                Multiselect = false,
                DereferenceLinks = true
            };

            if (fileDialog.ShowDialog() == true &&
                !string.IsNullOrWhiteSpace(fileDialog.FileName)) {
                DestinationPath.Text = fileDialog.FileName;
            }
        }
        validate_CreateLink();
    }
    private void useDestinationName(object sender, RoutedEventArgs e)
    {
        if (DestinationPath.Text != null)
        {
            string fileName = Path.GetFileName(DestinationPath.Text);
            LinkName.Text = Path.GetFileNameWithoutExtension(fileName);
        }
    }

    private void validate_CreateLink()
    {
        if (!string.IsNullOrEmpty(LinkFolderPath.Text) && !string.IsNullOrEmpty(DestinationPath.Text))
        {
            CreateLinkButton.IsEnabled = true;
        }
        else
        {
            CreateLinkButton.IsEnabled = false;
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
        string? SymbolicTypeText = SymbolicType.SelectedItem.ToString()?.Split(new string[] { ": " }, StringSplitOptions.None).Last();

        if (SymbolicTypeText.Equals(HardLink.Content))
        {
            LinkText.Content = "Link File";
        }
        else if (LinkText != null)
        {
            LinkText.Content = "Link Folder";
        }
    }
}
