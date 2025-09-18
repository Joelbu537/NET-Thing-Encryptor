using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NET_Thing_Encryptor
{
    public partial class ImageViewForm : Form
    {
        private readonly List<ThingObjectLink> Images = new();
        private event EventHandler OnIndexChanged;
        private int _index = 0;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Index
        {
            get
            {
                return _index;
            }
            set
            {
                if (_index == value) { return; }
                _index = Math.Clamp(value, 0, Images.Count - 1);
                OnIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public ImageViewForm(ThingFile file)
        {
            Debug.WriteLine($"Opening ImageViewForm for file {file.Name} (ID {file.ID})");
            if (file.Type != FileType.image)
            {
                MessageBox.Show("The provided file is not an image.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            ThingFolder? parentFolder = ThingData.LoadFileAsync<ThingFolder>(file.ParentID).Result;
            ArgumentNullException.ThrowIfNull(parentFolder);
            Images = parentFolder.Content.Where((x) => x.Type == FileType.image).ToList();
            int selectedIndex = Images.IndexOf(Images.FirstOrDefault((x) => x.ID == file.ID));

            OnIndexChanged += RefreshImage;
            this.KeyPreview = true;

            InitializeComponent();

            Index = selectedIndex;
        }
        private async void RefreshImage(object? o, EventArgs e)
        {
            ThingFile? imageFile = await ThingData.LoadFileAsync<ThingFile>(Images[Index].ID);
            ArgumentNullException.ThrowIfNull(imageFile);
            pictureBox.ClearImage();

            if (ThingData.VerifyFile(imageFile) && imageFile.Content != null)
            {
                using var ms = new MemoryStream(imageFile.Content);
                pictureBox.Image = Image.FromStream(ms);
            }
            else
            {
                ArgumentNullException.ThrowIfNull(pictureBox.ErrorImage);
                pictureBox.Image = (Image)pictureBox.ErrorImage.Clone();
            }
        }

        private void pictureBox_MouseClick(object sender, MouseEventArgs e)
        {
            if (ClientSize.Width / 2 < e.X)
            {
                Index++;
            }
            else
            {
                Index--;
            }
        }
        private void textBoxIndex_Leave(object sender, EventArgs e)
        {
            textBoxIndex.Text = $"{Index + 1}/{Images.Count}";
        }
        private void textBoxIndex_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (int.TryParse(textBoxIndex.Text.Split('/')[0], out int i))
                {
                    Index = i - 1;
                }
                else
                {
                    textBoxIndex.Text = $"{Index + 1}/{Images.Count}";
                }

                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }
        private void ImageViewForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space)
                Index++;
            if (e.KeyCode == Keys.Back)
                Index--;
            if (e.KeyCode == Keys.Escape)
            {
                pictureBox.ClearImage();
                this.Close();
            }
        }
    }
}
