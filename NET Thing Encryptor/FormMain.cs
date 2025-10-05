using Microsoft.WindowsAPICodePack.Dialogs;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace NET_Thing_Encryptor
{
    public partial class FormMain : Form
    {
        public event EventHandler FolderChanged;
        private ThingFolder? CurrentFolder;
        private ulong _currentFolderID = 1;

        public int version = 45;

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
            FolderChanged += OnFolderChanged;
        }
        private void FormMain_Load(object sender, EventArgs e)
        {
            Debug.WriteLine($"FormMain ist {System.Threading.Thread.CurrentThread.GetApartmentState()}");

            labelInfoVersion.Text = $"V {version}";
            CurrentFolderID = 0;
            _ = Task.Run(() =>
            {
                while (true)
                {
                    if(ThingData.Saving > 0)
                    {
                        if (labelInfoSaving.InvokeRequired)
                        {
                            labelInfoSaving.Invoke((Action)(() => labelInfoSaving.Visible = true));
                        }
                        else
                        {
                            labelInfoSaving.Visible = true;
                        }
                    }
                    else
                    {
                        if (labelInfoSaving.InvokeRequired)
                        {
                            labelInfoSaving.Invoke((Action)(() => labelInfoSaving.Visible = false));
                        }
                        else
                        {
                            labelInfoSaving.Visible = false;
                        }
                    }
                    Task.Delay(250).Wait();
                }
            }); // Save indicator clock
            _ = Task.Run(() =>
            {
                foreach(ThingObjectLink link in ThingData.Root.Content)
                {
                    _ = GetFolderSize(link.ID);
                }
            }); // Recalculate Folder Sizes
        }

        private async void OnFolderChanged(object sender, EventArgs e)
        {
            Debug.WriteLine("Folder changed. Refreshing items.");
            listViewMain.Items.Clear();
            if (CurrentFolderID == 0)
            {
                CurrentFolder = null;
            }
            else
            {
                CurrentFolder = await ThingData.LoadFileAsync(CurrentFolderID) as ThingFolder ?? throw new ArgumentException();
            }
            List<ThingObjectLink> new_content = await ThingData.LoadFolderContent(CurrentFolderID);
            Debug.WriteLine($"Loaded folder contains {new_content.Count} items:");

            int folderCount = 0;
            int fileCount = 0;
            long totalSize = 0;

            List<ListViewItem> items = new();

            foreach (ThingObjectLink o in new_content)
            {
                Debug.WriteLine($"  - {o.Name} ({o.Type}, ID {o.ID})");
                if (o.Type == FileType.folder) folderCount++;
                else fileCount++;

                ListViewItem item = new ListViewItem(o.Name);
                item.Name = o.ID.ToString();
                item.ImageKey = o.Type.ToString();

                item.SubItems.Add(o.Size.Sizeify().ToString());
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
            labelInfoTotalSize.Text = $"Total size on disk: {totalSize.Sizeify()}";
            if (CurrentFolderID == 0)
            {
                labelInfoTotalSize.Text = "Total Size: " + new DirectoryInfo(ThingData.Root.SaveLocation).EnumerateFiles("*", SearchOption.TopDirectoryOnly).Sum(f => f.Length).Sizeify();
            }
            else
            {
                labelInfoTotalSize.Text = $"Total Size: {totalSize.Sizeify()}";
            }
        }
        private async void listViewMain_DoubleClick(object sender, EventArgs e)
        {
            Debug.Write("Doppelklick auf ");
            if (listViewMain.SelectedItems.Count > 0)
            {
                ListViewItem item = listViewMain.SelectedItems[0];
                Debug.WriteLine($"{item.Text} mit ID {item.Name}");
                ThingObject? obj = await ThingData.LoadFileAsync(ulong.Parse(item.Name));
                if (obj is ThingFolder folder)
                {
                    CurrentFolderID = folder.ID;
                    textBoxNavigation.Text += $@"/{folder.Name}";
                }
                else if (obj is ThingFile file)
                {
                    Debug.WriteLine($"File: {file.Name} MD5: {file.MD5Hash} Type: {file.Type.ToString()}");
                    switch (file.Type) // Warum ist es immer OTHER ?????
                    {
                        case FileType.text:
                            MessageBox.Show("Text preview is not implemented yet.", "Not implemented", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            break;
                        case FileType.image:
                            {
                                ImageViewForm imageView = new(file);
                                imageView.Show();
                            }
                            break;
                        case FileType.audio:
                            MessageBox.Show("Audio preview is not implemented yet.", "Not implemented", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            break;
                        case FileType.video:
                            MessageBox.Show("Video preview is not implemented yet.", "Not implemented", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            break;
                        case FileType.other:
                            MessageBox.Show("Viewer for this file type is not integrated. To view this file, please export it so other applications can read its contents.", "Not integrated", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            break;
                        default:
                            throw new InvalidEnumArgumentException("Unknown internal file type.");
                    }
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
                    },
                };

                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    List<string> selectedFiles = dialog.FileNames.ToList();

                    foreach (string file in selectedFiles)
                    {
                        if (!File.Exists(file))
                        {
                            MessageBox.Show($"The file {file} does not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            continue;
                        }
                        ThingFile? newFile = new ThingFile(System.IO.Path.GetFileName(file), File.ReadAllBytes(file));
                        Enum.TryParse<FileType>(ThingData.GetFileType(file), true, out FileType result);
                        newFile.Type = result;
                        Enum.TryParse<FileExtension>(Path.GetExtension(file).TrimStart('.'), true, out FileExtension extResult);
                        newFile.Extension = extResult;

                        await ThingData.MoveFileToFolderAsync(newFile, CurrentFolderID);
                        await ThingData.SaveFileAsync(newFile);

                        ThingFolder? currentFolderTemp = await ThingData.LoadFileAsync<ThingFolder>(CurrentFolder.ID);

                        ThingObjectLink? link = currentFolderTemp.Content.Where(l => l.ID == newFile.ID).FirstOrDefault();
                        ArgumentNullException.ThrowIfNull(link, "Imported file link not found in folder after import.");


                        link.Size = (long)(new FileInfo(file).Length);
                        link.Type = result;
                        await ThingData.SaveFileAsync(currentFolderTemp);
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
                ThingFolder newFolder = new ThingFolder(form.Name).AddToRoot();
                await ThingData.SaveFileAsync(newFolder);
                if (CurrentFolderID == 0)
                {
                    await ThingData.SaveRootAsync();
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
            if (listViewMain.SelectedItems.Count > 0)
            {
                ListViewItem item = listViewMain.SelectedItems[0];
                Debug.WriteLine($"Deleting {item.Text}");

                ThingObject? deletion = await ThingData.LoadFileAsync(ulong.Parse(item.Name));
                if (deletion is ThingFolder)
                {
                    DialogResult r = MessageBox.Show($"Are you sure you want to delete {deletion.Name} and all of its contents?",
                        "Folder deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (r == DialogResult.Yes)
                    {
                        await ThingData.DeleteObject(deletion);
                    }
                }
                else if (deletion is ThingFile)
                {
                    await ThingData.DeleteFile(deletion.ID);
                }
                listViewMain.Items.RemoveAt(listViewMain.SelectedIndices[0]);
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
                        MessageBox.Show("Please wait until the saving process has finished!", "Saving in progress", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            ShutdownBlockReasonDestroy(this.Handle);
        }

        // Auf Multiselect umstellen!!!
        private async void buttonNavigationExport_Click(object sender, EventArgs e)
        {
            if (listViewMain.SelectedItems.Count <= 0)
                return;

            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Multiselect = false,
                Title = "Select target directory"
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                string path = dialog.FileName;
                ListViewItem item = listViewMain.SelectedItems[0];
                ThingObject? o = await ThingData.LoadFileAsync(ulong.Parse(item.Name));
                if (o is ThingFolder f)
                {
                    foreach (ThingObjectLink link in f.Content)
                    {
                        await ExportFile(link, path);
                    }
                }
                else
                {
                    MessageBox.Show("Exporting files is not implemented yet. Please export the parent folder instead!", "Not implemented", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        private async Task ExportFile(ThingObjectLink file, string path)
        {
            ThingData.Saving++;
            if (file.Type == FileType.folder)
            {
                ThingFolder? f = await ThingData.LoadFileAsync<ThingFolder>(file.ID);
                ArgumentNullException.ThrowIfNull(f);

                foreach (ThingObjectLink o in f.Content)
                {
                    await ExportFile(o, Path.Combine(path, f.Name));
                }
            }
            else
            {
                ThingFile? f = await ThingData.LoadFileAsync<ThingFile>(file.ID);
                ArgumentNullException.ThrowIfNull(f);

                if (f.Content == null || f.Content.Length == 0)
                {
                    MessageBox.Show($"File {f.Name} has no content.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                string filePath = Path.Combine(path, f.Name);
                File.WriteAllBytes(filePath, f.Content);
            }
            ThingData.Saving--;
        }

        private void listViewMain_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var item = listViewMain.GetItemAt(e.X, e.Y);
                if (item != null)
                {
                    listViewMain.FocusedItem = item;
                    contextMenuStrip.Show(listViewMain, e.Location);
                }
            }
        }

        private async void toolStripMenuItemRename_Click(object sender, EventArgs e)
        {
            if(listViewMain.SelectedItems.Count > 0)
            {
                ListViewItem item = listViewMain.SelectedItems[0];
                CreateFolderForm form = new(item.Text);
                if(form.ShowDialog() == DialogResult.OK)
                {
                    string newName = form.Name;
                    ThingObject? o = await ThingData.LoadFileAsync(ulong.Parse(item.Name));
                    ArgumentNullException.ThrowIfNull(o);
                    o.Name = newName;
                    await ThingData.SaveFileAsync(o);
                    if(CurrentFolder == null)
                    {
                        ThingData.Root.Content.FirstOrDefault(l => l.ID == o.ID).Name = newName;
                        await ThingData.SaveRootAsync();
                    }
                    else
                    {
                        CurrentFolder.Content.FirstOrDefault(l => l.ID == o.ID).Name = newName;
                        await ThingData.SaveFileAsync(CurrentFolder);
                    }
                    CurrentFolderID = CurrentFolderID;
                    listViewMain.SelectedItems.Clear();
                }
            }
        }
        private async Task<long> GetFolderSize(ulong folderID)
        {
            ThingFolder? folder = await ThingData.LoadFileAsync<ThingFolder>(folderID);
            long folder_size = 0;
            foreach(ThingObjectLink link in folder.Content)
            {
                if(link.Type == FileType.folder)
                {
                    folder_size += await GetFolderSize(link.ID);
                }
                else
                {
                    folder_size += link.Size;
                }
            }
            if(folder.ParentID == 0)
            {
                ThingData.Root.Content.FirstOrDefault(l => l.ID == folder.ID).Size = folder_size;
                await ThingData.SaveRootAsync();
            }
            else
            {
                ThingFolder? parentFolder = await ThingData.LoadFileAsync<ThingFolder>(folder.ParentID);
                parentFolder.Content.FirstOrDefault(l => l.ID == folder.ID).Size = folder_size;
                await ThingData.SaveFileAsync(parentFolder);
            }
            return folder_size;
        }
    }
}