using System.Runtime.InteropServices;

namespace NET_Thing_Encryptor
{
    internal readonly record struct ThemePalette(
        Color Background,
        Color Surface,
        Color SurfaceRaised,
        Color Border,
        Color Text,
        Color MutedText,
        Color Accent,
        Color Hover,
        Color Selection);

    internal static class AppTheme
    {
        private const int DwmUseImmersiveDarkMode = 20;

        private static readonly ThemePalette DarkPalette = new(
            Color.FromArgb(24, 24, 27),
            Color.FromArgb(31, 31, 35),
            Color.FromArgb(42, 42, 47),
            Color.FromArgb(63, 63, 70),
            Color.FromArgb(244, 244, 245),
            Color.FromArgb(161, 161, 170),
            Color.FromArgb(79, 134, 247),
            Color.FromArgb(55, 55, 62),
            Color.FromArgb(49, 86, 150));

        private static readonly ThemePalette LightPalette = new(
            Color.FromArgb(245, 247, 250),
            Color.White,
            Color.FromArgb(236, 239, 243),
            Color.FromArgb(210, 215, 222),
            Color.FromArgb(31, 41, 55),
            Color.FromArgb(102, 112, 133),
            Color.FromArgb(45, 105, 220),
            Color.FromArgb(225, 230, 237),
            Color.FromArgb(205, 222, 250));

        public static void Apply(Form form, bool darkMode)
        {
            ThemePalette palette = darkMode ? DarkPalette : LightPalette;
            form.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            ApplyControl(form, palette, darkMode);
            form.HandleCreated += (_, _) => ApplyNativeTheme(form, darkMode);
            if (form.IsHandleCreated)
                ApplyNativeTheme(form, darkMode);
        }

        public static void Apply(ToolStrip toolStrip, bool darkMode)
        {
            StyleToolStrip(toolStrip, darkMode ? DarkPalette : LightPalette);
        }

        public static void StylePrimaryButton(Button button, bool darkMode)
        {
            ThemePalette palette = darkMode ? DarkPalette : LightPalette;
            StyleButton(button, palette);
            button.BackColor = palette.Accent;
            button.ForeColor = Color.White;
            button.FlatAppearance.BorderColor = palette.Accent;
            button.FlatAppearance.MouseOverBackColor = ControlPaint.Light(palette.Accent, 0.08F);
            button.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(palette.Accent, 0.08F);
        }

        private static void ApplyControl(Control control, ThemePalette palette, bool darkMode)
        {
            switch (control)
            {
                case Form:
                    control.BackColor = palette.Background;
                    control.ForeColor = palette.Text;
                    break;

                case RichTextBox richTextBox:
                    richTextBox.BackColor = palette.Surface;
                    richTextBox.ForeColor = palette.Text;
                    richTextBox.BorderStyle = BorderStyle.None;
                    break;

                case TextBox textBox:
                    textBox.BackColor = textBox.ReadOnly ? palette.SurfaceRaised : palette.Surface;
                    textBox.ForeColor = palette.Text;
                    textBox.BorderStyle = BorderStyle.FixedSingle;
                    break;

                case ComboBox comboBox:
                    comboBox.BackColor = palette.Surface;
                    comboBox.ForeColor = palette.Text;
                    comboBox.FlatStyle = FlatStyle.Flat;
                    break;

                case NumericUpDown numeric:
                    numeric.BackColor = palette.Surface;
                    numeric.ForeColor = palette.Text;
                    numeric.BorderStyle = BorderStyle.FixedSingle;
                    break;

                case ListView listView:
                    listView.BackColor = palette.Surface;
                    listView.ForeColor = palette.Text;
                    listView.BorderStyle = BorderStyle.None;
                    listView.HideSelection = false;
                    ConfigureListViewHeader(listView, palette, darkMode);
                    listView.HandleCreated += (_, _) =>
                        SetWindowTheme(listView.Handle, darkMode ? "DarkMode_Explorer" : "Explorer", null);
                    if (listView.IsHandleCreated)
                        SetWindowTheme(listView.Handle, darkMode ? "DarkMode_Explorer" : "Explorer", null);
                    break;

                case Button button:
                    StyleButton(button, palette);
                    break;

                case StatusStrip statusStrip:
                    StyleToolStrip(statusStrip, palette);
                    statusStrip.SizingGrip = false;
                    break;

                case ToolStrip toolStrip:
                    StyleToolStrip(toolStrip, palette);
                    break;

                case Panel panel when panel.Name.Contains("warning", StringComparison.OrdinalIgnoreCase):
                    foreach (Control child in panel.Controls)
                    {
                        child.BackColor = Color.Transparent;
                        child.ForeColor = Color.White;
                    }
                    return;

                case TableLayoutPanel or FlowLayoutPanel or Panel:
                    control.BackColor = IsSurfaceContainer(control.Name)
                        ? palette.Surface
                        : palette.Background;
                    control.ForeColor = palette.Text;
                    break;

                case Label label:
                    if (label.BackColor == Color.Red)
                        break;
                    label.ForeColor = label.Name.Contains("Muted", StringComparison.OrdinalIgnoreCase)
                        ? palette.MutedText
                        : palette.Text;
                    if (label.BackColor != Color.Transparent)
                        label.BackColor = Color.Transparent;
                    break;

                case CheckBox checkBox:
                    checkBox.ForeColor = palette.Text;
                    checkBox.BackColor = Color.Transparent;
                    break;

                case GroupBox groupBox:
                    groupBox.ForeColor = palette.Text;
                    groupBox.BackColor = Color.Transparent;
                    break;
            }

            foreach (Control child in control.Controls)
                ApplyControl(child, palette, darkMode);
        }

