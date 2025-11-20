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
    public partial class PasswordForm : Form
    {
        public PasswordForm()
        {
            InitializeComponent();
            
            this.Text = $"Enter Password - NET Thing Encryptor v{Program.Version}";
        }

        private void textBoxPassword_TextChanged(object sender, EventArgs e)
        {
            if(textBoxPassword.Text.Length >= 8)
            {
                buttonContinue.Enabled = true;
            }
            else
            {
                buttonContinue.Enabled = false;
            }
        }

        private async void buttonContinue_Click(object sender, EventArgs e)
        {
            if(textBoxPassword.Text.Length >= 8)
            {
                textBoxPassword.Enabled = false;
                buttonContinue.Enabled = false;
                bool pwdCorrect = await ThingData.AttemptDecrypt(textBoxPassword.Text);
                if(pwdCorrect)
                {
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    textBoxPassword.Text = string.Empty;
                    MessageBox.Show("Incorrect password. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    textBoxPassword.Enabled = true;
                    buttonContinue.Enabled = true;
                    GC.Collect();
                    textBoxPassword.Focus();
                }
            }
        }
        private async void buttonCancel_Click(object sender, EventArgs e)
        {
            Application.Exit();
            this.Close();
            return;
        }
    }
}
