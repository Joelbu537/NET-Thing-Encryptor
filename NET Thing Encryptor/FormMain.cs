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

            foreach (ThingObjectLink o in new_content)
            {
                Debug.WriteLine($"  - {o.Name} ({o.Type}, ID {o.ID})");

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
                    textBoxNavigation.Text += $@"/{folder.Name}";
                }
                else if (obj is ThingFile file)
                {
                    Debug.WriteLine($"File: {file.Name} MD5: {file.MD5Hash}");
                    switch (file.Type)
                    {
                        case FileType.text:
                            break;
                        case FileType.image:
                            break;
                        case FileType.audio:
                            break;
                        case FileType.video:
                            break;
                        case FileType.other:
                            break;

                    }
                    throw new NotImplementedException();
                }
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
            // Implement creation dialog
            throw new NotImplementedException();
        }

        private async void buttonNavigationCreateFolder_Click(object sender, EventArgs e)
        {
            using CreateFolderForm form = new CreateFolderForm();
            if(form.ShowDialog() == DialogResult.OK)
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
    }
}