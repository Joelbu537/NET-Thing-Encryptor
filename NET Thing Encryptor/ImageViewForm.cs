using ImageMagick;
using ImageMagick.Drawing;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.Media.Core;

namespace NET_Thing_Encryptor
{
    public partial class ImageViewForm : Form
    {
        private List<ThingObjectLink> Images = new();
        private List<Bitmap?> imageBuffer = new();
        private event EventHandler OnIndexChanged;
        private int _oldIndex = 0;
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
                value = Math.Clamp(value, 0, Math.Max(Images.Count - 1, 0));
                if (_index == value) { return; }
                _oldIndex = _index;
                _index = value;
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
            if (file.Type != FileType.image)
            {
                MessageBox.Show("The provided file is not an image.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            List<ThingObjectLink> content = await ThingData.LoadFolderContent(file.ParentID);
            Images = content.Where(x => x.Type == FileType.image).ToList();

            int selectedIndex = Images.FindIndex(x => x.ID == file.ID);
            if (selectedIndex < 0) selectedIndex = 0;

            ArgumentNullException.ThrowIfNull(ThingData.Root);

            // Start- und Endindex für den Buffer berechnen
            int start = Math.Max(0, selectedIndex - ThingData.Root.BackBuffer);
            int end = Math.Min(Images.Count - 1, selectedIndex + ThingData.Root.ForwardBuffer);

            imageBuffer.Clear();
            for (int i = start; i <= end; i++)
            {
                var bmp = await LoadBitmapAsync(Images[i].ID);
                imageBuffer.Add(bmp);
            }

            // aktuelles Bild liegt jetzt bei:
            _oldIndex = _index = selectedIndex;
            currentBufferIndex = selectedIndex - start;

            pictureBox.Image = imageBuffer[currentBufferIndex] ?? (Image)pictureBox.ErrorImage.Clone();
            textBoxIndex.Text = $"{Index + 1}/{Images.Count}";
        }

        // Hilfsfunktion zum Laden einer Bitmap
        private async Task<Bitmap?> LoadBitmapAsync(ulong id)
        {
            ThingFile? imageFile = await ThingData.LoadFileAsync<ThingFile>(id);
            if (imageFile != null && ThingData.VerifyFile(imageFile) && imageFile.Content != null)
            {
                using var img = new MagickImage(imageFile.Content);
                img.ColorSpace = ColorSpace.RGB;
                using var ms = new MemoryStream();
                img.Write(ms, MagickFormat.Bmp);
                ms.Position = 0;
                return new Bitmap(ms);
            }
            return (Bitmap?)pictureBox.ErrorImage.Clone();
        }
        private int currentBufferIndex = 0;

        private void RefreshImage(object? o, EventArgs e)
        {
            if (Images.Count == 0) return;

            pictureBox.ClearImage();

            if (_oldIndex < Index)
            {
                // Vorwärts
                imageBuffer[0]?.Dispose();
                imageBuffer.RemoveAt(0);

                // Platzhalter hinten einfügen
                imageBuffer.Add(null);

                currentBufferIndex = Math.Min(currentBufferIndex, imageBuffer.Count - 1);

                // Bild sofort anzeigen
                pictureBox.Image = imageBuffer[currentBufferIndex] ?? (Image)pictureBox.ErrorImage.Clone();

                // Asynchron nachladen
                _ = Task.Run(async () =>
                {
                    int target = Index + ThingData.Root.ForwardBuffer;
                    var bmp = (target >= 0 && target < Images.Count) ? await LoadBitmapAsync(Images[target].ID) : null;
                    this.Invoke(() => imageBuffer[^1] = bmp);
                });
            }
            else
            {
                // Rückwärts
                imageBuffer[^1]?.Dispose();
                imageBuffer.RemoveAt(imageBuffer.Count - 1);

                // Platzhalter vorne einfügen
                imageBuffer.Insert(0, null);

                currentBufferIndex = Math.Max(0, currentBufferIndex);

                // Bild sofort anzeigen
                pictureBox.Image = imageBuffer[currentBufferIndex] ?? (Image)pictureBox.ErrorImage.Clone();

                // Asynchron nachladen
                _ = Task.Run(async () =>
                {
                    int target = Index - ThingData.Root.BackBuffer;
                    var bmp = (target >= 0 && target < Images.Count) ? await LoadBitmapAsync(Images[target].ID) : null;
                    this.Invoke(() => imageBuffer[0] = bmp);
                });
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
    }
}