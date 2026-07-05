using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace NET_Thing_Encryptor
{
    public partial class EmergencyEditorForm : Form
    {
        private readonly ulong _id;
        private readonly System.Windows.Forms.Timer _validationTimer = new()
        {
            Interval = 350
        };

        private string _filePath = string.Empty;
        private string _encryptedBackupPath = string.Empty;
        private string _savedText = string.Empty;
        private bool _isLoading = true;
        private bool _isSaving;
        private bool _allowClose;

        public EmergencyEditorForm(ulong id)
        {
            if (id == 0)
                throw new ArgumentException("The root object cannot be opened in the emergency editor.", nameof(id));

            _id = id;
            InitializeComponent();
            KeyPreview = true;
            Text = $"Emergency Editor — {ThingData.IDToHex(_id)}";
            _validationTimer.Tick += ValidationTimer_Tick;
            EditorAppearance.InitializeFontControls(
                editor,
                toolStripComboBoxFontFamily,
                toolStripComboBoxFontSize);
            UpdateEditingState();
        }

        private async void EmergencyEditorForm_Load(object sender, EventArgs e)
        {
            try
            {
                _filePath = ThingData.GetFilePath(_id);
                Debug.WriteLine($"Emergency file path resolved to: {_filePath}");

                _encryptedBackupPath = await CreateEncryptedBackupAsync(_filePath);
                statusLabelBackup.Text = $"Encrypted backup: {Path.GetFileName(_encryptedBackupPath)}";
                statusLabelBackup.ToolTipText = _encryptedBackupPath;
                statusLabelBackup.ForeColor = Color.FromArgb(120, 210, 140);

                await using FileStream input = File.OpenRead(_filePath);
                await using MemoryStream decrypted = await ThingData.Decrypt(input);
                byte[] data = decrypted.ToArray();
                string text = new UTF8Encoding(false, true).GetString(data);

                LoadTextAsClean(text);
                _isLoading = false;
                UpdateEditingState();
                UpdateTitle();
                UpdateStatus();
                ValidateAndDisplay(showDialogOnFailure: false);
            }
            catch (Exception ex)
            {
                editor.ReadOnly = true;
                toolStripButtonSave.Enabled = false;
                statusLabelJson.Text = "Load failed";
                statusLabelJson.ForeColor = Color.FromArgb(255, 120, 120);
                MessageBox.Show(
                    $"The encrypted object could not be opened: {ex.Message}",
                    "Emergency editor error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private async void toolStripButtonSave_Click(object sender, EventArgs e)
        {
            await SaveAsync();
        }

        private void toolStripButtonReadOnly_Click(object sender, EventArgs e)
        {
            if (!toolStripButtonReadOnly.Checked)
            {
                DialogResult result = MessageBox.Show(
                    "Emergency editing can make this object unreadable. Enable editing?",
                    "Enable emergency editing",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                if (result != DialogResult.Yes)
                    toolStripButtonReadOnly.Checked = true;
            }

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

        private void toolStripButtonFormat_Click(object sender, EventArgs e)
        {
            if (!TryParseJson(out JsonDocument? document, out string? error))
            {
                ShowJsonError(error);
                return;
            }
            ArgumentNullException.ThrowIfNull(document);

            using (document)
            {
                int selection = editor.SelectionStart;
                editor.Text = JsonSerializer.Serialize(
                    document.RootElement,
                    new JsonSerializerOptions { WriteIndented = true });
                editor.SelectionStart = Math.Min(selection, editor.TextLength);
                editor.ScrollToCaret();
            }

            ValidateAndDisplay(showDialogOnFailure: false);
        }

        private void toolStripButtonValidate_Click(object sender, EventArgs e)
        {
            if (ValidateAndDisplay(showDialogOnFailure: true))
            {
                MessageBox.Show(
                    "The document contains valid JSON.",
                    "Validation successful",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        private async void toolStripButtonExportDecrypted_Click(object sender, EventArgs e)
        {
            DialogResult warning = MessageBox.Show(
                "This creates an unencrypted JSON copy. Anyone with access to that file can read its contents. Continue?",
                "Create decrypted copy?",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (warning != DialogResult.Yes)
                return;

            using var dialog = new SaveFileDialog
            {
                AddExtension = true,
                DefaultExt = "json",
                FileName = $"{ThingData.IDToHex(_id)} {DateTime.Now:yyyyMMdd-HHmmss}.json",
                Filter = "JSON files (*.json)|*.json|Text files (*.txt)|*.txt|All files (*.*)|*.*",
                InitialDirectory = Path.GetDirectoryName(_encryptedBackupPath)
            };
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                await File.WriteAllTextAsync(dialog.FileName, editor.Text, new UTF8Encoding(false));
                statusLabelState.Text = "Decrypted copy exported";
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"The decrypted copy could not be created: {ex.Message}",
                    "Export failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void editor_TextChanged(object sender, EventArgs e)
        {
            if (_isLoading)
                return;

            UpdateTitle();
            UpdateStatus();
            statusLabelJson.Text = "Checking…";
            statusLabelJson.ForeColor = Color.FromArgb(210, 210, 210);
            _validationTimer.Stop();
            _validationTimer.Start();
        }

        private void editor_SelectionChanged(object sender, EventArgs e)
        {
            UpdateStatus();
        }

        private void ValidationTimer_Tick(object? sender, EventArgs e)
        {
            _validationTimer.Stop();
            ValidateAndDisplay(showDialogOnFailure: false);
        }

        private async Task<bool> SaveAsync()
        {
            if (_isSaving)
                return false;
            if (!IsDirty)
                return true;
            _validationTimer.Stop();
            if (!ValidateAndDisplay(showDialogOnFailure: true))
                return false;

            _isSaving = true;
            UpdateEditingState();
            statusLabelState.Text = "Saving…";

            try
            {
                byte[] data = new UTF8Encoding(false).GetBytes(editor.Text);
                using var stream = new MemoryStream(data, writable: false);
                await ThingData.SaveEncryptedDataAsync(_id, stream);

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
                    $"The emergency changes could not be saved: {ex.Message}",
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

        private bool ValidateAndDisplay(bool showDialogOnFailure)
        {
            if (TryParseJson(out JsonDocument? document, out string? error))
            {
                ArgumentNullException.ThrowIfNull(document);
                document.Dispose();
                statusLabelJson.Text = "Valid JSON";
                statusLabelJson.ForeColor = Color.FromArgb(120, 210, 140);
                return true;
            }

            statusLabelJson.Text = "Invalid JSON";
            statusLabelJson.ForeColor = Color.FromArgb(255, 120, 120);
            if (showDialogOnFailure)
                ShowJsonError(error);
            return false;
        }

        private bool TryParseJson(out JsonDocument? document, out string? error)
        {
            try
            {
                document = JsonDocument.Parse(editor.Text);
                if (document.RootElement.ValueKind != JsonValueKind.Object)
                {
                    document.Dispose();
                    document = null;
                    error = "The root JSON value must be an object.";
                    return false;
                }

                error = null;
                return true;
            }
            catch (JsonException ex)
            {
                document = null;
                error = $"Line {ex.LineNumber + 1}, byte {ex.BytePositionInLine + 1}: {ex.Message}";
                return false;
            }
        }

        private static void ShowJsonError(string? error)
        {
            MessageBox.Show(
                error ?? "The document does not contain valid JSON.",
                "Invalid JSON",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        private void UpdateEditingState()
        {
            bool editable = !toolStripButtonReadOnly.Checked && !_isSaving;
            editor.ReadOnly = !editable;
            toolStripButtonSave.Enabled = !_isSaving;
            toolStripButtonFormat.Enabled = editable;
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
            Text = $"{(IsDirty ? "● " : string.Empty)}Emergency Editor — {ThingData.IDToHex(_id)}";
        }

        private void UpdateStatus()
        {
            int selectionStart = editor.SelectionStart;
            int line = editor.GetLineFromCharIndex(selectionStart);
            int firstCharacter = editor.GetFirstCharIndexFromLine(line);
            int column = firstCharacter < 0 ? 0 : selectionStart - firstCharacter;

            statusLabelPosition.Text = $"Ln {line + 1}, Col {column + 1}";
            statusLabelCharacters.Text = $"{editor.TextLength:N0} characters";
            if (!_isSaving)
            {
                statusLabelState.Text = IsDirty
                    ? editor.ReadOnly ? "Modified — read only" : "Modified"
                    : editor.ReadOnly ? "Read only" : "Saved";
            }
        }

        private static async Task<string> CreateEncryptedBackupAsync(string sourcePath)
        {
            string backupDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "NET Thing Encryptor",
                "Emergency Backups");
            return await CreateEncryptedBackupAsync(sourcePath, backupDirectory);
        }

        private static async Task<string> CreateEncryptedBackupAsync(
            string sourcePath,
            string backupDirectory)
        {
            Directory.CreateDirectory(backupDirectory);

            string backupPath = Path.Combine(
                backupDirectory,
                $"{Path.GetFileNameWithoutExtension(sourcePath)}-backup-" +
                $"{DateTime.Now:yyyyMMdd-HHmmss-fff}-{Guid.NewGuid():N}.nte");
            string temporaryPath = backupPath + ".tmp";

            try
            {
                await using (FileStream source = new(
                    sourcePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    81920,
                    FileOptions.Asynchronous | FileOptions.SequentialScan))
                await using (FileStream destination = new(
                    temporaryPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    81920,
                    FileOptions.Asynchronous | FileOptions.WriteThrough))
                {
                    await source.CopyToAsync(destination);
                    await destination.FlushAsync();
                    destination.Flush(flushToDisk: true);
                }

                File.Move(temporaryPath, backupPath);

                await using FileStream sourceVerification = File.OpenRead(sourcePath);
                await using FileStream backupVerification = File.OpenRead(backupPath);
                byte[] sourceHash = await SHA256.HashDataAsync(sourceVerification);
                byte[] backupHash = await SHA256.HashDataAsync(backupVerification);
                if (!CryptographicOperations.FixedTimeEquals(sourceHash, backupHash))
                {
                    File.Delete(backupPath);
                    throw new IOException("The encrypted backup failed its integrity check.");
                }

                return backupPath;
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
        }

        private void EmergencyEditorForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.S)
            {
                _ = SaveAsync();
            }
            else if (e.Control && e.Shift && e.KeyCode == Keys.F)
            {
                toolStripButtonFormat.PerformClick();
            }
            else if (e.KeyCode == Keys.F5)
            {
                toolStripButtonValidate.PerformClick();
            }
            else
            {
                return;
            }

            e.Handled = true;
            e.SuppressKeyPress = true;
        }

        private async void EmergencyEditorForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_allowClose || !IsDirty)
                return;

            if (_isSaving)
            {
                e.Cancel = true;
                return;
            }

            DialogResult result = MessageBox.Show(
                "Save emergency changes before closing?",
                "Unsaved emergency changes",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Warning);
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

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _validationTimer.Stop();
            _validationTimer.Tick -= ValidationTimer_Tick;
            _validationTimer.Dispose();
            base.OnFormClosed(e);
        }
    }
}
