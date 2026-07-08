using Microsoft.WindowsAPICodePack.Dialogs;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace NET_Thing_Encryptor
{
    public partial class FormMain : Form
    {
        public event EventHandler? FolderChanged;

        private ThingFolder? CurrentFolder;
        private ulong _currentFolderID = 0;
        private int recalculatingFileSystemSize;
        private const int AutoLockMinutes = 5;
        private const long LargeFileWarningThreshold = 256L * 1024 * 1024;
        private readonly System.Windows.Forms.Timer _savingIndicatorTimer = new()
        {
            Interval = 500
        };
        private readonly System.Windows.Forms.Timer _sessionLockTimer = new()
        {
            Interval = 1000
        };
        private readonly ToolStripMenuItem _toolStripMenuItemMoveHere = new("Move marked here");
        private readonly ActivityMessageFilter _activityMessageFilter;
        private DateTime _lastUserActivityUtc = DateTime.UtcNow;
        private bool _lockingSession;
        private readonly List<ulong> _pendingMoveIds = [];
        private readonly TextBox _textBoxSearch = new()
        {
            Width = 180,
            PlaceholderText = "Search"
        };
        private List<ThingObjectLink> _currentFolderContent = [];
        private static ThingRoot Root =>
            ThingData.Root ?? throw new InvalidOperationException("The root data has not been loaded.");

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ulong CurrentFolderID
        {
            get
            {
                return _currentFolderID;
            }
            set
            {
                _currentFolderID = value;
                FolderChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public FormMain()
        {
            InitializeComponent();
            AppTheme.Apply(this, Root.DarkMode);
            AppTheme.Apply(contextMenuStrip, Root.DarkMode);
            ConfigureMainLayout();
            _activityMessageFilter = new ActivityMessageFilter(RecordUserActivity);
            Application.AddMessageFilter(_activityMessageFilter);
            _savingIndicatorTimer.Tick += SavingIndicatorTimer_Tick;
            _sessionLockTimer.Tick += SessionLockTimer_Tick;

            FolderChanged += OnFolderChanged;
        }

        private void ConfigureMainLayout()
        {
            labelMe.Visible = false;
            tableLayoutPanelMain.Padding = new Padding(10, 8, 10, 8);
            tableLayoutPanelNavigation.Margin = new Padding(0, 0, 0, 8);
            tableLayoutPanelDiv.Margin = Padding.Empty;
            flowLayoutPanelInfo.Margin = new Padding(0, 8, 0, 0);
            flowLayoutPanelInfo.Padding = new Padding(6, 2, 6, 2);

            textBoxNavigation.Margin = new Padding(16, 7, 16, 7);
            textBoxNavigation.Font = new Font("Segoe UI", 10.5F);

            Button[] navigationButtons =
            {
                buttonNavigationBack,
                buttonNavigationCreateFile,
                buttonNavigationCreateFolder,
                buttonNavigationDeleteSelected,
                buttonNavigationExport,
                buttonNavigationSettings,
                buttonNavigationRoot,
                buttonExitApplication
            };

            foreach (Button button in navigationButtons)
            {
                button.AutoSize = false;
                button.MinimumSize = Size.Empty;
                button.Size = new Size(44, 44);
                button.Margin = new Padding(3);
                button.Padding = new Padding(7);
                button.BackgroundImageLayout = ImageLayout.Zoom;
            }

            buttonNavigationRoot.Margin = new Padding(15, 3, 3, 3);
            buttonExitApplication.FlatAppearance.MouseOverBackColor = Color.FromArgb(112, 42, 48);
            buttonExitApplication.FlatAppearance.MouseDownBackColor = Color.FromArgb(135, 45, 52);

            listViewMain.Font = new Font("Segoe UI", 10.5F);
            listViewMain.Margin = Padding.Empty;
            listViewMain.MultiSelect = true;
            listViewMain.ContextMenuStrip = contextMenuStrip;
            listViewMain.Resize += (_, _) => ResizeListColumns();
            toolStripMenuItemCopy.Text = "Mark for move";
            toolStripMenuItemCopy.Click += toolStripMenuItemMarkForMove_Click;
            _toolStripMenuItemMoveHere.Click += toolStripMenuItemMoveHere_Click;
            contextMenuStrip.Items.Insert(3, _toolStripMenuItemMoveHere);
            _textBoxSearch.Margin = new Padding(18, 8, 3, 3);
            _textBoxSearch.BackColor = Root.DarkMode ? Color.FromArgb(45, 45, 45) : SystemColors.Window;
            _textBoxSearch.ForeColor = Root.DarkMode ? Color.White : SystemColors.WindowText;
            _textBoxSearch.BorderStyle = BorderStyle.FixedSingle;
            _textBoxSearch.TextChanged += textBoxSearch_TextChanged;
            flowLayoutPanelNavigationButtons.Controls.Add(_textBoxSearch);
            AppTheme.Apply(contextMenuStrip, Root.DarkMode);
            ResizeListColumns();
        }

        private void ResizeListColumns()
        {
            int availableWidth = listViewMain.ClientSize.Width -
                                 SystemInformation.VerticalScrollBarWidth - 8;
            if (availableWidth <= 0)
                return;

            columnHeaderSize.Width = Math.Min(150, Math.Max(100, availableWidth / 5));
            columnHeaderCreated.Width = Math.Min(210, Math.Max(150, availableWidth / 4));
            columnHeaderName.Width = Math.Max(
                220,
                availableWidth - columnHeaderSize.Width - columnHeaderCreated.Width);
        }
        private void FormMain_Load(object sender, EventArgs e)
        {
            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
            {
                Debug.WriteLine("FormMain is Single Threaded Apartment\n");
            }
            else
            {
                Debug.WriteLine("FormMain is Multi Threaded Apartment, which is very bad and should NOT happen!\n");
            }

            labelInfoVersion.Text = $"v{Program.Version}";
            CurrentFolderID = 0;
            _savingIndicatorTimer.Start();
            _sessionLockTimer.Start();
            RecalculateFileSystemSize(); // Recalculate Folder Sizes
        }

        private void SavingIndicatorTimer_Tick(object? sender, EventArgs e)
        {
            if (_pendingMoveIds.Count > 0)
            {
                labelInfoSaving.BackColor = Color.FromArgb(255, 205, 120);
                labelInfoSaving.ForeColor = Color.Black;
                labelInfoSaving.Text = $"Marked {_pendingMoveIds.Count} for move";
                labelInfoSaving.Visible = true;
                return;
            }

            labelInfoSaving.BackColor = Color.Red;
            labelInfoSaving.ForeColor = Color.Black;
            labelInfoSaving.Text = "Saving...";
            labelInfoSaving.Visible = ThingData.Saving > 0;
        }

        private void RecordUserActivity()
        {
            if (!_lockingSession)
                _lastUserActivityUtc = DateTime.UtcNow;
        }

        private async void SessionLockTimer_Tick(object? sender, EventArgs e)
        {
            if (_lockingSession ||
                ThingData.Saving > 0 ||
                !ThingData.IsSessionUnlocked ||
                DateTime.UtcNow - _lastUserActivityUtc < TimeSpan.FromMinutes(AutoLockMinutes))
            {
                return;
            }

            await LockSessionAsync();
        }

        private async Task LockSessionAsync()
        {
            if (_lockingSession)
                return;

            _lockingSession = true;
            _sessionLockTimer.Stop();
            try
            {
                if (ThingData.Saving > 0)
                    return;

                if (!CloseSecondaryFormsForSessionLock())
                {
                    _lastUserActivityUtc = DateTime.UtcNow;
                    return;
                }

                await ThingData.SaveRootAsync();
                Hide();
                ThingData.LockSession();

                using PasswordForm passwordForm = new();
                if (passwordForm.ShowDialog() == DialogResult.OK)
                {
                    Show();
                    WindowState = FormWindowState.Maximized;
                    CurrentFolderID = CurrentFolderID;
                    _lastUserActivityUtc = DateTime.UtcNow;
                }
                else
                {
                    Application.Exit();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"The session could not be locked safely: {ex.Message}",
                    "Lock failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                Show();
                _lastUserActivityUtc = DateTime.UtcNow;
            }
            finally
            {
                _lockingSession = false;
                _sessionLockTimer.Start();
            }
        }

        private bool CloseSecondaryFormsForSessionLock()
        {
            foreach (Form form in Application.OpenForms.Cast<Form>().ToList())
            {
                if (ReferenceEquals(form, this) || form is PasswordForm)
                    continue;

                form.Close();
                if (!form.IsDisposed && form.Visible)
                    return false;
            }

            return true;
        }

        private async void OnFolderChanged(object? sender, EventArgs e)
        {
            Debug.WriteLine("Folder changed. Refreshing items.");
            if (CurrentFolderID == 0)
            {
                CurrentFolder = null;
                RecalculateFileSystemSize(); // Maybe optimize so it only runs when changes to the MD5 or something were made? Set a flag if changes were made...
            }
            else
            {
                CurrentFolder = await ThingData.LoadFileAsync<ThingFolder>(CurrentFolderID);
            }
            _currentFolderContent = await ThingData.LoadFolderContent(CurrentFolderID);

            Debug.WriteLine($"Loaded folder contains {_currentFolderContent.Count} items:");
            Debug.WriteLine(new string('-', 80));
            foreach (ThingObjectLink o in _currentFolderContent)
            {
                Debug.WriteLine($"  - {o.Name} {o.Type}  ID {o.ID} ({ThingData.IDToHex(o.ID)})");

            }
            Debug.WriteLine(new string('-', 80) + '\n');

            RenderCurrentFolderContent();
        }

        private void textBoxSearch_TextChanged(object? sender, EventArgs e)
        {
            RenderCurrentFolderContent();
        }

        private void RenderCurrentFolderContent()
        {
            listViewMain.Items.Clear();
            string searchText = _textBoxSearch.Text.Trim();
            IEnumerable<ThingObjectLink> visibleContent = _currentFolderContent;
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                visibleContent = visibleContent.Where(link =>
                    link.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    link.Type.ToString().Contains(searchText, StringComparison.OrdinalIgnoreCase));
            }

            int folderCount = 0;
            int fileCount = 0;
            long totalSize = 0;
            List<ListViewItem> items = new();

            foreach (ThingObjectLink o in visibleContent)
            {
                if (o.Type == FileType.folder) folderCount++;
                else fileCount++;

                ListViewItem item = new ListViewItem(text: o.Name);
                item.Name = o.ID.ToString();
                item.ImageKey = o.Type.ToString();

                item.SubItems.Add(o.Size.Sizeify());
                totalSize += o.Size;
                item.SubItems.Add(o.CreatedAt.ToString());

                items.Add(item);
            }


            items = items.OrderBy(i => i.Text, new NaturalStringComparer()).ToList();
            foreach (ListViewItem item in items)
            {
                listViewMain.Items.Add(item);
            }

            labelInfoFileCount.Text = $"Files: {fileCount}";
            labelInfoFolderCount.Text = $"Folders: {folderCount}";
            labelInfoTotalSize.Text = $"Total Size: {totalSize.Sizeify()}";
            if (!string.IsNullOrWhiteSpace(searchText))
                labelInfoTotalSize.Text += $" - Filtered from {_currentFolderContent.Count}";
            if (CurrentFolderID == 0)
            {
                labelInfoTotalSize.Text += " - With encryption overhead: " +
                    new DirectoryInfo(Root.SaveLocation).EnumerateFiles("*", SearchOption.TopDirectoryOnly)
                        .Sum(f => f.Length).Sizeify();
            }
        }
        private async void listViewMain_DoubleClick(object sender, EventArgs e)
        {
            Debug.Write("Double click on ");
            if (listViewMain.SelectedItems.Count > 0)
            {
                ListViewItem item = listViewMain.SelectedItems[0];
                Debug.WriteLine($"{item.Text} mit ID {item.Name} ({ThingData.IDToHex(Convert.ToUInt64(item.Name))}");
                try
                {
                    ThingObject? obj = await ThingData.LoadFileAsync(ulong.Parse(item.Name)); // Loads a file after it has been double clicked.
                    if (obj is ThingFolder folder)
                    {
                        CurrentFolderID = folder.ID;
                        textBoxNavigation.Text += $@"/{folder.Name}";
                    }
                    else if (obj is ThingFile file)
                    {
                        Debug.WriteLine($"File: {file.Name} Type: {file.Type.ToString()} ID {file.ID} ({ThingData.IDToHex(file.ID)})");
                        OpenFile(file);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"The selected item could not be opened: {ex.Message}",
                        "Open failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            else
            {
                Debug.WriteLine("nichts?!");
            }
        }
        private void buttonNavigationBack_Click(object sender, EventArgs e)
        {
            if (CurrentFolderID != 0 && CurrentFolder != null)
            {
                CurrentFolderID = CurrentFolder.ParentID;
                textBoxNavigation.Text = textBoxNavigation.Text.Substring(0, textBoxNavigation.Text.LastIndexOf('/'));
            }
        }
        private async void buttonNavigationCreateFile_Click(object sender, EventArgs e)
        {
            if (CurrentFolder == null && CurrentFolderID == 0)
            {
                MessageBox.Show("You cannot create files in the root directory. Please create a folder first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                ArgumentNullException.ThrowIfNull(CurrentFolder);

                var dialog = new CommonOpenFileDialog
                {
                    IsFolderPicker = false,
                    Multiselect = true,
                    Title = "Select Files",
                    Filters =
                    {
                        new CommonFileDialogFilter("All files", "*.*"),
                        new CommonFileDialogFilter("Image Files",
                            "*.bmp;*.dib;" +                                // Windows Bitmap
                            "*.miff;" +                                                 // ImageMagick MIFF
                            "*.rgb;*.rgba;*.gray;*.pgm;*.ppm;*.pnm;*.pbm;" +            // Rohformate / Portable Bitmaps
                            "*.txt;*.csv;" +                                            // Text / ASCII-Bilder
                            "*.tile;*.y;*.cmyk;*.uyvy;" +                               // Tiled / Farbebenen
                            "*.tga;*.vda;*.icb;*.vst;" +                                // Truevision Targa
                            "*.ras;" +                                                  // Sun Raster
                            "*.xpm;*.xwd;" +                                            // X Window Formate
                            "*.sgi;" +                                                  // Silicon Graphics
                            "*.pcd;" +                                                  // Kodak PhotoCD
                            "*.eps;*.ps;" +                                             // PostScript
                            "*.avif;*.av1" +                                            // AVIF
                            "*.jpg;*.jpeg;*.png;*.gif;*.tiff;*.tif;*.webp;*.heic;*.pdf" // Defaults
                        ),
                        new CommonFileDialogFilter("Video files", "*.*"),
                        new CommonFileDialogFilter("Audio files", "*.*"),
                        new CommonFileDialogFilter("Text files", "*.txt"),
                    },
                };
                if (!Directory.Exists(Root.ImportLocation))
                {
                    MessageBox.Show("The default import directory does not exist anymore.");
                    Root.ImportLocation = "C:\\";
                    dialog.InitialDirectory = "C:\\";
                }
                else
                {
                    dialog.InitialDirectory = Root.ImportLocation;
                }

                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    List<string> selectedFiles = FilterLargeImportFiles(dialog.FileNames.ToList());
                    HashSet<string> reservedNames = new(StringComparer.OrdinalIgnoreCase);

                    foreach (string file in selectedFiles)
                    {
                        if (!File.Exists(file))
                        {
                            MessageBox.Show($"The file {file} does not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            continue;
                        }

                        ThingFile? newFile = null;
                        ThingData.BeginSaving();
                        try
                        {
                            string objectName = GetUniqueNameForCurrentFolder(
                                Path.GetFileNameWithoutExtension(file),
                                reservedNames);
                            reservedNames.Add(objectName);

                            newFile = new ThingFile(
                                objectName,
                                await File.ReadAllBytesAsync(file));
                            Enum.TryParse<FileType>(FileCategories.GetFileType(file).ToString(), true,
                                out FileType result);
                            newFile.Type = result;
                            newFile.Extension = Path.GetExtension(file).TrimStart('.');

                            await ThingData.MoveFileToFolderAsync(newFile, CurrentFolderID);
                        }
                        finally
                        {
                            long releasedBytes = newFile?.Content?.LongLength ?? 0;
                            newFile?.ReleaseContent();
                            MemoryMaintenance.NotifyLargeBufferReleased(releasedBytes);
                            ThingData.EndSaving();
                        }
                    }
                    CurrentFolderID = CurrentFolderID;
                }
            }
        }
        private async void buttonNavigationCreateFolder_Click(object sender, EventArgs e)
        {
            using CreateFolderForm form = new();
            if (form.ShowDialog() == DialogResult.OK)
            {
                if (CurrentFolderContainsName(form.Name))
                {
                    MessageBox.Show(
                        $"The current folder already contains an item named {form.Name}.",
                        "Name already exists",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                ThingFolder newFolder = new ThingFolder(form.Name).AddToRoot();
                await ThingData.SaveFileAsync(newFolder);
                if (CurrentFolderID == 0)
                {
                    await ThingData.SaveRootAsync(); // New Folder-Parent needs to acknowledge its childs existence
                }
                else
                {
                    await ThingData.MoveFolderToFolderAsync(newFolder.ID, CurrentFolderID);
                }

            }
            else
            {
                return;
            }

            CurrentFolderID = CurrentFolderID;
        }
        private async void buttonNavigationDeleteSelected_Click(object sender, EventArgs e)
        {
            if (listViewMain.SelectedItems.Count <= 0)
                return;

            List<ListViewItem> selectedItems = listViewMain.SelectedItems
                .Cast<ListViewItem>()
                .ToList();
            string message = selectedItems.Count == 1
                ? $"Are you sure you want to delete {selectedItems[0].Text} and all of its contents?"
                : $"Are you sure you want to delete {selectedItems.Count} selected items and all of their contents?";

            DialogResult r = MessageBox.Show(
                message,
                "Confirmation required",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (r != DialogResult.Yes)
                return;

            foreach (ListViewItem item in selectedItems)
            {
                Debug.WriteLine($"Deleting {item.Text}");
                try
                {
                    await ThingData.DeleteObject(ulong.Parse(item.Name));
                    listViewMain.Items.Remove(item);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"The item {item.Text} could not be deleted: {ex.Message}",
                        "Delete failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }
        private void buttonNavigationRoot_Click(object sender, EventArgs e)
        {
            if (CurrentFolderID != 0)
            {
                CurrentFolderID = 0;
                textBoxNavigation.Text = "/Root";
            }
        }
        private void buttonNavigationSettings_Click(object sender, EventArgs e)
        {
            using SettingsForm form = new SettingsForm();
            form.ShowDialog();
        }
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool ShutdownBlockReasonCreate(IntPtr hWnd, [MarshalAs(UnmanagedType.LPWStr)] string pwszReason);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool ShutdownBlockReasonDestroy(IntPtr hWnd);
        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (ThingData.Saving > 0)
            {
                switch (e.CloseReason)
                {
                    // Closing allowed
                    case CloseReason.ApplicationExitCall:
                        e.Cancel = false;
                        break;
                    case CloseReason.TaskManagerClosing:
                        e.Cancel = false;
                        break;
                    // Closing forbidden
                    case CloseReason.UserClosing:
                        e.Cancel = true;
                        if (MessageBox.Show(
                                "Please wait until the saving process has finished! If you decide to close the program now, data might be lost.",
                                "Saving in progress", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) ==
                            DialogResult.OK)
                        {
                            e.Cancel = false;
                        }
                        break;
                    case CloseReason.WindowsShutDown:
                        ShutdownBlockReasonCreate(this.Handle, "Saving changes...");
                        e.Cancel = true;
                        break;
                    default:
                        e.Cancel = true;
                        break;
                }
            }
        }

        private void FormMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            _savingIndicatorTimer.Stop();
            _savingIndicatorTimer.Tick -= SavingIndicatorTimer_Tick;
            _savingIndicatorTimer.Dispose();
            _sessionLockTimer.Stop();
            _sessionLockTimer.Tick -= SessionLockTimer_Tick;
            _sessionLockTimer.Dispose();
            Application.RemoveMessageFilter(_activityMessageFilter);
            ShutdownBlockReasonDestroy(this.Handle);
        }

        private async void buttonNavigationExport_Click(object sender, EventArgs e)
        {
            if (listViewMain.SelectedItems.Count <= 0)
                return;

            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Multiselect = false,
                Title = "Select export directory"
            };
            if (!Directory.Exists(Root.ExportLocation))
            {
                MessageBox.Show("The default export directory does not exist anymore.");
                Root.ExportLocation = "C:\\";
                dialog.InitialDirectory = "C:\\";
            }
            else
            {
                dialog.InitialDirectory = Root.ExportLocation;
            }

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok &&
                !string.IsNullOrWhiteSpace(dialog.FileName))
            {
                foreach (ListViewItem item in listViewMain.SelectedItems.Cast<ListViewItem>().ToList())
                {
                    string path = dialog.FileName; // The Folder to export to
                    ThingObject? o = await ThingData.LoadFileAsync(ulong.Parse(item.Name));
                    if (o is ThingFolder folder)
                    {
                        await ExportFolder(folder, path);
                    }
                    else if (o is ThingFile file)
                    {
                        await ExportLoadedFile(file, path);
                    }
                }
            }
        }
        private async Task ExportFile(ThingObjectLink file, string path)
        {
            if (file.Type == FileType.folder)
            {
                ThingFolder? f = await ThingData.LoadFileAsync<ThingFolder>(file.ID);
                ArgumentNullException.ThrowIfNull(f);
                await ExportFolder(f, path);
            }
            else
            {
                ThingFile? f = await ThingData.LoadFileAsync<ThingFile>(file.ID);
                ArgumentNullException.ThrowIfNull(f);
                await ExportLoadedFile(f, path);
            }
        }

        private async Task ExportFolder(ThingFolder folder, string parentPath)
        {
            string folderPath = GetAvailableDirectoryPath(Path.Combine(parentPath, folder.Name));
            Directory.CreateDirectory(folderPath);
            foreach (ThingObjectLink child in folder.Content)
                await ExportFile(child, folderPath);
        }

        private static async Task ExportLoadedFile(ThingFile file, string path)
        {
            try
            {
                if (file.Content == null || file.Content.Length == 0)
                {
                    MessageBox.Show(
                        $"File {file.Name} has no content.",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }
                string filePath = GetAvailableExportFilePath(path, file.Name, file.Extension);
                string? directory = Path.GetDirectoryName(filePath);
                if (directory is not null)
                    Directory.CreateDirectory(directory);
                await using FileStream output = new(
                    filePath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    81920,
                    FileOptions.Asynchronous | FileOptions.SequentialScan);
                await output.WriteAsync(file.Content);
            }
            finally
            {
                long releasedBytes = file.Content?.LongLength ?? 0;
                file.ReleaseContent();
                MemoryMaintenance.NotifyLargeBufferReleased(releasedBytes);
            }
        }

        private static string GetAvailableExportFilePath(string directory, string name, string extension)
        {
            string suffix = string.IsNullOrWhiteSpace(extension) ? string.Empty : "." + extension;
            return GetAvailablePath(Path.Combine(directory, name + suffix), isDirectory: false);
        }

        private static string GetAvailableDirectoryPath(string directory)
        {
            return GetAvailablePath(directory, isDirectory: true);
        }

        private static string GetAvailablePath(string path, bool isDirectory)
        {
            if (!PathExists(path, isDirectory))
                return path;

            string? directory = Path.GetDirectoryName(path);
            string name = isDirectory
                ? Path.GetFileName(path)
                : Path.GetFileNameWithoutExtension(path);
            string extension = isDirectory ? string.Empty : Path.GetExtension(path);

            for (int index = 2; ; index++)
            {
                string candidate = Path.Combine(directory ?? string.Empty, $"{name} ({index}){extension}");
                if (!PathExists(candidate, isDirectory))
                    return candidate;
            }
        }

        private static bool PathExists(string path, bool isDirectory)
        {
            return isDirectory ? Directory.Exists(path) : File.Exists(path);
        }

        private void listViewMain_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
            {
                return;
            }

            ListViewItem? item = listViewMain.GetItemAt(e.X, e.Y);
            if (item is not null && !item.Selected)
            {
                listViewMain.SelectedItems.Clear();
                item.Selected = true;
                item.Focused = true;
            }
            else if (item is null)
            {
                listViewMain.SelectedItems.Clear();
            }

            UpdateContextMenuState();

            contextMenuStrip.Show(listViewMain, e.Location);
        }

        private async void toolStripMenuItemRename_Click(object sender, EventArgs e)
        {
            if (listViewMain.SelectedItems.Count > 0)
            {
                ListViewItem item = listViewMain.SelectedItems[0];
                CreateFolderForm form = new(item.Text);
                if (form.ShowDialog() == DialogResult.OK)
                {
                    string newName = form.Name;
                    if (CurrentFolderContainsName(newName, ulong.Parse(item.Name)))
                    {
                        MessageBox.Show(
                            $"The current folder already contains an item named {newName}.",
                            "Name already exists",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        return;
                    }

                    await ThingData.RenameObjectAsync(ulong.Parse(item.Name), newName);
                    CurrentFolderID = CurrentFolderID;
                    listViewMain.SelectedItems.Clear();
                }
            }
        }

        private void toolStripMenuItemMarkForMove_Click(object? sender, EventArgs e)
        {
            _pendingMoveIds.Clear();
            _pendingMoveIds.AddRange(GetSelectedObjectIds());
            UpdateContextMenuState();
            labelInfoSaving.Visible = true;
            labelInfoSaving.BackColor = Color.FromArgb(255, 205, 120);
            labelInfoSaving.ForeColor = Color.Black;
            labelInfoSaving.Text = $"Marked {_pendingMoveIds.Count} for move";
        }

        private async void toolStripMenuItemMoveHere_Click(object? sender, EventArgs e)
        {
            if (_pendingMoveIds.Count == 0)
                return;

            List<ulong> moveIds = _pendingMoveIds.ToList();
            List<string> failures = [];
            foreach (ulong id in moveIds)
            {
                try
                {
                    ThingObject? obj = await ThingData.LoadFileAsync(id);
                    switch (obj)
                    {
                        case ThingFile file:
                            await ThingData.MoveFileToFolderAsync(file, CurrentFolderID);
                            break;
                        case ThingFolder folder:
                            await ThingData.MoveFolderToFolderAsync(folder.ID, CurrentFolderID);
                            break;
                        case null:
                            failures.Add($"{ThingData.IDToHex(id)} no longer exists.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    failures.Add($"{ThingData.IDToHex(id)}: {ex.Message}");
                }
            }

            _pendingMoveIds.Clear();
            labelInfoSaving.Text = "Saving...";
            labelInfoSaving.BackColor = Color.Red;
            labelInfoSaving.ForeColor = Color.Black;
            labelInfoSaving.Visible = ThingData.Saving > 0;
            CurrentFolderID = CurrentFolderID;

            if (failures.Count > 0)
            {
                MessageBox.Show(
                    "Some items could not be moved:\n\n" + string.Join("\n", failures),
                    "Move failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private async Task<long> GetFolderSize(ulong folderID)
        {

            ThingFolder folder = await ThingData.LoadFileAsync<ThingFolder>(folderID)
                ?? throw new FileNotFoundException("The folder could not be loaded.");
            long folder_size = 0;
            foreach (ThingObjectLink link in folder.Content)
            {
                if (link.Type == FileType.folder)
                {
                    folder_size += await GetFolderSize(link.ID);
                }
                else
                {
                    folder_size += link.Size;
                }
            }

            await ThingData.UpdateObjectSizeAsync(folder.ID, folder_size, folder.ParentID);

            return folder_size;
        }

        private void toolStripMenuItemEmergencyEditor_Click(object sender, EventArgs e)
        {
            if (listViewMain.SelectedItems.Count > 0)
            {
                ListViewItem item = listViewMain.SelectedItems[0];
                EmergencyEditorForm form = new(ulong.Parse(item.Name));
                form.Show();
            }
        }

        private void RecalculateFileSystemSize()
        {
            if (Interlocked.Exchange(ref recalculatingFileSystemSize, 1) != 0)
                return;
            _ = Task.Run(async () =>
            {
                try
                {
                    foreach (ThingObjectLink link in Root.Content ?? [])
                    {
                        if (link.Type == FileType.folder)
                            await GetFolderSize(link.ID);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Could not recalculate folder sizes: {ex}");
                }
                finally
                {
                    Volatile.Write(ref recalculatingFileSystemSize, 0);
                }
            });
        }

        private void buttonExitApplication_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void contextMenuStrip_Opening(object sender, CancelEventArgs e)
        {
            UpdateContextMenuState();
        }

        private void UpdateContextMenuState()
        {
            int selectedCount = listViewMain.SelectedItems.Count;
            toolStripMenuItemRename.Enabled = selectedCount == 1;
            toolStripMenuItemEmergencyEditor.Enabled = selectedCount == 1;
            toolStripMenuItemCopy.Enabled = selectedCount > 0;
            _toolStripMenuItemMoveHere.Enabled = _pendingMoveIds.Count > 0;
        }

        private List<ulong> GetSelectedObjectIds()
        {
            return listViewMain.SelectedItems
                .Cast<ListViewItem>()
                .Select(item => ulong.Parse(item.Name))
                .ToList();
        }

        private bool CurrentFolderContainsName(string name, ulong? excludingId = null)
        {
            return _currentFolderContent.Any(link =>
                (!excludingId.HasValue || link.ID != excludingId.Value) &&
                string.Equals(link.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        private string GetUniqueNameForCurrentFolder(string requestedName, ISet<string> reservedNames)
        {
            HashSet<string> usedNames = _currentFolderContent
                .Select(link => link.Name)
                .Concat(reservedNames)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            return CreateUniqueName(requestedName, usedNames);
        }

        private static string CreateUniqueName(string requestedName, ISet<string> usedNames)
        {
            if (!usedNames.Contains(requestedName))
                return requestedName;

            for (int index = 2; ; index++)
            {
                string candidate = $"{requestedName} ({index})";
                if (!usedNames.Contains(candidate))
                    return candidate;
            }
        }

        private static List<string> FilterLargeImportFiles(List<string> files)
        {
            List<string> largeFiles = files
                .Where(File.Exists)
                .Where(file => new FileInfo(file).Length >= LargeFileWarningThreshold)
                .ToList();
            if (largeFiles.Count == 0)
                return files;

            long totalLargeSize = largeFiles.Sum(file => new FileInfo(file).Length);
            DialogResult result = MessageBox.Show(
                $"{largeFiles.Count} selected file(s) are at least {LargeFileWarningThreshold.Sizeify()} each " +
                $"({totalLargeSize.Sizeify()} total). This storage format still has to load imported file content into memory while encrypting it.\n\n" +
                "Import these large files anyway?",
                "Large import",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Warning);

            return result switch
            {
                DialogResult.Yes => files,
                DialogResult.No => files.Except(largeFiles, StringComparer.OrdinalIgnoreCase).ToList(),
                _ => []
            };
        }

        private List<ThingObjectLink> allFiles = [];
        private async void ToolStripMenuItemOpenRandom_Click(object sender, EventArgs e)
        {
            allFiles.Clear();
            foreach (ThingObjectLink link in Root.Content ?? [])
            {
                if (link.Type != FileType.folder)
                    continue;
                ThingFolder? rootFolder = await ThingData.LoadFileAsync<ThingFolder>(link.ID);
                if (rootFolder is not null)
                    await RecursiveFolderSearcher(rootFolder);
            }

            if (allFiles.Count == 0)
            {
                MessageBox.Show("No matching media files were found.", "Nothing to open",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            ulong randomID = allFiles[Random.Shared.Next(allFiles.Count)].ID;
            ThingFile? randomFile = await ThingData.LoadFileAsync<ThingFile>(randomID);
            if (randomFile is not null)
                OpenFile(randomFile);
        }

        private async Task RecursiveFolderSearcher(ThingFolder folder)
        {
            foreach (ThingObjectLink link in folder.Content)
            {
                if (link.Type is not FileType.folder and not FileType.other && link.Name is "0") // "0" is used by me for collections of images, where 0 is the first image in order and I do not want to open any other images but the first ones.
                {
                     allFiles.Add(link);
                }
                else if (link.Type == FileType.folder)
                {
                    ThingFolder? f = await ThingData.LoadFileAsync<ThingFolder>(link.ID);
                    ArgumentNullException.ThrowIfNull(f);
                    await RecursiveFolderSearcher(f);
                }
            }
        }

        public static void OpenFile(ThingFile file)
        {
            switch (file.Type)
            {
                case FileType.text:
                    TextEditorForm textEditor = new(file);
                    textEditor.Show();
                    break;
                case FileType.image:
                    ImageViewForm imageView = new(file);
                    imageView.Show();
                    break;
                case FileType.audio:
                case FileType.video:
                    VideoViewForm mediaPlayer = new(file);
                    mediaPlayer.Show();
                    break;
                case FileType.other:
                    long releasedBytes = file.Content?.LongLength ?? 0;
                    file.ReleaseContent();
                    MemoryMaintenance.NotifyLargeBufferReleased(releasedBytes);
                    MessageBox.Show(
                        "Viewer for this file type is not integrated. To view this file, please export it so other applications can read its contents.",
                        "Not integrated", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    break;
                default:
                    throw new InvalidEnumArgumentException("Unknown internal file type.");
            }
        }

        private sealed class ActivityMessageFilter(Action activityCallback) : IMessageFilter
        {
            private const int WmKeyDown = 0x0100;
            private const int WmSysKeyDown = 0x0104;
            private const int WmLButtonDown = 0x0201;
            private const int WmRButtonDown = 0x0204;
            private const int WmMButtonDown = 0x0207;
            private const int WmMouseWheel = 0x020A;

            public bool PreFilterMessage(ref Message m)
            {
                if (m.Msg is WmKeyDown or WmSysKeyDown or
                    WmLButtonDown or WmRButtonDown or WmMButtonDown or WmMouseWheel)
                {
                    activityCallback();
                }

                return false;
            }
        }
    }
}