        private static void ConfigureListViewHeader(
            ListView listView,
            ThemePalette palette,
            bool darkMode)
        {
            if (!darkMode)
                return;

            listView.OwnerDraw = true;
            listView.DrawColumnHeader += (_, e) =>
            {
                using SolidBrush background = new(palette.SurfaceRaised);
                using Pen border = new(palette.Border);
                e.Graphics.FillRectangle(background, e.Bounds);
                e.Graphics.DrawLine(
                    border,
                    e.Bounds.Left,
                    e.Bounds.Bottom - 1,
                    e.Bounds.Right,
                    e.Bounds.Bottom - 1);

                Rectangle textBounds = Rectangle.Inflate(e.Bounds, -8, 0);
                TextRenderer.DrawText(
                    e.Graphics,
                    e.Header?.Text ?? string.Empty,
                    listView.Font,
                    textBounds,
                    palette.Text,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine);
            };
            listView.DrawItem += (_, e) => e.DrawDefault = true;
            listView.DrawSubItem += (_, e) => e.DrawDefault = true;
        }

        private static void StyleButton(Button button, ThemePalette palette)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.Cursor = Cursors.Hand;
            button.ForeColor = palette.Text;
            button.BackColor = button.BackgroundImage is null
                ? palette.SurfaceRaised
                : palette.Surface;
            button.FlatAppearance.BorderColor = palette.Border;
            button.FlatAppearance.BorderSize = button.BackgroundImage is null ? 1 : 0;
            button.FlatAppearance.MouseOverBackColor = palette.Hover;
            button.FlatAppearance.MouseDownBackColor = palette.Selection;
            if (button.BackgroundImage is not null)
                button.BackgroundImageLayout = ImageLayout.Zoom;
        }

        private static void StyleToolStrip(ToolStrip toolStrip, ThemePalette palette)
        {
            toolStrip.BackColor = palette.Surface;
            toolStrip.ForeColor = palette.Text;
            toolStrip.Renderer = new ToolStripProfessionalRenderer(new ThemeColorTable(palette));
            toolStrip.GripStyle = ToolStripGripStyle.Hidden;

            foreach (ToolStripItem item in toolStrip.Items)
                StyleToolStripItem(item, palette);
        }

        private static void StyleToolStripItem(ToolStripItem item, ThemePalette palette)
        {
            item.ForeColor = palette.Text;
            if (item is ToolStripTextBox textBox)
            {
                textBox.BackColor = palette.SurfaceRaised;
                textBox.ForeColor = palette.Text;
            }
            else if (item is ToolStripComboBox comboBox)
            {
                comboBox.BackColor = palette.SurfaceRaised;
                comboBox.ForeColor = palette.Text;
            }
            else if (item is ToolStripDropDownItem dropDown)
            {
                dropDown.DropDown.BackColor = palette.Surface;
                dropDown.DropDown.ForeColor = palette.Text;
                dropDown.DropDown.Renderer =
                    new ToolStripProfessionalRenderer(new ThemeColorTable(palette));
                foreach (ToolStripItem child in dropDown.DropDownItems)
                    StyleToolStripItem(child, palette);
            }
        }

        private static bool IsSurfaceContainer(string name)
        {
            return name.Contains("Navigation", StringComparison.OrdinalIgnoreCase) ||
                   name.Contains("Info", StringComparison.OrdinalIgnoreCase) ||
                   name.Contains("control", StringComparison.OrdinalIgnoreCase) ||
                   name.Contains("flowLayoutPanel1", StringComparison.OrdinalIgnoreCase);
        }

        private static void ApplyNativeTheme(Form form, bool darkMode)
        {
            int enabled = darkMode ? 1 : 0;
            _ = DwmSetWindowAttribute(
                form.Handle,
                DwmUseImmersiveDarkMode,
                ref enabled,
                Marshal.SizeOf<int>());
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(
            IntPtr window,
            int attribute,
            ref int attributeValue,
            int attributeSize);

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr handle, string? subAppName, string? subIdList);

        private sealed class ThemeColorTable(ThemePalette palette) : ProfessionalColorTable
        {
            public override Color ToolStripGradientBegin => palette.Surface;
            public override Color ToolStripGradientMiddle => palette.Surface;
            public override Color ToolStripGradientEnd => palette.Surface;
            public override Color MenuStripGradientBegin => palette.Surface;
            public override Color MenuStripGradientEnd => palette.Surface;
            public override Color StatusStripGradientBegin => palette.Surface;
            public override Color StatusStripGradientEnd => palette.Surface;
            public override Color ToolStripDropDownBackground => palette.Surface;
            public override Color ImageMarginGradientBegin => palette.Surface;
            public override Color ImageMarginGradientMiddle => palette.Surface;
            public override Color ImageMarginGradientEnd => palette.Surface;
            public override Color MenuItemSelected => palette.Hover;
            public override Color MenuItemBorder => palette.Border;
            public override Color MenuItemSelectedGradientBegin => palette.Hover;
            public override Color MenuItemSelectedGradientEnd => palette.Hover;
            public override Color MenuItemPressedGradientBegin => palette.SurfaceRaised;
            public override Color MenuItemPressedGradientEnd => palette.SurfaceRaised;
            public override Color ButtonSelectedBorder => palette.Border;
            public override Color ButtonSelectedGradientBegin => palette.Hover;
            public override Color ButtonSelectedGradientEnd => palette.Hover;
            public override Color ButtonPressedGradientBegin => palette.Selection;
            public override Color ButtonPressedGradientEnd => palette.Selection;
            public override Color SeparatorDark => palette.Border;
            public override Color SeparatorLight => palette.SurfaceRaised;
        }
    }
}
