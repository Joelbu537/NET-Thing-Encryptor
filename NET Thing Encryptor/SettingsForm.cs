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
            checkBoxDarkMode.Checked = ThingData.Root.DarkMode;
            numericPreviousImageBuffer.Value = ThingData.Root.ImageViewerPreviousBufferCount;
            numericNextImageBuffer.Value = ThingData.Root.ImageViewerNextBufferCount;
        }

        private async void buttonApply_Click(object sender, EventArgs e)
        {
            if (buttonApply.Enabled == false) return;
            buttonApply.Enabled = false;
            buttonCancel.Enabled = false;
            try
            {
                ArgumentNullException.ThrowIfNull(ThingData.Root);

                // Save Location
                string pathSave = Path.GetFullPath(textBoxSaveLocation.Text);
                if (pathSave != Path.GetFullPath(ThingData.Root.SaveLocation))
                {
                    Directory.CreateDirectory(pathSave);

                    List<FileInfo> files = new DirectoryInfo(Path.GetFullPath(ThingData.Root.SaveLocation))
                        .GetFiles("*.nte").ToList();
                    files.RemoveAll(f => f.Name == "0.nte");

                    using SettingsMoveFilesForm moveForm = new(files, pathSave);
                    if (moveForm.ShowDialog() != DialogResult.OK)
                        throw new IOException("The encrypted files were not moved completely.");
                    ThingData.Root.SaveLocation = pathSave;
                }

                // Import Location
                string pathImport = Path.GetFullPath(textBoxImportLocation.Text);
                Directory.CreateDirectory(pathImport);
                ThingData.Root.ImportLocation = pathImport;

                // Export Location
                string pathExport = Path.GetFullPath(textBoxExportLocation.Text);
                Directory.CreateDirectory(pathExport);
                ThingData.Root.ExportLocation = pathExport;

                // Dark Mode
                ThingData.Root.DarkMode = checkBoxDarkMode.Checked;

                // Image viewer buffering
                ThingData.Root.ImageViewerPreviousBufferCount =
                    decimal.ToInt32(numericPreviousImageBuffer.Value);
                ThingData.Root.ImageViewerNextBufferCount =
                    decimal.ToInt32(numericNextImageBuffer.Value);

                await ThingData.SaveRootAsync();
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"The settings could not be saved: {ex.Message}",
                    "Settings error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                buttonApply.Enabled = true;
                buttonCancel.Enabled = true;
            }
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            if (!buttonCancel.Enabled) return;
            this.Close();
        }
        private string? ExplorerDialog(string title, string initialDirectory)
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Multiselect = false,
                InitialDirectory = initialDirectory,
                Title = title,
                ShowHiddenItems = true
            };
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                return dialog.FileName;
            }
            return null;
        }
        private void buttonSaveLocation_Click(object sender, EventArgs e)
        {
            buttonSaveLocation.Enabled = false;
            if (this.InvokeRequired)
            {
                this.Invoke((Action)(() => buttonSaveLocation_Click(sender, e)));
                return;
            }

            string? path = ExplorerDialog("Select new save location", Directory.GetCurrentDirectory());
            if (path is not null)
                textBoxSaveLocation.Text = path;

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

            string? path = ExplorerDialog(
                "Select new default import directory",
                Directory.GetCurrentDirectory());
            if (path is not null)
                textBoxImportLocation.Text = path;

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

            string? path = ExplorerDialog(
                "Select new default export directory",
                Directory.GetCurrentDirectory());
            if (path is not null)
                textBoxExportLocation.Text = path;

            buttonExportLocation.Enabled = true;
        }
    }
}
