using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;

namespace Symbolic11.Pages;
/// <summary>
/// Interaction logic for LinkView.xaml
/// </summary>
/// 
public partial class LinkView : Page
{
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetFileAttributesEx(string name, GetFileAttributesExInfo infoLevel, ref REPARSE_POINT_DATA lpReparsePointData);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    static extern int SHGetPathFromName(string pszPath, ref IntPtr pszPathOut);

    private string GetShortcutTarget(string file)
    {
        try
        {
            if (System.IO.Path.GetExtension(file).ToLower() != ".lnk")
            {
                throw new Exception("Supplied file must be a .LNK file");
            }

            FileStream fileStream = File.Open(file, FileMode.Open, FileAccess.Read);
            using (System.IO.BinaryReader fileReader = new BinaryReader(fileStream))
            {
                fileStream.Seek(0x14, SeekOrigin.Begin);     // Seek to flags
                uint flags = fileReader.ReadUInt32();        // Read flags
                if ((flags & 1) == 1)
                {                      // Bit 1 set means we have to
                                       // skip the shell item ID list
                    fileStream.Seek(0x4c, SeekOrigin.Begin); // Seek to the end of the header
                    uint offset = fileReader.ReadUInt16();   // Read the length of the Shell item ID list
                    fileStream.Seek(offset, SeekOrigin.Current); // Seek past it (to the file locator info)
                }

                long fileInfoStartsAt = fileStream.Position; // Store the offset where the file info
                                                             // structure begins
                uint totalStructLength = fileReader.ReadUInt32(); // read the length of the whole struct
                fileStream.Seek(0xc, SeekOrigin.Current); // seek to offset to base pathname
                uint fileOffset = fileReader.ReadUInt32(); // read offset to base pathname
                                                           // the offset is from the beginning of the file info struct (fileInfoStartsAt)
                fileStream.Seek((fileInfoStartsAt + fileOffset), SeekOrigin.Begin); // Seek to beginning of
                                                                                    // base pathname (target)
                long pathLength = (totalStructLength + fileInfoStartsAt) - fileStream.Position - 2; // read
                                                                                                    // the base pathname. I don't need the 2 terminating nulls.
                char[] linkTarget = fileReader.ReadChars((int)pathLength); // should be unicode safe
                var link = new string(linkTarget);

                int begin = link.IndexOf("\0\0");
                if (begin > -1)
                {
                    int end = link.IndexOf("\\\\", begin + 2) + 2;
                    end = link.IndexOf('\0', end) + 1;

                    string firstPart = link.Substring(0, begin);
                    string secondPart = link.Substring(end);

                    return firstPart + secondPart;
                }
                else
                {
                    return link;
                }
            }
        } catch
        {
            return "";
        }
    }

