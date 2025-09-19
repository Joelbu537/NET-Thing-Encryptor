using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace NET_Thing_Encryptor
{
    public partial class FormMain : Form
    {
        public event EventHandler FolderChanged;
        private ThingFolder? CurrentFolder;
        private ulong _currentFolderID = 1;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        //[Browsable(false)]
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

            ThingData.SaveStatusChanged += (o, e) =>
            {
                if (ThingData.Saving > 0)
                {
                    labelInfoSaving.Visible = true;
                }
                else
                {
                    labelInfoSaving.Visible = false;
                }
            };
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            Debug.WriteLine($"FormMain ist {System.Threading.Thread.CurrentThread.GetApartmentState()}");

            CurrentFolderID = 0;
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

            foreach (ThingObjectLink o in new_content)
            {
                Debug.WriteLine($"  - {o.Name} ({o.Type}, ID {o.ID})");
                if(o.Type == FileType.folder) folderCount++;
                else fileCount++;

                ListViewItem item = new ListViewItem(o.Name);
                item.Name = o.ID.ToString();
                item.ImageKey = o.Type.ToString();

                item.SubItems.Add(o.Size.Sizeify().ToString());
                totalSize += o.Size;
                item.SubItems.Add(o.CreatedAt.ToString());

                listViewMain.Items.Add(item);
            }

            labelInfoFileCount.Text = $"Files: {fileCount}";
            labelInfoFolderCount.Text = $"Folders: {folderCount}";
            labelInfoTotalSize.Text = $"Total Size: {totalSize.Sizeify()}";
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
                Debug.WriteLine("nichts!");
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

        private void buttonNavigationCreateFile_Click(object sender, EventArgs e)
        {
            if(CurrentFolder == null && CurrentFolderID == 0)
            {
                MessageBox.Show("You cannot create files in the root directory. Please create a folder first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                ArgumentNullException.ThrowIfNull(CurrentFolder);
                using CreateFileForm form = new(CurrentFolder);
                form.ShowDialog();
                CurrentFolderID = CurrentFolderID;
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
            // Implement creation dialog
            //throw new NotImplementedException();
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
                CurrentFolderID = CurrentFolderID;
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

        private void listViewMain_SelectedIndexChanged(object sender, EventArgs e)
        {

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
    }
}