using System.Text;

namespace NET_Thing_Encryptor
{
    public partial class TextEditorForm : Form
    {
        private ThingFile _file;
        private readonly Encoding _encoding;
        private readonly bool _emitByteOrderMark;

        private string _savedText = string.Empty;
        private bool _isLoading = true;
        private bool _isSaving;
        private bool _allowClose;

        public TextEditorForm(ThingFile file)
        {
            ArgumentNullException.ThrowIfNull(file);
            if (file.Type != FileType.text || file.Content is null)
                throw new ArgumentException("The file must contain text data.", nameof(file));

            InitializeComponent();
            KeyPreview = true;

            DecodedText decoded = DecodeText(file.Content);
            _file = file;
            _encoding = decoded.Encoding;
            _emitByteOrderMark = decoded.HasByteOrderMark;

            LoadTextAsClean(decoded.Text);

            EditorAppearance.InitializeFontControls(
                editor,
                toolStripComboBoxFontFamily,
                toolStripComboBoxFontSize);
            UpdateEditingState();
            // Applying a font or ReadOnly can make RichTextBox rebuild its internal
            // RTF and normalize line endings once more.
            _savedText = editor.Text;
            editor.Modified = false;
            _isLoading = false;
            UpdateTitle();
            UpdateStatus();
        }

        private async void toolStripButtonSave_Click(object sender, EventArgs e)
        {
            await SaveAsync();
        }

        private void toolStripButtonUndo_Click(object sender, EventArgs e)
        {
            if (editor.CanUndo)
                editor.Undo();
        }

        private void toolStripButtonRedo_Click(object sender, EventArgs e)
        {
            if (editor.CanRedo)
                editor.Redo();
        }

        private void toolStripButtonReadOnly_Click(object sender, EventArgs e)
        {
            UpdateEditingState();
            if (!editor.ReadOnly)
                editor.Focus();
            UpdateStatus();
        }

        private void toolStripComboBoxFont_SelectedIndexChanged(object sender, EventArgs e)
        {
            EditorAppearance.TryApplyFont(
                editor,
                toolStripComboBoxFontFamily,
                toolStripComboBoxFontSize);
        }

        private void toolStripComboBoxFontSize_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter)
                return;

