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
    public partial class SettingsMoveFilesForm : Form
    {
        List<FileInfo> files;
        string path;
        public SettingsMoveFilesForm(List<FileInfo> files, string targetDirectory)
        {
            InitializeComponent();
            this.files = files;
            path = targetDirectory;
        }

        private void SettingsMoveFilesForm_Load(object sender, EventArgs e)
        {
            progressBar.Maximum = files.Count * 2;
            progressBar.Value = 0;
            Parallel.ForEach(files, (file) =>
            {
                File.Copy(file.FullName, Path.Combine(path, file.Name), true);
                this.Invoke(() =>
                {
                    progressBar.Value += 1;
                    label.Text = $"Moving {progressBar.Value}/{progressBar.Maximum} files...";
                });
            });
            Parallel.ForEach(files, (file) =>
            {
                File.Delete(file.FullName);
                this.Invoke(() =>
                {
                    progressBar.Value += 1;
                    label.Text = $"Deleting {progressBar.Value - files.Count}/{progressBar.Maximum / 2} files...";
                });
            });
        }
    }
}
