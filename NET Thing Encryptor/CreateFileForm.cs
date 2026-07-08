using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

namespace NET_Thing_Encryptor
{
    public partial class CreateFileForm : Form
    {
        private const long LargeFileWarningThreshold = 256L * 1024 * 1024;
        private readonly ulong currentFolderID;
        public CreateFileForm(ThingFolder currentFolder)
        {
            ArgumentNullException.ThrowIfNull(currentFolder);
            currentFolderID = currentFolder.ID;
            InitializeComponent();
            bool darkMode = ThingData.Root?.DarkMode ?? true;
            AppTheme.Apply(this, darkMode);
            AppTheme.StylePrimaryButton(buttonImport, darkMode);
            listViewFiles.Resize += (_, _) => ResizeListColumns();
            ResizeListColumns();
        }

        private void ResizeListColumns()
        {
            int availableWidth = listViewFiles.ClientSize.Width -
                                 SystemInformation.VerticalScrollBarWidth - 8;
            if (availableWidth <= 0)
                return;

            columnHeaderName.Width = Math.Max(180, availableWidth / 3);
            columnHeaderPath.Width = Math.Max(260, availableWidth - columnHeaderName.Width);
        }
        private void buttonAddFiles_Click(object sender, EventArgs e)
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = false,
                Multiselect = true,
                Title = "Select Files",
                Filters =
                {
                    new CommonFileDialogFilter("All files", "*.*"),
                },
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                List<string> selectedFiles = dialog.FileNames.ToList();

                foreach (string file in selectedFiles)
                {
                    ListViewItem item = new ListViewItem(System.IO.Path.GetFileName(file));
                    item.SubItems.Add(file);
                    item.ImageKey = GetImageKey(file);
                    listViewFiles.Items.Add(item);
                }

                if (listViewFiles.Items.Count > 0)
                {
                    buttonImport.Enabled = true;
                }
            }
        }
        private string GetImageKey(string filePath)
        {
            return FileCategories.GetFileType(filePath) switch
            {
                FileType.text => "text",
                FileType.image => "image",
                FileType.audio => "audio",
                FileType.video => "video",
                _ => "other"
            };
        }

        private void buttonRemoveSelected_Click(object sender, EventArgs e)
        {
            if(listViewFiles.SelectedItems.Count > 0)
            {
                foreach(ListViewItem item in listViewFiles.SelectedItems)
                {
                    listViewFiles.Items.Remove(item);
                }
            }
            if (listViewFiles.Items.Count == 0)
            {
                buttonImport.Enabled = false;
            }
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            if(buttonCancel.Enabled == false) { return; }
            this.Close();
        }

        private async void buttonImport_Click(object sender, EventArgs e)
        {
            if (buttonImport.Enabled == false) { return; }
            buttonImport.Enabled = false;
            buttonCancel.Enabled = false;

            List<ListViewItem> importItems = FilterLargeImportItems(
                listViewFiles.Items.Cast<ListViewItem>().ToList());
            List<ThingObjectLink> existingContent = await ThingData.LoadFolderContent(currentFolderID);
            HashSet<string> usedNames = existingContent
                .Select(link => link.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (ListViewItem item in importItems)
            {
                ThingFile? importedFile = null;
                try
                {
                    string filePath = item.SubItems[1].Text;

                    if(!File.Exists(filePath))
                        throw new FileNotFoundException("File not found.", filePath);

                    string objectName = CreateUniqueName(
                        Path.GetFileNameWithoutExtension(filePath),
                        usedNames);
                    usedNames.Add(objectName);

                    importedFile = new ThingFile(
                        objectName,
                        await File.ReadAllBytesAsync(filePath));
                    Enum.TryParse<FileType>(item.ImageKey, true, out FileType result);
                    importedFile.Type = result;
                    importedFile.Extension = Path.GetExtension(filePath).TrimStart('.');
                    await ThingData.MoveFileToFolderAsync(importedFile, currentFolderID);
                    listViewFiles.Items.Remove(item);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error importing file: {ex.Message}");
                    item.BackColor = Color.Red;
                }
                finally
                {
                    long releasedBytes = importedFile?.Content?.LongLength ?? 0;
                    importedFile?.ReleaseContent();
                    MemoryMaintenance.NotifyLargeBufferReleased(releasedBytes);
                }
            }

            buttonCancel.Enabled = true;
        }

        private static List<ListViewItem> FilterLargeImportItems(List<ListViewItem> items)
        {
            List<ListViewItem> largeItems = items
                .Where(item => item.SubItems.Count > 1)
                .Where(item => File.Exists(item.SubItems[1].Text))
                .Where(item => new FileInfo(item.SubItems[1].Text).Length >= LargeFileWarningThreshold)
                .ToList();
            if (largeItems.Count == 0)
                return items;

            long totalLargeSize = largeItems.Sum(item => new FileInfo(item.SubItems[1].Text).Length);
            DialogResult result = MessageBox.Show(
                $"{largeItems.Count} queued file(s) are at least {LargeFileWarningThreshold.Sizeify()} each " +
                $"({totalLargeSize.Sizeify()} total). This storage format still has to load imported file content into memory while encrypting it.\n\n" +
                "Import these large files anyway?",
                "Large import",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Warning);

            return result switch
            {
                DialogResult.Yes => items,
                DialogResult.No => items.Except(largeItems).ToList(),
                _ => []
            };
        }

        private static string CreateUniqueName(string requestedName, ISet<string> usedNames)
        {
            if (!usedNames.Contains(requestedName))
                return requestedName;

            for (int index = 2; ; index++)
            {
                string candidate = $"{requestedName} ({index})";
                if (!usedNames.Contains(candidate))
                    return candidate;
            }
        }

        private void listViewFiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(listViewFiles.SelectedItems.Count > 0)
            {
                buttonRemoveSelected.Enabled = true;
            }
            else
            {
                buttonRemoveSelected.Enabled = false;
            }
        }
    }
}
