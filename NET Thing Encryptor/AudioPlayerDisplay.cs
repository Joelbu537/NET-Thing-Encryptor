using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace NET_Thing_Encryptor
{
    internal sealed class AudioPlayerDisplay : Control
    {
        private const int ArtworkSize = 220;

        private string _fileName = string.Empty;
        private string _format = string.Empty;
        private bool _isPlaying;

        public AudioPlayerDisplay()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.UserPaint,
                true);

            BackColor = Color.FromArgb(18, 18, 22);
            ForeColor = Color.White;
            AccessibleRole = AccessibleRole.Graphic;
        }

        [DefaultValue("")]
        public string FileName
        {
            get => _fileName;
            set
            {
                _fileName = value ?? string.Empty;
                AccessibleName = $"Audio player for {_fileName}";
                Invalidate();
            }
        }

        [DefaultValue("")]
        public string Format
        {
            get => _format;
            set
            {
                _format = value ?? string.Empty;
                Invalidate();
            }
        }

        [DefaultValue(false)]
        public bool IsPlaying
        {
            get => _isPlaying;
            set
            {
                if (_isPlaying == value)
                    return;

                _isPlaying = value;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint =
                System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            using LinearGradientBrush background = new(
                ClientRectangle,
                Color.FromArgb(20, 20, 25),
                Color.FromArgb(31, 35, 49),
                LinearGradientMode.Vertical);
            e.Graphics.FillRectangle(background, ClientRectangle);

            int availableArtworkSize = Math.Min(
                ArtworkSize,
                Math.Max(120, Math.Min(ClientSize.Width - 80, ClientSize.Height - 190)));
            int artworkX = (ClientSize.Width - availableArtworkSize) / 2;
            int artworkY = Math.Max(36, (ClientSize.Height - availableArtworkSize - 105) / 2);
            Rectangle artwork = new(
                artworkX,
                artworkY,
                availableArtworkSize,
                availableArtworkSize);

            DrawArtwork(e.Graphics, artwork);
            DrawMetadata(e.Graphics, artwork);
        }

        private void DrawArtwork(Graphics graphics, Rectangle artwork)
        {
            Rectangle shadow = artwork;
            shadow.Offset(0, 8);

            using GraphicsPath shadowPath = CreateRoundedRectangle(shadow, 24);
            using SolidBrush shadowBrush = new(Color.FromArgb(70, 0, 0, 0));
            graphics.FillPath(shadowBrush, shadowPath);

            using GraphicsPath artworkPath = CreateRoundedRectangle(artwork, 24);
            using LinearGradientBrush artworkBrush = new(
                artwork,
                Color.FromArgb(55, 72, 118),
                Color.FromArgb(74, 56, 104),
                LinearGradientMode.ForwardDiagonal);
            graphics.FillPath(artworkBrush, artworkPath);

            int circleSize = Math.Max(74, artwork.Width / 2);
            Rectangle circle = new(
                artwork.X + (artwork.Width - circleSize) / 2,
                artwork.Y + (artwork.Height - circleSize) / 2,
                circleSize,
                circleSize);
            using SolidBrush circleBrush = new(Color.FromArgb(85, 15, 18, 29));
            graphics.FillEllipse(circleBrush, circle);

            float noteSize = Math.Max(42F, artwork.Width * 0.30F);
            using Font noteFont = new("Segoe UI Symbol", noteSize, FontStyle.Regular);
            TextRenderer.DrawText(
                graphics,
                "♫",
                noteFont,
                circle,
                Color.White,
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.NoPadding);
        }

        private void DrawMetadata(Graphics graphics, Rectangle artwork)
        {
            Rectangle nameBounds = new(
                Math.Max(20, ClientSize.Width / 2 - 300),
                artwork.Bottom + 26,
                Math.Min(600, ClientSize.Width - 40),
                34);
            using Font nameFont = new("Segoe UI Semibold", 15F);
            TextRenderer.DrawText(
                graphics,
                string.IsNullOrWhiteSpace(FileName) ? "Audio" : FileName,
                nameFont,
                nameBounds,
                Color.White,
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis |
                TextFormatFlags.SingleLine);

            string state = IsPlaying ? "PLAYING" : "PAUSED";
            string details = string.IsNullOrWhiteSpace(Format)
                ? state
                : $"{Format.ToUpperInvariant()}  •  {state}";
            Rectangle detailsBounds = new(
                nameBounds.X,
                nameBounds.Bottom + 4,
                nameBounds.Width,
                26);
            using Font detailsFont = new("Segoe UI Semibold", 9F);
            TextRenderer.DrawText(
                graphics,
                details,
                detailsFont,
                detailsBounds,
                Color.FromArgb(174, 181, 202),
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine);
        }

        private static GraphicsPath CreateRoundedRectangle(Rectangle rectangle, int radius)
        {
            int diameter = Math.Min(radius * 2, Math.Min(rectangle.Width, rectangle.Height));
            GraphicsPath path = new();
            if (diameter <= 0)
                return path;

            Rectangle arc = new(rectangle.Location, new Size(diameter, diameter));
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
