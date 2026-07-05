using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace NET_Thing_Encryptor
{
    public sealed class TimelineSeekEventArgs(double position) : EventArgs
    {
        public double Position { get; } = Math.Clamp(position, 0d, 1d);
    }

    public sealed class VideoTimeline : Control
    {
        private const int HorizontalPadding = 12;
        private const int TrackHeight = 6;
        private const int ThumbRadius = 8;

        private double _value;
        private bool _isDragging;
        private bool _isHovered;

        public event EventHandler? SeekStarted;
        public event EventHandler<TimelineSeekEventArgs>? SeekPreview;
        public event EventHandler<TimelineSeekEventArgs>? SeekCompleted;

        public VideoTimeline()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.UserPaint |
                ControlStyles.Selectable,
                true);

            BackColor = Color.FromArgb(24, 24, 24);
            Cursor = Cursors.Hand;
            MinimumSize = new Size(120, 28);
            TabStop = true;
        }

        [DefaultValue(0d)]
        public double Value
        {
            get => _value;
            set
            {
                double clamped = Math.Clamp(value, 0d, 1d);
                if (Math.Abs(_value - clamped) < 0.000001d)
                    return;

                _value = clamped;
                Invalidate();
            }
        }

        [Browsable(false)]
        public bool IsDragging => _isDragging;

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle track = GetTrackRectangle();
            int progressWidth = (int)Math.Round(track.Width * Value);
            var progress = new Rectangle(track.X, track.Y, progressWidth, track.Height);

            using var trackBrush = new SolidBrush(Color.FromArgb(76, 76, 76));
            using var progressBrush = new SolidBrush(Color.FromArgb(229, 72, 72));
            using GraphicsPath trackPath = CreateRoundedRectangle(track, TrackHeight / 2f);
            e.Graphics.FillPath(trackBrush, trackPath);

            if (progress.Width > 0)
            {
                using GraphicsPath progressPath = CreateRoundedRectangle(progress, TrackHeight / 2f);
                e.Graphics.FillPath(progressBrush, progressPath);
            }

            int thumbX = track.X + progressWidth;
            int radius = _isDragging || _isHovered || Focused ? ThumbRadius + 1 : ThumbRadius;
            var thumb = new Rectangle(
                thumbX - radius,
                track.Y + track.Height / 2 - radius,
                radius * 2,
                radius * 2);
            using var thumbBrush = new SolidBrush(Color.White);
            e.Graphics.FillEllipse(thumbBrush, thumb);

            if (Focused && ShowFocusCues)
            {
                using var focusPen = new Pen(Color.FromArgb(150, 255, 255, 255));
                focusPen.DashStyle = DashStyle.Dot;
                e.Graphics.DrawRectangle(focusPen, ClientRectangle.X, ClientRectangle.Y,
                    ClientRectangle.Width - 1, ClientRectangle.Height - 1);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left)
                return;

            Focus();
            _isDragging = true;
            Capture = true;
            SeekStarted?.Invoke(this, EventArgs.Empty);
            UpdateDragPosition(e.X);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            _isHovered = true;
            if (_isDragging)
                UpdateDragPosition(e.X);
            else
                Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button != MouseButtons.Left || !_isDragging)
                return;

            UpdateDragPosition(e.X);
            _isDragging = false;
            Capture = false;
            SeekCompleted?.Invoke(this, new TimelineSeekEventArgs(Value));
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _isHovered = false;
            if (!_isDragging)
                Invalidate();
        }

        protected override void OnMouseCaptureChanged(EventArgs e)
        {
            base.OnMouseCaptureChanged(e);
            if (!_isDragging)
                return;

            _isDragging = false;
            SeekCompleted?.Invoke(this, new TimelineSeekEventArgs(Value));
            Invalidate();
        }

        protected override bool IsInputKey(Keys keyData)
        {
            Keys key = keyData & Keys.KeyCode;
            return key is Keys.Left or Keys.Right or Keys.Home or Keys.End ||
                   base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            double? newValue = e.KeyCode switch
            {
                Keys.Left => Value - (e.Shift ? 0.05d : 0.01d),
                Keys.Right => Value + (e.Shift ? 0.05d : 0.01d),
                Keys.Home => 0d,
                Keys.End => 1d,
                _ => null
            };

            if (newValue is null)
            {
                base.OnKeyDown(e);
                return;
            }

            SeekStarted?.Invoke(this, EventArgs.Empty);
            Value = newValue.Value;
            SeekPreview?.Invoke(this, new TimelineSeekEventArgs(Value));
            SeekCompleted?.Invoke(this, new TimelineSeekEventArgs(Value));
            e.Handled = true;
            e.SuppressKeyPress = true;
        }

        private void UpdateDragPosition(int mouseX)
        {
            Rectangle track = GetTrackRectangle();
            Value = track.Width <= 0
                ? 0d
                : (mouseX - track.Left) / (double)track.Width;
            SeekPreview?.Invoke(this, new TimelineSeekEventArgs(Value));
        }

        private Rectangle GetTrackRectangle()
        {
            int width = Math.Max(1, ClientSize.Width - HorizontalPadding * 2);
            return new Rectangle(
                HorizontalPadding,
                (ClientSize.Height - TrackHeight) / 2,
                width,
                TrackHeight);
        }

        private static GraphicsPath CreateRoundedRectangle(Rectangle rectangle, float radius)
        {
            var path = new GraphicsPath();
            if (rectangle.Width <= 0 || rectangle.Height <= 0)
                return path;

            float diameter = Math.Min(radius * 2, Math.Min(rectangle.Width, rectangle.Height));
            if (diameter <= 1f)
            {
                path.AddRectangle(rectangle);
                return path;
            }

            var arc = new RectangleF(rectangle.X, rectangle.Y, diameter, diameter);
            path.AddArc(arc, 180, 90);
            arc.X = rectangle.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = rectangle.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = rectangle.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
