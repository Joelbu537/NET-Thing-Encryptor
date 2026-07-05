using System.Globalization;

namespace NET_Thing_Encryptor
{
    internal static class EditorAppearance
    {
        private static readonly float[] CommonFontSizes =
            [8, 9, 10, 11, 12, 14, 16, 18, 20, 24, 28, 32, 36, 48];

        public static void InitializeFontControls(
            RichTextBox editor,
            ToolStripComboBox fontFamily,
            ToolStripComboBox fontSize)
        {
            string[] families = FontFamily.Families
                .Select(family => family.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase)
                .ToArray();
            fontFamily.Items.AddRange(families.Cast<object>().ToArray());

            foreach (float size in CommonFontSizes)
                fontSize.Items.Add(size.ToString("0.##", CultureInfo.CurrentCulture));

            int familyIndex = fontFamily.FindStringExact(editor.Font.FontFamily.Name);
            if (familyIndex >= 0)
                fontFamily.SelectedIndex = familyIndex;
            else
                fontFamily.Text = editor.Font.FontFamily.Name;

            fontSize.Text = editor.Font.SizeInPoints.ToString("0.##", CultureInfo.CurrentCulture);
        }

        public static bool TryApplyFont(
            RichTextBox editor,
            ToolStripComboBox fontFamily,
            ToolStripComboBox fontSize)
        {
            string family = fontFamily.Text.Trim();
            if (string.IsNullOrWhiteSpace(family))
                family = editor.Font.FontFamily.Name;

            if (!TryParseSize(fontSize.Text, out float size))
            {
                fontSize.Text = editor.Font.SizeInPoints.ToString("0.##", CultureInfo.CurrentCulture);
                return false;
            }

            try
            {
                editor.Font = new Font(family, size, FontStyle.Regular, GraphicsUnit.Point);
                fontSize.Text = size.ToString("0.##", CultureInfo.CurrentCulture);
                return true;
            }
            catch (ArgumentException)
            {
                int currentFamily = fontFamily.FindStringExact(editor.Font.FontFamily.Name);
                if (currentFamily >= 0)
                    fontFamily.SelectedIndex = currentFamily;
                return false;
            }
        }

        private static bool TryParseSize(string value, out float size)
        {
            bool parsed =
                float.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out size) ||
                float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out size);
            return parsed && size is >= 6f and <= 72f;
        }
    }
}
