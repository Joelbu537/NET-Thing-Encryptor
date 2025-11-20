using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using LibVLCSharp.Shared;
using Timer = System.Windows.Forms.Timer;

namespace NET_Thing_Encryptor
{
    public partial class VideoViewForm : Form
    {
        private readonly LibVLC _libVlc;
        private readonly MediaPlayer _mediaPlayer;
        private readonly MemoryStream? _videoStream;
        private readonly Media _media;
        private readonly Timer _positionUpdateTimer = new();
        private bool _isUserDragging = false;
        public VideoViewForm(ThingFile file)
        {
            InitializeComponent();

            Core.Initialize();
            _libVlc = new LibVLC();
            _mediaPlayer = new MediaPlayer(_libVlc);
            videoView.MediaPlayer = _mediaPlayer;

            Debug.WriteLine($"Opening VideoViewForm for file {file.Name} (ID {file.ID}) with ParentID {file.ParentID}");
            if (file.Type != FileType.video || file.Content == null)
            {
                MessageBox.Show("The provided file is not a video.", "Aborting", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
                return;
            }

            _positionUpdateTimer.Interval = 100; // Alle 0.1
            _positionUpdateTimer.Tick += PositionUpdateTimer_Tick;

            _videoStream = new MemoryStream(file.Content);
            _media = new Media(_libVlc, new StreamMediaInput(_videoStream));
        }
        private void VideoViewForm_Load(object sender, EventArgs e)
        {
            trackBar.Value = 0;
            _mediaPlayer.Play(_media); // Good enough rn
            _positionUpdateTimer.Start();
        }

        private void PositionUpdateTimer_Tick(object sender, EventArgs e)
        {
            if (!_isUserDragging && _mediaPlayer.Length > 0)
            {
                int trackBarValue = (int)(_mediaPlayer.Position * 1000);

                if (trackBarValue >= 0 && trackBarValue <= 1000)
                {
                    trackBar.Value = trackBarValue;
                }
            }
        }
        private void trackBar_MouseDown(object sender, MouseEventArgs e)
        {
            _isUserDragging = true;
            _mediaPlayer.Pause();
        }

        private void trackBar_MouseUp(object sender, MouseEventArgs e)
        {
            _isUserDragging = false;
            UpdateVideoPosition();
            _mediaPlayer.Play();
        }
        private void UpdateVideoPosition()
        {
            if (_mediaPlayer.Length > 0)
            {
                long newTime = (long)(trackBar.Value / 1000.0 * _mediaPlayer.Length);
                _mediaPlayer.Time = newTime;
            }
        }
        private void VideoViewForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _positionUpdateTimer.Stop();
            _positionUpdateTimer.Dispose();
            _mediaPlayer.Stop();
            _mediaPlayer.Dispose();
            _libVlc?.Dispose();
            _videoStream?.Dispose();
        }
    }
}
