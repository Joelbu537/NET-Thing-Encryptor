using System.Diagnostics;
using LibVLCSharp.Shared;
using Timer = System.Windows.Forms.Timer;

namespace NET_Thing_Encryptor
{
    public partial class VideoViewForm : Form
    {
        private const int PositionUpdateIntervalMilliseconds = 100;
        private const int KeyboardSeekSeconds = 5;

        private readonly LibVLC _libVlc;
        private readonly MediaPlayer _mediaPlayer;
        private MemoryStream? _mediaStream;
        private readonly Media _media;
        private readonly Timer _positionUpdateTimer;
        private readonly bool _isAudio;
        private readonly string _mediaKind;

        private long _durationMilliseconds;
        private bool _isSeeking;
        private bool _resumeAfterSeek;
        private bool _restartInProgress;
        private bool _isClosing;

        public VideoViewForm(ThingFile file)
        {
            ArgumentNullException.ThrowIfNull(file);
            if (file.Type is not FileType.video and not FileType.audio ||
                file.Content is not { Length: > 0 })
            {
                throw new ArgumentException(
                    "The file must contain audio or video data.",
                    nameof(file));
            }

            _isAudio = file.Type == FileType.audio;
            _mediaKind = _isAudio ? "audio" : "video";
            InitializeComponent();
            KeyPreview = true;
            ConfigureMediaMode(file);

            toolTip.SetToolTip(buttonPlayPause, "Play / Pause (Space)");
            toolTip.SetToolTip(buttonSeekBackward, "Back 10 seconds");
            toolTip.SetToolTip(buttonSeekForward, "Forward 10 seconds");
            toolTip.SetToolTip(timeline, "Click or drag to seek");

            Core.Initialize();
            _libVlc = new LibVLC("--no-video-title-show", "--quiet");
            _mediaPlayer = new MediaPlayer(_libVlc)
            {
                EnableHardwareDecoding = !_isAudio
            };
            videoView.MediaPlayer = _mediaPlayer;

            Debug.WriteLine(
                $"Opening media player for {_mediaKind} file {file.Name} " +
                $"(ID {file.ID}) with ParentID {file.ParentID}");

            _mediaStream = new MemoryStream(file.Content, writable: false);
            _media = new Media(_libVlc, new StreamMediaInput(_mediaStream));
            file.ReleaseContent();
            if (_isAudio)
                _media.AddOption(":no-video");

            _positionUpdateTimer = new Timer
            {
                Interval = PositionUpdateIntervalMilliseconds
            };
            _positionUpdateTimer.Tick += PositionUpdateTimer_Tick;

            timeline.SeekStarted += Timeline_SeekStarted;
            timeline.SeekPreview += Timeline_SeekPreview;
            timeline.SeekCompleted += Timeline_SeekCompleted;

            _mediaPlayer.Playing += MediaPlayer_Playing;
            _mediaPlayer.Paused += MediaPlayer_Paused;
            _mediaPlayer.Stopped += MediaPlayer_Stopped;
            _mediaPlayer.LengthChanged += MediaPlayer_LengthChanged;
            _mediaPlayer.EndReached += MediaPlayer_EndReached;
            _mediaPlayer.EncounteredError += MediaPlayer_EncounteredError;
        }

        private void ConfigureMediaMode(ThingFile file)
        {
            string displayName = GetDisplayName(file);
            Text = _isAudio
                ? $".NET Thing Audio Player — {displayName}"
                : $".NET Thing Video Viewer — {displayName}";

            videoView.Visible = !_isAudio;
            audioDisplay.Visible = _isAudio;
            audioDisplay.FileName = displayName;
            audioDisplay.Format = file.Extension;

            if (_isAudio)
            {
                audioDisplay.BringToFront();
                WindowState = FormWindowState.Normal;
                StartPosition = FormStartPosition.CenterScreen;
                ClientSize = new Size(900, 600);
            }
            else
            {
                videoView.BringToFront();
            }
        }

        private static string GetDisplayName(ThingFile file)
        {
            if (string.IsNullOrWhiteSpace(file.Extension) ||
                file.Name.EndsWith($".{file.Extension}", StringComparison.OrdinalIgnoreCase))
            {
                return file.Name;
            }

            return $"{file.Name}.{file.Extension}";
        }

