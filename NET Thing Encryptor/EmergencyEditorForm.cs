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
using Windows.Security.Cryptography.Core;

namespace NET_Thing_Encryptor
{
    public partial class EmergencyEditorForm : Form
    {
        private string filePath;
        private string MD5;
        private ulong ID;
        private ThingObject? obj;
        public EmergencyEditorForm(ulong id)
        {
            ID = id;
            InitializeComponent();
        }

        private async void EmergencyEditorForm_Load(object sender, EventArgs e)
        {
            filePath = ThingData.GetFilePath(ID);
            Debug.WriteLine($"Emergency file path resolved to: {filePath}");

            using FileStream fs = File.OpenRead(filePath);
            using var decrypted = await ThingData.Decrypt(fs);
            Debug.WriteLine("Reading file...");

            byte[] data = decrypted.ToArray();
            Debug.WriteLine("Read " + data.Length + " bytes.");

            string text = Encoding.UTF8.GetString(data);
            File.WriteAllText($"{ThingData.IDToHex(ID)} - Emergency Copy at {DateTime.Now.ToString().Replace(':', '-')}.txt", text);
            MessageBox.Show("A DECRYPTED emergency copy of this file has been saved in the program folder as a plain text backup.", "Security compromised!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            MD5 = ThingData.ComputeMD5Hash(data);
            textBox.Text = text;
        }

        private async void EmergencyEditorForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (MD5 != ThingData.ComputeMD5Hash(Encoding.UTF8.GetBytes(textBox.Text)))
            {
                var result = MessageBox.Show("The file has been modified. Do you want to save the changes?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    byte[] data = Encoding.UTF8.GetBytes(textBox.Text);
                    using MemoryStream ms = new(data);

                    var encrtypted = await ThingData.Encrypt(ms);

                    using FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
                    await encrtypted.CopyToAsync(fs);
                    await fs.FlushAsync();
                }
            }
        }
    }
}
