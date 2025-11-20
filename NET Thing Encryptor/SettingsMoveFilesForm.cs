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

        private async void SettingsMoveFilesForm_Load(object sender, EventArgs e)
        {
            progressBar.Maximum = files.Count;
            progressBar.Value = 0;

            int count = 0;
            ThingData.Saving++;

            await Task.Run(() =>
            {
                Parallel.ForEach(files, (file) =>
                {
                    File.Copy(file.FullName, Path.Combine(path, file.Name), true);
                    File.Delete(file.FullName);

                    int current = Interlocked.Increment(ref count);

                    this.Invoke(() =>
                    {
                        progressBar.Value = current;
                        label.Text = $"Moving {current}/{progressBar.Maximum} files...";
                    });
                });
            });

            ThingData.Saving--;
            this.Close();
        }
    }
}
