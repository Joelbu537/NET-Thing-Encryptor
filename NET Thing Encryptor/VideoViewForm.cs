using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using LibVLCSharp.Shared;
using Timer = System.Windows.Forms.Timer;

namespace NET_Thing_Encryptor
{
    public partial class VideoViewForm : Form
    {
        private readonly LibVLC _libVlc;
        private readonly MediaPlayer _mediaPlayer;
        private readonly MemoryStream _videoStream;
        private readonly Media _media;
        private readonly Timer _positionUpdateTimer;

        private bool _isUserDragging;
        private bool _wasPlayingBeforeDrag;

        public VideoViewForm(ThingFile file)
        {
            InitializeComponent();

            if (file.Type != FileType.video || file.Content == null)
            {
                MessageBox.Show(
                    "The provided file is not a video.",
                    "Aborting",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                throw new ArgumentException("file must be a video with content", nameof(file));
            }

            Core.Initialize();

            // input-repeat kann man drinlassen, aber für StreamMediaInput
            // setzen wir unten zusätzlich einen manuellen Restart um.
            _libVlc = new LibVLC();
            _mediaPlayer = new MediaPlayer(_libVlc)
            {
                EnableHardwareDecoding = true
            };

            videoView.MediaPlayer = _mediaPlayer;

            Debug.WriteLine($"Opening VideoViewForm for file {file.Name} (ID {file.ID}) with ParentID {file.ParentID}");

            _videoStream = new MemoryStream(file.Content);
            _media = new Media(_libVlc, new StreamMediaInput(_videoStream));

            _positionUpdateTimer = new Timer
            {
                Interval = 100
            };
            _positionUpdateTimer.Tick += PositionUpdateTimer_Tick;

            _mediaPlayer.EndReached += MediaPlayer_EndReached;
            _mediaPlayer.EncounteredError += MediaPlayer_EncounteredError;
        }

        private void VideoViewForm_Load(object sender, EventArgs e)
        {
            trackBar.Minimum = 0;
            trackBar.Maximum = 1000;
            trackBar.Value = 0;

            _mediaPlayer.Media = _media;
            _mediaPlayer.Play();

            _positionUpdateTimer.Start();
            UpdatePlayPauseButton();
        }

        private void PositionUpdateTimer_Tick(object? sender, EventArgs e)
        {
            if (_isUserDragging)
                return;

            if (_mediaPlayer.Length <= 0)
                return;

            int value = (int)(_mediaPlayer.Position * trackBar.Maximum);

            if (value < trackBar.Minimum)
                value = trackBar.Minimum;
            else if (value > trackBar.Maximum)
                value = trackBar.Maximum;

            trackBar.Value = value;
        }

        private void trackBar_MouseDown(object sender, MouseEventArgs e)
        {
            _isUserDragging = true;
            _wasPlayingBeforeDrag = _mediaPlayer.IsPlaying;

            if (_wasPlayingBeforeDrag)
                _mediaPlayer.Pause();
        }

        private async void trackBar_MouseUp(object sender, MouseEventArgs e)
        {
            _isUserDragging = false;
            UpdateVideoPosition();

            if (_wasPlayingBeforeDrag)
            {
                _mediaPlayer.Play();
            }
            else
            {
                _mediaPlayer.Mute = true;
                _mediaPlayer.Play();
                await Task.Delay(50);
                _mediaPlayer.Pause();
                _mediaPlayer.Mute = false;
            }

            UpdatePlayPauseButton();
        }

        private void trackBar_Scroll(object sender, EventArgs e)
        {
            if (_isUserDragging)
            {
                UpdateVideoPosition();
            }
        }

        private void UpdateVideoPosition()
        {
            if (_mediaPlayer.Length <= 0)
                return;

            long newTime = (long)(trackBar.Value / (double)trackBar.Maximum * _mediaPlayer.Length);
            _mediaPlayer.Time = newTime;
        }

        private void buttonPause_Click(object sender, EventArgs e)
        {
            if (_mediaPlayer.IsPlaying)
                _mediaPlayer.Pause();
            else
                _mediaPlayer.Play();

            UpdatePlayPauseButton();
        }

        private void UpdatePlayPauseButton()
        {
            // Wenn Video läuft, soll der Button "Pause" anzeigen.
            buttonPause.BackgroundImage = _mediaPlayer.IsPlaying
                ? Properties.Resources.pause_icon
                : Properties.Resources.play_icon;
        }

        private void MediaPlayer_EndReached(object? sender, EventArgs e)
        {
            // Sicherheitshalber zurück auf den UI-Thread
            if (IsDisposed || Disposing)
                return;

            BeginInvoke(new Action(() =>
            {
                try
                {
                    _positionUpdateTimer.Stop();

                    // Stream für erneute Wiedergabe zurücksetzen
                    _videoStream.Position = 0;

                    // Wiedergabe mit demselben Media-Objekt neu starten
                    _mediaPlayer.Stop();
                    _mediaPlayer.Play(_media);

                    trackBar.Value = 0;
                    UpdatePlayPauseButton();

                    _positionUpdateTimer.Start();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error while restarting video: " + ex);
                }
            }));
        }

        private void MediaPlayer_EncounteredError(object? sender, EventArgs e)
        {
            if (IsDisposed || Disposing)
                return;

            BeginInvoke(new Action(() =>
            {
                _positionUpdateTimer.Stop();
                UpdatePlayPauseButton();
                MessageBox.Show(
                    "An error occurred while playing the video.",
                    "Playback error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }));
        }

        private void VideoViewForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _positionUpdateTimer.Stop();
            _positionUpdateTimer.Tick -= PositionUpdateTimer_Tick;

            _mediaPlayer.EndReached -= MediaPlayer_EndReached;
            _mediaPlayer.EncounteredError -= MediaPlayer_EncounteredError;

            _mediaPlayer.Stop();

            _media.Dispose();
            _mediaPlayer.Dispose();
            _libVlc.Dispose();
            _videoStream.Dispose();
            _positionUpdateTimer.Dispose();
        }
    }
}