    [Flags]
    public enum GetFileAttributesExInfo : uint
    {
        Basic = 0,
        ReparsePoint = 1,
        Directory = 2,
        Volume = 3,
        FileSize = 4,
        FileAllocationSize = 5,
        ChangeTime = 6,
        AccessTime = 7,
        CreationTime = 8,
        InternalInfo = 9,
        All = Basic | ReparsePoint | Directory | Volume | FileSize | FileAllocationSize | ChangeTime | AccessTime | CreationTime | InternalInfo
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct REPARSE_POINT_DATA
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] Data;
        public uint ReparseTag;
        public ushort Reserved;
        public char[] ReparseTarget;
        public short SubsNameOffset;
        public short SubsNameLength;
        public ushort ReparseDataLength;
    }
    private const uint IO_REPARSE_TAG_MOUNT_POINT = 0xA0000003;
    private const uint IO_REPARSE_TAG_HSM = 0xC0000004;
    private const uint IO_REPARSE_TAG_SIS = 0x80000007;
    private const uint IO_REPARSE_TAG_DFS = 0x8000000A;
    private const uint IO_REPARSE_TAG_FILTER_MANAGER = 0x8000000B;
    private const uint IO_REPARSE_TAG_SYMLINK = 0xA000000C;

    //---------------------------------------------------

    public class Link
    {
        public string Source
        {
            get; set;
        }
        public string Type
        {
            get; set;
        }
        public string Target
        {
            get; set;
        }
        public bool Result
        {
            get; set;
        }
    }


    public string defaultSearchPath = "C:\\Users\\";
    public string currentSearchPath = "C:\\Users\\";
    ObservableCollection<Link> links = new ObservableCollection<Link>();

    private EnumerationOptions enumerationOptions = new EnumerationOptions
    {
        IgnoreInaccessible = true,
        AttributesToSkip = FileAttributes.ReadOnly | FileAttributes.Hidden | FileAttributes.System,
        RecurseSubdirectories = true,
        MaxRecursionDepth = 5,
    };
    private CancellationTokenSource cancellationTokenSource;
    private Task? searchTask;
    public LinkView()
    {
        InitializeComponent();
        SearchPathText.Text = defaultSearchPath;

        linkDataGrid.ItemsSource = links;

        linkDataGrid.Focus();
    }

    private async void performSearchOnPath(CancellationToken cToken)
    {
        string pathToSearch = currentSearchPath;

        var Files = Directory.EnumerateFileSystemEntries(pathToSearch, "*", enumerationOptions);
        Dispatcher.Invoke(() =>
        {
            links.Clear();
        });

        foreach (var filePath in Files)
        {
            if (cToken.IsCancellationRequested)
            {
                Debug.WriteLine("ct.IsCancellationRequested 1");
                return;
            }
            Debug.WriteLine("Searching: " + filePath);
            await Dispatcher.InvokeAsync(() =>
            {
                currentSearchDirectory.Text = filePath;
            });

            FileInfo fileInfo = new FileInfo(filePath);
            var Attributes = fileInfo.Attributes;

            string linkSource = filePath;
            string linkType = "";
            string linkTarget = "???";
            bool linkResult = false;

            if (Attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                linkType = "SymLink";
                linkTarget = fileInfo.LinkTarget;
                linkResult = Directory.Exists(linkTarget);
            }
            else if (fileInfo.Extension == ".lnk")
            {
                linkType = "Shortcut";
                linkTarget = GetShortcutTarget(filePath);
                linkResult = (linkTarget != null);
            }

            if (linkType != "")
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    links.Add(new Link
                    {
                        Type = linkType,
                        Source = filePath,
                        Target = linkTarget,
                        Result = linkResult
                    });
                });
            }
        }
        await Dispatcher.InvokeAsync(() => {
            searchButton.Content = "Search";
            currentSearchDirectory.Text = "";
        });
    }

    private void searchButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        Debug.WriteLine(searchTask == null);

        if (searchTask == null)
        {
            // Start the command
            cancellationTokenSource = new CancellationTokenSource();
            searchTask = Task.Run(() => performSearchOnPath(cancellationTokenSource.Token), cancellationTokenSource.Token);
            searchButton.Content = "Cancel";
        }
        else
        {
            searchButton.Content = "Search";
            currentSearchDirectory.Text = "";
            cancellationTokenSource.Cancel();
            searchTask = null;
        }
    }


    private void changeSearchDirectory_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        var folderDialog = new OpenFolderDialog {
            Multiselect = false,
            ValidateNames = true
        };

        if (folderDialog.ShowDialog() == true &&
            !string.IsNullOrEmpty(folderDialog.FolderName))
        {
            SearchPathText.Text = folderDialog.FolderName;
            currentSearchPath = folderDialog.FolderName;
        }
    }

    private void linkDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        bool enableDelete = false;
        if (linkDataGrid.SelectedItem != null)
        {
            if (linkDataGrid.SelectedItem.GetType() == typeof(Link))
            {
                Link selectedLink = (Link)linkDataGrid.SelectedItem;

                enableDelete = (selectedLink.Source != null);
            }
        }
        DeleteLink.IsEnabled = enableDelete;
    }

    private void linkDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) {
        if (linkDataGrid.SelectedItem is Link selectedLink) {
            if (!string.IsNullOrEmpty(selectedLink.Source)) {
                try {
                    Process.Start("explorer.exe", $"/select,\"{selectedLink.Source}\"");
                } catch (Exception ex) {
                    var messageBox = new Wpf.Ui.Controls.MessageBox {
                        Title = "Error",
                        Content = $"Failed to open link in File Explorer: {ex.Message}",
                        CloseButtonText = "OK",
                    };
                    messageBox.ShowDialogAsync();
                }
            }
        }
    }

    private void DeleteLink_Click(object sender, RoutedEventArgs e)
    {
        if (DeleteLink.IsEnabled)
        {
            Link selectedLink = (Link)linkDataGrid.SelectedItem;
            bool deleted = false;

            if (selectedLink.Type == "Shortcut")
            {

                if (File.Exists(selectedLink.Source))
                {
                    Debug.WriteLine("Deleting File:" + selectedLink.Source);
                    File.Delete(selectedLink.Source);
                    deleted = true;
                }
            }
            else
            {
                if (Directory.Exists(selectedLink.Source))
                {
                    Debug.WriteLine("Deleting Directory:" + selectedLink.Source);
                    Directory.Delete(selectedLink.Source);
                    deleted = true;
                }
            }
            if (deleted) {
                Dispatcher.Invoke(() =>
                {
                    links.Remove(selectedLink);
                });
            }
        }
    }
}
