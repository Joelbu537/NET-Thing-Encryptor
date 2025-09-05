using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;

namespace NET_Thing_Encryptor
{
    public partial class FormMain : Form
    {
        public event EventHandler FolderChanged;
        private ThingFolder? CurrentFolder;
        private ulong _currentFolderID = 1;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
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

        private async void FormMain_Load(object sender, EventArgs e)
        {
            ThingFolder folder = new ThingFolder("TestFolder").AddToRoot();
            await ThingData.SaveFileAsync(folder);
            await ThingData.SaveRootAsync();

            CurrentFolderID = 0;
        }
        private async void OnFolderChanged(object sender, EventArgs e)
        {
            Debug.WriteLine("Folder changed. Refreshing items.");
            listViewMain.Items.Clear();
            if(CurrentFolderID == 0)
            {
                CurrentFolder = null;
            }
            else
            {
                CurrentFolder = await ThingData.LoadFileAsync(CurrentFolderID) as ThingFolder ?? throw new ArgumentException();
            }
            List<ThingObjectLink> new_content = await ThingData.LoadFolderContent(CurrentFolderID);
            Debug.WriteLine($"Loaded folder contains {new_content.Count} items");

            foreach (ThingObjectLink o in new_content)
            {
                Debug.WriteLine($"Loading item: {o.Name} (ID: {o.ID}, Type: {o.Type}, CreatedAt: {o.CreatedAt})");

                ListViewItem item = new ListViewItem(o.Name);
                item.Name = o.ID.ToString();
                item.ImageKey = o.Type.ToString();

                FileInfo inf = new FileInfo(ThingData.GetFilePath(o.ID));
                item.SubItems.Add(inf.Length.Sizeify().ToString());
                item.SubItems.Add(o.CreatedAt.ToString());

                listViewMain.Items.Add(item);
            }
        }

        private async void listViewMain_DoubleClick(object sender, EventArgs e)
        {
            if (listViewMain.SelectedItems.Count > 0)
            {
                ListViewItem item = listViewMain.SelectedItems[0];
                Debug.WriteLine($"Doppelklick auf {item.Text} mit ID {item.Name}");
                ThingObject? obj = await ThingData.LoadFileAsync(ulong.Parse(item.Name));
                if (obj is ThingFolder folder)
                {
                    CurrentFolderID = folder.ID;
                }
                else if (obj is ThingFile file)
                {
                    Debug.WriteLine($"File: {file.Name} MD5: {file.MD5Hash}");
                    // Open File
                    throw new NotImplementedException();
                }
            }
        }

        private async void buttonNavigationBack_Click(object sender, EventArgs e)
        {
            if (CurrentFolderID != 0 && CurrentFolder != null)
            {
                CurrentFolderID = CurrentFolder.ParentID;
            }
        }

        private async void buttonNavigationCreateFile_Click(object sender, EventArgs e)
        {
            // Implement creation dialog
            throw new NotImplementedException();
        }

        private async void buttonNavigationCreateFolder_Click(object sender, EventArgs e)
        {
            ThingFolder newFolder = new ThingFolder("New Folder created :)").AddToRoot();
            await ThingData.SaveFileAsync(newFolder);
            await ThingData.MoveFolderToFolderAsync(newFolder.ID, CurrentFolderID);
            if(CurrentFolderID == 0)
            {
                await ThingData.SaveRootAsync();
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
                Debug.WriteLine($"Lösche {item.Text} mit ID {item.Name}");

                ThingObject? deletion = await ThingData.LoadFileAsync(ulong.Parse(item.Name));
                if(deletion is ThingFolder)
                {
                    DialogResult r = MessageBox.Show($"Lösche Ordner {deletion.Name} und alle seine Inhalte.",
                        "Folder deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if(r == DialogResult.No)
                    {
                        return;
                    }
                    await ThingData.DeleteObject(deletion);
                }
                else if(deletion is ThingFile)
                {
                    await ThingData.DeleteFile(deletion.ID);
                }
            }
        }

        private void buttonNavigationRoot_Click(object sender, EventArgs e)
        {
            if (CurrentFolderID != 0)
            {
                CurrentFolderID = 0;
            }
        }
    }
}