namespace NET_Thing_Encryptor;

internal static class WinFormsImageExtensions
{
    public static void ClearImage(this PictureBox pictureBox)
    {
        Image? image = pictureBox.Image;
        pictureBox.Image = null;
        image?.Dispose();
    }
}
