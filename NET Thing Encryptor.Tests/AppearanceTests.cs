using System.Globalization;
using System.Reflection;
using NET_Thing_Encryptor;

namespace NET_Thing_Encryptor.Tests;

public sealed class AppearanceTests
{
    [Fact]
    public void EditorAppearance_InitializesSelectorsAndAppliesValidSize()
    {
        using var editor = new RichTextBox();
        using var family = new ToolStripComboBox();
        using var size = new ToolStripComboBox();

        EditorAppearance.InitializeFontControls(editor, family, size);
        Assert.NotEmpty(family.Items.Cast<object>());
        Assert.Contains(12f.ToString("0.##", CultureInfo.CurrentCulture), size.Items.Cast<object>());

        family.Text = editor.Font.FontFamily.Name;
        size.Text = "18";
        Assert.True(EditorAppearance.TryApplyFont(editor, family, size));
        Assert.Equal(18f, editor.Font.SizeInPoints);
    }

    [Fact]
    public void EditorAppearance_RejectsOutOfRangeOrMalformedSizes()
    {
        using var editor = new RichTextBox();
        using var family = new ToolStripComboBox { Text = editor.Font.FontFamily.Name };
        using var size = new ToolStripComboBox { Text = "1000" };

        Assert.False(EditorAppearance.TryApplyFont(editor, family, size));
        size.Text = "not a size";
        Assert.False(EditorAppearance.TryApplyFont(editor, family, size));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AppTheme_StylesFormsAndPrimaryButtons(bool darkMode)
    {
        using var form = new Form();
        using var label = new Label { BackColor = Color.White };
        using var button = new Button();
        form.Controls.Add(label);
        form.Controls.Add(button);

        AppTheme.Apply(form, darkMode);
        AppTheme.StylePrimaryButton(button, darkMode);

        Assert.Equal(Color.Transparent, label.BackColor);
        Assert.Equal(FlatStyle.Flat, button.FlatStyle);
        Assert.Equal(Color.White, button.ForeColor);
        Assert.Equal(Cursors.Hand, button.Cursor);
    }

    [Fact]
    public void AppTheme_StylesNestedToolStripItems()
    {
        using var strip = new ToolStrip();
        var menu = new ToolStripDropDownButton("menu");
        var textBox = new ToolStripTextBox();
        menu.DropDownItems.Add(textBox);
        strip.Items.Add(menu);

        AppTheme.Apply(strip, darkMode: true);

        Assert.Equal(ToolStripGripStyle.Hidden, strip.GripStyle);
        Assert.NotNull(strip.Renderer);
        Assert.NotEqual(SystemColors.ControlText, menu.ForeColor);
        Assert.NotEqual(SystemColors.Window, textBox.BackColor);
    }

    [Fact]
    public void VideoTimeline_KeyboardSeekingRaisesCompleteEventSequence()
    {
        using var timeline = new VideoTimeline { Value = 0.5 };
        var events = new List<string>();
        timeline.SeekStarted += (_, _) => events.Add("started");
        timeline.SeekPreview += (_, args) => events.Add(
            "preview:" + args.Position.ToString("0.00", CultureInfo.InvariantCulture));
        timeline.SeekCompleted += (_, args) => events.Add(
            "completed:" + args.Position.ToString("0.00", CultureInfo.InvariantCulture));
        var args = new KeyEventArgs(Keys.Right | Keys.Shift);

        typeof(VideoTimeline)
            .GetMethod("OnKeyDown", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(timeline, [args]);

        Assert.Equal(0.55, timeline.Value, precision: 6);
        Assert.Equal(["started", "preview:0.55", "completed:0.55"], events);
        Assert.True(args.Handled);
        Assert.True(args.SuppressKeyPress);
    }

    [Fact]
    public void AudioPlayerDisplay_NormalizesNullMetadataAndUpdatesAccessibility()
    {
        using var display = new AudioPlayerDisplay
        {
            FileName = "track.mp3",
            Format = "mp3",
            IsPlaying = true
        };
        Assert.Equal("Audio player for track.mp3", display.AccessibleName);
        Assert.True(display.IsPlaying);

        display.FileName = null!;
        display.Format = null!;
        Assert.Equal(string.Empty, display.FileName);
        Assert.Equal(string.Empty, display.Format);
        Assert.Equal("Audio player for ", display.AccessibleName);
    }
}
