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
    public partial class CreateFolderForm : Form
    {
        public CreateFolderForm()
        {
            InitializeComponent();
            ApplyTheme();
        }
        public CreateFolderForm(string name)
        {
            InitializeComponent();
            ApplyTheme();
            textBox.Text = name;
            label.Text = "Change Name to:";
            textBox1_TextChanged(this, EventArgs.Empty);
        }

        private void ApplyTheme()
        {
            bool darkMode = ThingData.Root?.DarkMode ?? true;
            AppTheme.Apply(this, darkMode);
            AppTheme.StylePrimaryButton(buttonOK, darkMode);
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if(textBox.Text.Length > 0)
            {
                buttonOK.Enabled = true;
            }
            else
            {
                buttonOK.Enabled = false;
            }
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            if(textBox.Text.Length == 0)
            {
                return;
            }
            this.DialogResult = DialogResult.OK;
            this.Name = textBox.Text;
            this.Close();
        }
    }
}
