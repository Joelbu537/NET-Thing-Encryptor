using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
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

        private void buttonApply_Click(object sender, EventArgs e)
        {
            ArgumentNullException.ThrowIfNull(ThingData.Root);
            // Save Location
            string path = Path.GetFullPath(textBoxSaveLocation.Text);
            if (path != Path.GetFullPath(ThingData.Root.SaveLocation))
            {
                Directory.CreateDirectory(path);
                ThingData.Root.SaveLocation = path;
                // Move Files
                List<FileInfo> files = new DirectoryInfo(Path.GetFullPath(ThingData.Root.SaveLocation)).GetFiles("*.nte").ToList<FileInfo>();
                files.RemoveAll(f => f.Name == "0.nte");

                using SettingsMoveFilesForm moveForm = new SettingsMoveFilesForm(files, path);
                moveForm.ShowDialog();


            }
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {

        }
    }
}