            EditorAppearance.TryApplyFont(
                editor,
                toolStripComboBoxFontFamily,
                toolStripComboBoxFontSize);
            editor.Focus();
            e.Handled = true;
            e.SuppressKeyPress = true;
        }

        private void toolStripButtonFindNext_Click(object sender, EventArgs e)
        {
            FindNext();
        }

        private void toolStripTextBoxSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter)
                return;

            FindNext();
            e.Handled = true;
            e.SuppressKeyPress = true;
        }

        private void FindNext()
        {
            string searchText = toolStripTextBoxSearch.Text;
            if (string.IsNullOrEmpty(searchText))
                return;

            int startIndex = Math.Min(editor.SelectionStart + editor.SelectionLength, editor.TextLength);
            int match = editor.Find(searchText, startIndex, RichTextBoxFinds.None);
            if (match < 0 && startIndex > 0)
                match = editor.Find(searchText, 0, RichTextBoxFinds.None);

            if (match >= 0)
            {
                editor.Select(match, searchText.Length);
                editor.ScrollToCaret();
                editor.Focus();
            }
            else
            {
                statusLabelState.Text = "Text not found";
            }
        }

        private void editor_TextChanged(object sender, EventArgs e)
        {
            if (_isLoading)
                return;

            UpdateTitle();
            UpdateStatus();
        }

        private void editor_SelectionChanged(object sender, EventArgs e)
        {
            UpdateStatus();
        }

        private async Task<bool> SaveAsync()
        {
            if (_isSaving)
                return false;
            if (!IsDirty)
                return true;

            _isSaving = true;
            UpdateEditingState();
            statusLabelState.Text = "Saving…";

            try
            {
                byte[] content = EncodeText(editor.Text, _encoding, _emitByteOrderMark);
                ThingFile currentFile = await ThingData.LoadFileAsync<ThingFile>(_file.ID)
                    ?? throw new FileNotFoundException("The text file no longer exists.");
                currentFile.Content = content;
                await ThingData.SaveFileAsync(currentFile);
                await ThingData.UpdateObjectSizeAsync(currentFile.ID, content.LongLength);
                _file = currentFile;

                _savedText = editor.Text;
                editor.Modified = false;
                statusLabelState.Text = "Saved";
                UpdateTitle();
                UpdateStatus();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"The text file could not be saved: {ex.Message}",
                    "Save failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                statusLabelState.Text = "Save failed";
                return false;
            }
            finally
            {
                _isSaving = false;
                UpdateEditingState();
            }
        }

        private void UpdateEditingState()
        {
            bool editable = !toolStripButtonReadOnly.Checked && !_isSaving;
            editor.ReadOnly = !editable;
            toolStripButtonSave.Enabled = !_isSaving;
            toolStripButtonUndo.Enabled = editable;
            toolStripButtonRedo.Enabled = editable;
        }

        private bool IsDirty => !string.Equals(editor.Text, _savedText, StringComparison.Ordinal);

        private void LoadTextAsClean(string text)
        {
            editor.Text = text;
            _ = editor.Handle;
            // RichTextBox may normalize line endings while assigning Text.
            // Use its resulting value as the clean baseline.
            _savedText = editor.Text;
            editor.Modified = false;
        }

        private void UpdateTitle()
        {
            string extension = string.IsNullOrWhiteSpace(_file.Extension)
                ? string.Empty
                : $".{_file.Extension}";
            Text = $"{(IsDirty ? "● " : string.Empty)}{_file.Name}{extension} — Text Editor";
        }

        private void UpdateStatus()
        {
            int selectionStart = editor.SelectionStart;
            int line = editor.GetLineFromCharIndex(selectionStart);
            int firstCharacter = editor.GetFirstCharIndexFromLine(line);
            int column = firstCharacter < 0 ? 0 : selectionStart - firstCharacter;

            statusLabelPosition.Text = $"Ln {line + 1}, Col {column + 1}";
            statusLabelCharacters.Text = $"{editor.TextLength:N0} characters";
            statusLabelEncoding.Text =
                $"{_encoding.WebName.ToUpperInvariant()}{(_emitByteOrderMark ? " BOM" : string.Empty)}";
            if (_isSaving)
                return;
            statusLabelState.Text = IsDirty
                ? editor.ReadOnly ? "Modified — read only" : "Modified"
                : editor.ReadOnly ? "Read only" : "Saved";
        }

        private void TextEditorForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.S)
            {
                _ = SaveAsync();
            }
            else if (e.Control && e.KeyCode == Keys.F)
            {
                toolStripTextBoxSearch.Focus();
                toolStripTextBoxSearch.SelectAll();
            }
            else if (e.KeyCode == Keys.F3)
            {
                FindNext();
            }
            else if (e.KeyCode == Keys.Escape && toolStripTextBoxSearch.Focused)
            {
                editor.Focus();
            }
            else
            {
                return;
            }

            e.Handled = true;
            e.SuppressKeyPress = true;
        }

        private async void TextEditorForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_allowClose || !IsDirty)
                return;

            if (_isSaving)
            {
                e.Cancel = true;
                return;
            }

            DialogResult result = MessageBox.Show(
                "Save changes before closing?",
                "Unsaved changes",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            if (result == DialogResult.Cancel)
            {
                e.Cancel = true;
                return;
            }

            if (result == DialogResult.No)
                return;

            e.Cancel = true;
            if (await SaveAsync())
            {
                _allowClose = true;
                Close();
            }
        }

        private static DecodedText DecodeText(byte[] data)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            if (StartsWith(data, [0xEF, 0xBB, 0xBF]))
                return DecodeWithPreamble(data, new UTF8Encoding(true, true), 3);
            if (StartsWith(data, [0xFF, 0xFE, 0x00, 0x00]))
                return DecodeWithPreamble(data, new UTF32Encoding(false, true, true), 4);
            if (StartsWith(data, [0x00, 0x00, 0xFE, 0xFF]))
                return DecodeWithPreamble(data, new UTF32Encoding(true, true, true), 4);
            if (StartsWith(data, [0xFF, 0xFE]))
                return DecodeWithPreamble(data, new UnicodeEncoding(false, true, true), 2);
            if (StartsWith(data, [0xFE, 0xFF]))
                return DecodeWithPreamble(data, new UnicodeEncoding(true, true, true), 2);

            var utf8 = new UTF8Encoding(false, true);
            try
            {
                return new DecodedText(utf8.GetString(data), utf8, false);
            }
            catch (DecoderFallbackException)
            {
                Encoding windows1252 = Encoding.GetEncoding(1252);
                return new DecodedText(windows1252.GetString(data), windows1252, false);
            }
        }

        private static DecodedText DecodeWithPreamble(byte[] data, Encoding encoding, int preambleLength)
        {
            return new DecodedText(
                encoding.GetString(data, preambleLength, data.Length - preambleLength),
                encoding,
                true);
        }

        private static byte[] EncodeText(string text, Encoding encoding, bool includePreamble)
        {
            byte[] body = encoding.GetBytes(text);
            if (!includePreamble)
                return body;

            byte[] preamble = encoding.GetPreamble();
            if (preamble.Length == 0)
                return body;

            byte[] result = new byte[preamble.Length + body.Length];
            Buffer.BlockCopy(preamble, 0, result, 0, preamble.Length);
            Buffer.BlockCopy(body, 0, result, preamble.Length, body.Length);
            return result;
        }

        private static bool StartsWith(byte[] data, byte[] prefix)
        {
            return data.AsSpan().StartsWith(prefix);
        }

        private sealed record DecodedText(string Text, Encoding Encoding, bool HasByteOrderMark);
    }
}
