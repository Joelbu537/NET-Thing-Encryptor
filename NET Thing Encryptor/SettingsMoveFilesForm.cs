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
            progressBar.Maximum = files.Count;
            progressBar.Value = 0;
            Parallel.ForEach(files, (file) =>
            {
                File.Copy(file.FullName, Path.Combine(path, file.Name), true);
                File.Delete(file.FullName);
                if(progressBar.InvokeRequired)
                {
                    progressBar.Invoke(() =>
                    {
                        progressBar.Value += 1;
                    });
                }
                else{
                    progressBar.Value += 1;
                }
                if(label.InvokeRequired)
                {
                    label1.Invoke( () =>
                    {
                        label.Text = $"Moving {progressBar.Value}/{progressBar.Maximum} files...";
                   }
                );}
                else
                {
                    label.Text = $"Moving {progressBar.Value}/{progressBar.Maximum} files...";
                }
            });
        }
    }
}
