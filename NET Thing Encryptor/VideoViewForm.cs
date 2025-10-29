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
using LibVLCSharp.Shared;

namespace NET_Thing_Encryptor
{
    public partial class VideoViewForm : Form
    {
        private LibVLC _libVLC;
        private MediaPlayer _mediaPlayer;

        private ThingFile? file;
        private MemoryStream videoStream;
        private Media? media = null;
        public VideoViewForm(ThingFile file)
        {
            InitializeComponent();
            this.file = file;

            Core.Initialize();
            _libVLC = new LibVLC();
            _mediaPlayer = new MediaPlayer(_libVLC);
            videoView.MediaPlayer = _mediaPlayer;
        }
        private void VideoViewForm_Load(object sender, EventArgs e)
        {
            Debug.WriteLine($"Opening VideoViewForm for file {file.Name} (ID {file.ID}) with ParentID {file.ParentID}");
            if (file.Type != FileType.video && file.Content != null)
            {
                MessageBox.Show("The provided file is not a video.", "Aborting", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
            }

            videoStream = new MemoryStream(file.Content);
            media = new Media(_libVLC, new StreamMediaInput(videoStream));

            _mediaPlayer.Play(media); // Good enough rn
        }
        private void VideoViewForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _mediaPlayer?.Stop();
            _mediaPlayer?.Dispose();
            _libVLC?.Dispose();
            videoStream.Dispose();
        }
    }
}
