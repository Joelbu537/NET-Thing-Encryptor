using ImageMagick;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;

namespace NET_Thing_Encryptor
{
    public partial class ImageViewForm : Form
    {
        private List<ThingObjectLink>? Images = new();
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
                if (_index == Math.Clamp(value, 0, Math.Max(Images.Count - 1, 0))) { return; }
                _index = Math.Clamp(value, 0, Math.Max(Images.Count - 1, 0));
                OnIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public ImageViewForm(ThingFile file)
        {
            OnIndexChanged += RefreshImage;
            this.KeyPreview = true;

            InitializeComponent();

            _ = InitAsync(file);
        }
        private async Task InitAsync(ThingFile file)
        {
            Debug.WriteLine($"Opening ImageViewForm for file {file.Name} (ID {file.ID}) with ParentID {file.ParentID}");
            if (file.Type != FileType.image)
            {
                MessageBox.Show("The provided file is not an image.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            List<ThingObjectLink> content = await ThingData.LoadFolderContent(file.ParentID);
            Images = content.Where((x) => x.Type == FileType.image).ToList();
            Debug.WriteLine($"Found {Images.Count} images!");
            int selectedIndex = Images.IndexOf(Images.FirstOrDefault((x) => x.ID == file.ID));

            Index = selectedIndex;
        }
        private async void RefreshImage(object? o, EventArgs e)
        {
            if (Images.Count == 0) { return; }
            pictureBox.ClearImage();
            ThingFile? imageFile = await ThingData.LoadFileAsync<ThingFile>(Images[Index].ID);
            ArgumentNullException.ThrowIfNull(imageFile);

            try
            {
                if (ThingData.VerifyFile(imageFile) && imageFile.Content != null)
                {
                    var imageData = imageFile.Content;

                    imageFile.Clear();

                    using (var img = new MagickImage(imageData))
                    {
                        if (img.ColorSpace != ColorSpace.sRGB)
                        {
                            img.TransformColorSpace(ColorProfile.SRGB);
                        }

                        using (var ms = new MemoryStream())
                        {
                            img.Write(ms, MagickFormat.Bmp);
                            ms.Position = 0;
                            pictureBox.Image = new Bitmap(ms);
                        }
                    }

                    imageData = null;
                }
                else
                {
                    ArgumentNullException.ThrowIfNull(pictureBox.ErrorImage);
                    pictureBox.Image = (Image)pictureBox.ErrorImage.Clone();
                }
            }
            finally
            {
                imageFile = null;
            }

            textBoxIndex.Text = $"{Index + 1}/{Images.Count}";
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
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.D)
                Index++;
            if (e.KeyCode == Keys.Back || e.KeyCode == Keys.A)
                Index--;
            if (e.KeyCode == Keys.Escape)
                this.Close();
        }
        private void ImageViewForm_Load(object sender, EventArgs e)
        {
            while (Images.Count == 0)
            {
                Application.DoEvents();
                Task.Delay(100).Wait();
            }
            OnIndexChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ImageViewForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            OnIndexChanged -= RefreshImage;
            pictureBox.ClearImage();

            Images?.Clear();
            Images = null;

            GC.Collect();
        }
    }
}