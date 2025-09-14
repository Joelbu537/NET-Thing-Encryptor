using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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
        }

        private async void buttonApply_Click(object sender, EventArgs e)
        {
            if(buttonApply.Enabled == false) return;
            buttonApply.Enabled = false;
            buttonCancel.Enabled = false;
            ArgumentNullException.ThrowIfNull(ThingData.Root);
            // Save Location
            string path = Path.GetFullPath(textBoxSaveLocation.Text);
            if (path != Path.GetFullPath(ThingData.Root.SaveLocation))
            {
                Directory.CreateDirectory(path);

                List<FileInfo> files = new DirectoryInfo(Path.GetFullPath(ThingData.Root.SaveLocation)).GetFiles("*.nte").ToList<FileInfo>();
                files.RemoveAll(f => f.Name == "0.nte");

                using SettingsMoveFilesForm moveForm = new SettingsMoveFilesForm(files, path);
                moveForm.ShowDialog();
                ThingData.Root.SaveLocation = path;
                await ThingData.SaveRootAsync();
            }
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            if(buttonCancel.Enabled == false) return;
            this.Close();
        }

        private void buttonSaveLocation_Click(object sender, EventArgs e)
        {
            buttonSaveLocation.Enabled = false;
            if (this.InvokeRequired)
            {
                this.Invoke((Action)(() => buttonSaveLocation_Click(sender, e)));
                return;
            }

            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Multiselect = false,
                InitialDirectory = ThingData.Root.SaveLocation,
                Title = "Select new save location",
                ShowHiddenItems = true
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                textBoxSaveLocation.Text = dialog.FileName;
            }
            buttonSaveLocation.Enabled = true;
        }
    }
}
