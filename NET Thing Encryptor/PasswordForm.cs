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
            if (ThingData.LoadMainData().Result)
            {
                InitializeComponent();
            }
            else
            {
                MessageBox.Show("Failed to load main data. Please check the configuration.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); // Change Text, maybe add auto repair options.
                Application.Exit();
            }
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
                    MessageBox.Show("Incorrect password. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    textBoxPassword.Enabled = true;
                    buttonContinue.Enabled = true;
                }
            }
        }
    }
}
