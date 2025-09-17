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
    public partial class CreateFileForm : Form
    {
        private ThingFolder currentFolder;
        public CreateFileForm(ThingFolder currentFolder)
        {
            this.currentFolder = currentFolder;
            InitializeComponent();
        }

        private void comboBox_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void CreateFileForm_Load(object sender, EventArgs e)
        {

        }
    }
}