        private void VideoViewForm_Load(object sender, EventArgs e)
        {
            timeline.Value = 0d;
            UpdateTimeDisplay(0, 0);
            UpdatePlayPauseButton(isPlaying: false);

            _mediaPlayer.Media = _media;
            if (!_mediaPlayer.Play())
            {
                MessageBox.Show(
                    $"The {_mediaKind} file could not be started.",
                    "Playback error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            _positionUpdateTimer.Start();
        }

        private void PositionUpdateTimer_Tick(object? sender, EventArgs e)
        {
            if (_isClosing)
                return;

            long duration = GetDurationMilliseconds();
            if (duration <= 0)
            {
                UpdateTimeDisplay(0, 0);
                return;
            }

            if (_isSeeking)
                return;

            long currentTime = Math.Clamp(_mediaPlayer.Time, 0, duration);
            timeline.Value = currentTime / (double)duration;
            UpdateTimeDisplay(currentTime, duration);
        }

        private void Timeline_SeekStarted(object? sender, EventArgs e)
        {
            _isSeeking = true;
            _resumeAfterSeek = _mediaPlayer.IsPlaying;
            if (_resumeAfterSeek)
            {
                _mediaPlayer.SetPause(true);
                UpdatePlayPauseButton(isPlaying: false);
            }
        }

        private void Timeline_SeekPreview(object? sender, TimelineSeekEventArgs e)
        {
            long duration = GetDurationMilliseconds();
            if (duration <= 0)
                return;

            long previewTime = PositionToTime(e.Position, duration);
            UpdateTimeDisplay(previewTime, duration);
        }

        private void Timeline_SeekCompleted(object? sender, TimelineSeekEventArgs e)
        {
            try
            {
                long duration = GetDurationMilliseconds();
                if (duration <= 0)
                    return;

                long targetTime = PositionToTime(e.Position, duration);
                _mediaPlayer.Time = targetTime;
                timeline.Value = targetTime / (double)duration;
                UpdateTimeDisplay(targetTime, duration);

                if (_resumeAfterSeek)
                {
                    _mediaPlayer.SetPause(false);
                    UpdatePlayPauseButton(isPlaying: true);
                }
            }
            finally
            {
                _isSeeking = false;
                _resumeAfterSeek = false;
            }
        }

        private void buttonPlayPause_Click(object sender, EventArgs e)
        {
            TogglePlayback();
        }

        private void buttonSeekBackward_Click(object sender, EventArgs e)
        {
            SeekBy(TimeSpan.FromSeconds(-10));
        }

        private void buttonSeekForward_Click(object sender, EventArgs e)
        {
            SeekBy(TimeSpan.FromSeconds(10));
        }

        private void TogglePlayback()
        {
            if (_mediaPlayer.IsPlaying)
            {
                _mediaPlayer.SetPause(true);
                UpdatePlayPauseButton(isPlaying: false);
                return;
            }

            if (_mediaPlayer.State == VLCState.Paused)
            {
                _mediaPlayer.SetPause(false);
                UpdatePlayPauseButton(isPlaying: true);
                return;
            }

            long duration = GetDurationMilliseconds();
            if (duration > 0 && _mediaPlayer.Time >= duration - 250)
            {
                RewindMediaStream();
                timeline.Value = 0d;
                UpdateTimeDisplay(0, duration);
            }

            _mediaPlayer.Play(_media);
            UpdatePlayPauseButton(isPlaying: true);
            _positionUpdateTimer.Start();
        }

        private void SeekBy(TimeSpan offset)
        {
            long duration = GetDurationMilliseconds();
            if (duration <= 0)
                return;

            long targetTime = Math.Clamp(
                _mediaPlayer.Time + (long)offset.TotalMilliseconds,
                0,
                duration);
            _mediaPlayer.Time = targetTime;
            timeline.Value = targetTime / (double)duration;
            UpdateTimeDisplay(targetTime, duration);
        }

        private void RewindMediaStream()
        {
            MemoryStream stream = _mediaStream
                ?? throw new ObjectDisposedException(nameof(VideoViewForm));
            stream.Position = 0;
        }

        private void UpdatePlayPauseButton(bool isPlaying)
        {
            // The legacy resource files are named the wrong way around:
            // play_icon contains pause bars, pause_icon contains the play triangle.
            buttonPlayPause.BackgroundImage = isPlaying
                ? Properties.Resources.play_icon
                : Properties.Resources.pause_icon;
            buttonPlayPause.AccessibleName = isPlaying ? "Pause" : "Play";
            audioDisplay.IsPlaying = isPlaying;
            buttonPlayPause.Invalidate();
        }

        private void UpdateTimeDisplay(long currentMilliseconds, long durationMilliseconds)
        {
            labelCurrentTime.Text = FormatTime(currentMilliseconds, durationMilliseconds);
            labelDuration.Text = FormatTime(durationMilliseconds, durationMilliseconds);
        }

        private long GetDurationMilliseconds()
        {
            long playerDuration = _mediaPlayer.Length;
            if (playerDuration > 0)
                _durationMilliseconds = playerDuration;
            return _durationMilliseconds;
        }

        private static long PositionToTime(double position, long durationMilliseconds)
        {
            return Math.Clamp(
                (long)Math.Round(Math.Clamp(position, 0d, 1d) * durationMilliseconds),
                0,
                durationMilliseconds);
        }

        private static string FormatTime(long milliseconds, long durationMilliseconds)
        {
            TimeSpan value = TimeSpan.FromMilliseconds(Math.Max(0, milliseconds));
            bool showHours = durationMilliseconds >= 3_600_000 || value.TotalHours >= 1d;
            return showHours
                ? $"{(int)value.TotalHours}:{value.Minutes:00}:{value.Seconds:00}"
                : $"{(int)value.TotalMinutes:00}:{value.Seconds:00}";
        }

        private void MediaPlayer_Playing(object? sender, EventArgs e)
        {
            PostToUi(() =>
            {
                UpdatePlayPauseButton(isPlaying: true);
                _positionUpdateTimer.Start();
            });
        }

        private void MediaPlayer_Paused(object? sender, EventArgs e)
        {
            PostToUi(() => UpdatePlayPauseButton(isPlaying: false));
        }

        private void MediaPlayer_Stopped(object? sender, EventArgs e)
        {
            PostToUi(() => UpdatePlayPauseButton(isPlaying: false));
        }

        private void MediaPlayer_LengthChanged(object? sender, MediaPlayerLengthChangedEventArgs e)
        {
            if (e.Length <= 0)
                return;

            Interlocked.Exchange(ref _durationMilliseconds, e.Length);
            PostToUi(() => UpdateTimeDisplay(Math.Max(0, _mediaPlayer.Time), e.Length));
        }

        private void MediaPlayer_EndReached(object? sender, EventArgs e)
        {
            PostToUi(RestartAfterEndAsync);
        }

        private async void RestartAfterEndAsync()
        {
            if (_isClosing || _restartInProgress)
                return;

            _restartInProgress = true;
            _positionUpdateTimer.Stop();
            UpdatePlayPauseButton(isPlaying: false);
            timeline.Value = 0d;
            UpdateTimeDisplay(0, GetDurationMilliseconds());

            try
            {
                // EndReached is raised from a VLC callback. Waiting until that callback has
                // returned avoids stopping/restarting the player from inside VLC itself.
                await Task.Delay(75);
                if (_isClosing)
                    return;

                RewindMediaStream();
                _mediaPlayer.Stop();
                if (!_mediaPlayer.Play(_media))
                    throw new InvalidOperationException(
                        $"VLC could not restart the {_mediaKind} file.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error while restarting {_mediaKind} playback: {ex}");
                if (!_isClosing)
                {
                    MessageBox.Show(
                        $"The {_mediaKind} file could not be restarted.",
                        "Playback error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            finally
            {
                _restartInProgress = false;
            }
        }

        private void MediaPlayer_EncounteredError(object? sender, EventArgs e)
        {
            PostToUi(() =>
            {
                _positionUpdateTimer.Stop();
                UpdatePlayPauseButton(isPlaying: false);
                MessageBox.Show(
                    $"An error occurred while playing the {_mediaKind} file.",
                    "Playback error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            });
        }

        private void VideoViewForm_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Space:
                    TogglePlayback();
                    break;
                case Keys.Left:
                case Keys.J:
                    SeekBy(TimeSpan.FromSeconds(-KeyboardSeekSeconds));
                    break;
                case Keys.Right:
                case Keys.L:
                    SeekBy(TimeSpan.FromSeconds(KeyboardSeekSeconds));
                    break;
                case Keys.Escape:
                    Close();
                    break;
                default:
                    return;
            }

            e.Handled = true;
            e.SuppressKeyPress = true;
        }

        private void PostToUi(Action action)
        {
            if (_isClosing || IsDisposed || Disposing || !IsHandleCreated)
                return;

            try
            {
                BeginInvoke(new Action(() =>
                {
                    if (!_isClosing && !IsDisposed)
                        action();
                }));
            }
            catch (InvalidOperationException)
            {
                // The window was closed between the handle check and BeginInvoke.
            }
        }

        private void VideoViewForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_isClosing)
                return;

            _isClosing = true;
            _positionUpdateTimer.Stop();
            _positionUpdateTimer.Tick -= PositionUpdateTimer_Tick;

            timeline.SeekStarted -= Timeline_SeekStarted;
            timeline.SeekPreview -= Timeline_SeekPreview;
            timeline.SeekCompleted -= Timeline_SeekCompleted;

            _mediaPlayer.Playing -= MediaPlayer_Playing;
            _mediaPlayer.Paused -= MediaPlayer_Paused;
            _mediaPlayer.Stopped -= MediaPlayer_Stopped;
            _mediaPlayer.LengthChanged -= MediaPlayer_LengthChanged;
            _mediaPlayer.EndReached -= MediaPlayer_EndReached;
            _mediaPlayer.EncounteredError -= MediaPlayer_EncounteredError;

            long releasedBytes = _mediaStream?.Length ?? 0;
            _mediaPlayer.Stop();
            videoView.MediaPlayer = null;
            _mediaPlayer.Dispose();
            _media.Dispose();
            _libVlc.Dispose();
            _mediaStream?.Dispose();
            _mediaStream = null;
            _positionUpdateTimer.Dispose();
            MemoryMaintenance.NotifyLargeBufferReleased(releasedBytes);
        }
    }
}
