using Microsoft.WindowsAPICodePack.Dialogs;

namespace NET_Thing_Encryptor
{
    public partial class SettingsForm : Form
    {
        public SettingsForm()
        {
            InitializeComponent();
        }

        private void SettingsForm_Load(object sender, EventArgs e)
        {
            ArgumentNullException.ThrowIfNull(ThingData.Root);
            textBoxSaveLocation.Text = Path.GetFullPath(ThingData.Root.SaveLocation);
            textBoxImportLocation.Text = Path.GetFullPath(ThingData.Root.ImportLocation);
            textBoxExportLocation.Text = Path.GetFullPath(ThingData.Root.ExportLocation);
        }

        private async void buttonApply_Click(object sender, EventArgs e)
        {
            if (buttonApply.Enabled == false) return;
            buttonApply.Enabled = false;
            buttonCancel.Enabled = false;
            ArgumentNullException.ThrowIfNull(ThingData.Root);

            // Save Location
            string pathSave = Path.GetFullPath(textBoxSaveLocation.Text);
            if (pathSave != Path.GetFullPath(ThingData.Root.SaveLocation))
            {
                Directory.CreateDirectory(pathSave);

                List<FileInfo> files = new DirectoryInfo(Path.GetFullPath(ThingData.Root.SaveLocation)).GetFiles("*.nte").ToList<FileInfo>();
                files.RemoveAll(f => f.Name == "0.nte");

                using SettingsMoveFilesForm moveForm = new SettingsMoveFilesForm(files, pathSave);
                moveForm.ShowDialog();
                ThingData.Root.SaveLocation = pathSave;
            }

            // Import Location
            string pathImport = Path.GetFullPath(textBoxImportLocation.Text);
            if (!Directory.Exists(pathImport))
            {
                Directory.CreateDirectory(pathImport);
            }
            ThingData.Root.ImportLocation = pathImport;

            // Export Location
            string pathExport = Path.GetFullPath(textBoxExportLocation.Text);
            if (!Directory.Exists(pathExport))
            {
                Directory.CreateDirectory(pathExport);
            }
            ThingData.Root.ExportLocation = pathExport;


            await ThingData.SaveRootAsync();
            this.Close();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            if (!buttonCancel.Enabled) return;
            this.Close();
        }
        private string[] ExplorerDialog(string title, bool folderPicker = false, bool multiSelect = true, string initialDirectory = "C:\\")
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = folderPicker,
                Multiselect = multiSelect,
                InitialDirectory = initialDirectory,
                Title = title,
                ShowHiddenItems = true
            };
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                return dialog.FileNames.ToArray();
            }
            return Array.Empty<string>();
        }
        private void buttonSaveLocation_Click(object sender, EventArgs e)
        {
            buttonSaveLocation.Enabled = false;
            if (this.InvokeRequired)
            {
                this.Invoke((Action)(() => buttonSaveLocation_Click(sender, e)));
                return;
            }

            textBoxSaveLocation.Text = ExplorerDialog(
                title: "Select new save location",
                folderPicker: false,
                multiSelect: false,
                initialDirectory: Directory.GetCurrentDirectory())[0];

            buttonSaveLocation.Enabled = true;
        }
        private void buttonImportLocation_Click(object sender, EventArgs e)
        {
            buttonImportLocation.Enabled = false;
            if (this.InvokeRequired)
            {
                this.Invoke((Action)(() => buttonImportLocation_Click(sender, e)));
                return;
            }

            textBoxImportLocation.Text = ExplorerDialog(
                title: "Select new default import directory",
                folderPicker: true,
                multiSelect: false,
                initialDirectory: Directory.GetCurrentDirectory())[0];

            buttonImportLocation.Enabled = true;
        }
        private void buttonExportLocation_Click(object sender, EventArgs e)
        {
            buttonExportLocation.Enabled = false;
            if (this.InvokeRequired)
            {
                this.Invoke((Action)(() => buttonExportLocation_Click(sender, e)));
                return;
            }

            textBoxExportLocation.Text = ExplorerDialog(
                title: "Select new default export directory",
                folderPicker: true,
                multiSelect: false,
                initialDirectory: Directory.GetCurrentDirectory())[0];

            buttonExportLocation.Enabled = true;
        }
    }
}
