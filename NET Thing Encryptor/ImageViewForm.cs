using ImageMagick;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Runtime;

namespace NET_Thing_Encryptor
{
    public partial class ImageViewForm : Form
    {
        private List<ThingObjectLink>? Images = new();

        private Task<Bitmap?>? nextBitmap;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Index { get; set; }
        public ImageViewForm(ThingFile file)
        {
            this.KeyPreview = true;

            InitializeComponent();

            _ = InitAsync(file);
        }
        private async Task InitAsync(ThingFile file)
        {
            Debug.WriteLine($"Opening ImageViewForm for file {file.Name} (ID {file.ID}) with ParentID {file.ParentID}");
            if (file.Type != FileType.image)
            {
                MessageBox.Show("The provided file is not an image.", "Aborting", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
                return;
            }

            List<ThingObjectLink> content = await ThingData.LoadFolderContent(file.ParentID);
            Images = content.Where((x) => x.Type == FileType.image).ToList();
            Debug.WriteLine($"Found {Images.Count} images!");
            int selectedIndex = Images.IndexOf(Images.FirstOrDefault((x) => x.ID == file.ID));


            // Load given image
            nextBitmap = LoadBitmap(selectedIndex);
            textBoxIndex.Text = $"{selectedIndex + 1}/{Images.Count}";
            Index = selectedIndex;

            pictureBox.SizeMode = PictureBoxSizeMode.CenterImage;
            pictureBox.Image = (Image)pictureBox.InitialImage.Clone();
            Bitmap? result = await nextBitmap;
            pictureBox.ClearImage();
            pictureBox.Image = result;
            pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            nextBitmap = null;

            // Preload next
            if (selectedIndex + 1 < Images.Count)
            {
                nextBitmap = LoadBitmap(selectedIndex + 1);
            }
        }
        private async Task SwitchImage(int index)
        {
            if (index >= Images.Count || index < 0 || index == Index) { return; }
            if (Images.Count == 0) { return; }

            Debug.WriteLine("Image is being refreshed");

            pictureBox.ClearImage();
            textBoxIndex.Text = $"{index + 1}/{Images.Count}";

            if (index == Index + 1 && nextBitmap != null)
            {
                Debug.WriteLine("Image is next in queue");
                if (!nextBitmap.IsCompleted)
                {
                    Debug.WriteLine("Image has not loaded yet! Loading...");
                    pictureBox.SizeMode = PictureBoxSizeMode.CenterImage;
                    pictureBox.Image = (Image)pictureBox.InitialImage.Clone();
                    Bitmap? b = await nextBitmap;

                    Debug.WriteLine("Image has loaded.");
                    pictureBox.ClearImage();
                    pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
                    pictureBox.Image = b;
                }
                else
                {
                    Debug.WriteLine("Image has already loaded!");
                    Bitmap? b = await nextBitmap;
                    pictureBox.ClearImage();
                    pictureBox.Image = b;
                }
            }
            else
            {
                Debug.WriteLine("Image is not next in queue. Loading...");
                pictureBox.SizeMode = PictureBoxSizeMode.CenterImage;
                pictureBox.Image = (Image)pictureBox.InitialImage.Clone();
                Bitmap? b = await LoadBitmap(index);

                Debug.WriteLine("Image has loaded.");
                pictureBox.ClearImage();
                pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
                pictureBox.Image = b;
            }

            Index = index;
            textBoxIndex.Text = $"{Index + 1}/{Images.Count}";

            nextBitmap = null;
            nextBitmap = LoadBitmap(index + 1);
            Debug.WriteLine("Image refreshed!");
        }
        private async Task<Bitmap?> LoadBitmap(int index)
        {
            try
            {
                ThingFile? imageFile = await ThingData.LoadFileAsync<ThingFile>(Images[index].ID);
                ArgumentNullException.ThrowIfNull(imageFile);

                if (!ThingData.VerifyFile(imageFile) || imageFile.Content == null)
                {
                    throw new FileFormatException("Provided file was not an image or empty!");
                }

                var imageData = imageFile.Content;

                imageFile.Clear();

                return await Task.Run(() =>
                {
                    using var img = new MagickImage(imageData);
                    if (img.ColorSpace != ColorSpace.sRGB)
                        img.TransformColorSpace(ColorProfile.SRGB);

                    using var ms = new MemoryStream();
                    img.Write(ms, MagickFormat.Png32);
                    ms.Position = 0;

                    using var tempBitmap = new Bitmap(ms);
                    return (Bitmap)tempBitmap.Clone();
                }).ConfigureAwait(false);
            }
            catch (Exception)
            {
                ArgumentNullException.ThrowIfNull(pictureBox.ErrorImage);
                return (Bitmap)pictureBox.ErrorImage.Clone();
            }
        }
        private async void pictureBox_MouseClick(object sender, MouseEventArgs e)
        {
            if (ClientSize.Width / 2 < e.X)
            {
                await SwitchImage(Index + 1);
            }
            else
            {
                await SwitchImage(Index - 1);
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
        private async void ImageViewForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.D)
                await SwitchImage(Index + 1);
            else if (e.KeyCode == Keys.Back || e.KeyCode == Keys.A)
                await SwitchImage(Index - 1);
            else if (e.KeyCode == Keys.Escape)
                this.Close();
        } 
        private void ImageViewForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            pictureBox.ClearImage();

            Images?.Clear();
            Images = null;

            GC.Collect(2, GCCollectionMode.Aggressive, true, true);
        }

        private void textBoxIndex_Enter(object sender, EventArgs e)
        {
            textBoxIndex.Text = String.Empty;
        }
    }
}