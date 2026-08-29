namespace NET_Thing_Encryptor;

public sealed class GlobalFileSearchForm : Form
{
    private readonly Func<ulong, Task> _openFileAsync;
    private readonly TextBox _nameTextBox = new() { PlaceholderText = "Part of the file name" };
    private readonly ComboBox _typeComboBox = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _extensionTextBox = new() { PlaceholderText = "e.g. pdf or .pdf" };
    private readonly NumericUpDown _minimumSize = CreateSizeInput();
    private readonly NumericUpDown _maximumSize = CreateSizeInput();
    private readonly DateTimePicker _createdFrom = CreateDateInput();
    private readonly DateTimePicker _createdTo = CreateDateInput();
    private readonly Button _searchButton = new() { Text = "Search all files", AutoSize = true };
    private readonly Label _statusLabel = new() { AutoSize = true, Text = "Enter filters or search without filters to list every file." };
    private readonly ListView _results = new()
    {
        Dock = DockStyle.Fill,
        FullRowSelect = true,
        GridLines = true,
        MultiSelect = false,
        View = View.Details
    };
    private CancellationTokenSource? _searchCancellation;

    public GlobalFileSearchForm(Func<ulong, Task> openFileAsync)
    {
        _openFileAsync = openFileAsync ?? throw new ArgumentNullException(nameof(openFileAsync));
        Text = "Search all files";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(850, 560);
        Size = new Size(1100, 700);
        ShowIcon = false;

        _typeComboBox.Items.Add(new TypeChoice("All types", null));
        foreach (FileType type in Enum.GetValues<FileType>().Where(type => type != FileType.folder))
            _typeComboBox.Items.Add(new TypeChoice(ToDisplayName(type), type));
        _typeComboBox.SelectedIndex = 0;

        _results.Columns.Add("Name", 220);
        _results.Columns.Add("Type", 90);
        _results.Columns.Add("Extension", 90);
        _results.Columns.Add("Size", 110);
        _results.Columns.Add("Created", 110);
        _results.Columns.Add("Folder", 360);
        _results.DoubleClick += Results_DoubleClick;
        _results.KeyDown += Results_KeyDown;
        _searchButton.Click += SearchButton_Click;
        AcceptButton = _searchButton;

        Controls.Add(CreateLayout());
        AppTheme.Apply(this, ThingData.Root?.DarkMode == true);
    }

    private Control CreateLayout()
    {
        TableLayoutPanel layout = new()
        {
            ColumnCount = 1,
            RowCount = 3,
            Dock = DockStyle.Fill,
            Padding = new Padding(12)
        };
        layout.RowStyles.Add(new RowStyle());
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle());

        TableLayoutPanel filters = new()
        {
            AutoSize = true,
            ColumnCount = 4,
            Dock = DockStyle.Top,
            Margin = new Padding(0, 0, 0, 10)
        };
        filters.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        filters.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        AddFilter(filters, 0, "Name", _nameTextBox, "Type", _typeComboBox);
        AddFilter(filters, 1, "Extension", _extensionTextBox, "Minimum size (MB)", _minimumSize);
        AddFilter(filters, 2, "Created from", _createdFrom, "Maximum size (MB)", _maximumSize);
        AddFilter(filters, 3, "Created to", _createdTo, string.Empty, _searchButton);

        layout.Controls.Add(filters, 0, 0);
        layout.Controls.Add(_results, 0, 1);
        layout.Controls.Add(_statusLabel, 0, 2);
        return layout;
    }

    private static void AddFilter(
        TableLayoutPanel panel,
        int row,
        string firstLabel,
        Control firstControl,
        string secondLabel,
        Control secondControl)
    {
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Label leftLabel = new() { Text = firstLabel, AutoSize = true, Anchor = AnchorStyles.Left };
        Label rightLabel = new() { Text = secondLabel, AutoSize = true, Anchor = AnchorStyles.Left };
        firstControl.Dock = DockStyle.Fill;
        secondControl.Dock = DockStyle.Fill;
        firstControl.Margin = new Padding(6, 4, 18, 4);
        secondControl.Margin = new Padding(6, 4, 0, 4);
        panel.Controls.Add(leftLabel, 0, row);
        panel.Controls.Add(firstControl, 1, row);
        panel.Controls.Add(rightLabel, 2, row);
        panel.Controls.Add(secondControl, 3, row);
    }

    private async void SearchButton_Click(object? sender, EventArgs e)
    {
        if (_minimumSize.Value > 0 && _maximumSize.Value > 0 &&
            _minimumSize.Value > _maximumSize.Value)
        {
            MessageBox.Show(
                "The minimum size cannot be greater than the maximum size.",
                "Invalid size range",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        _searchCancellation?.Cancel();
        _searchCancellation?.Dispose();
        _searchCancellation = new CancellationTokenSource();
        _searchButton.Enabled = false;
        _results.Items.Clear();
        _statusLabel.Text = "Searching the complete vault...";

        try
        {
            TypeChoice typeChoice = (TypeChoice)_typeComboBox.SelectedItem!;
            GlobalFileSearchCriteria criteria = new(
                _nameTextBox.Text,
                typeChoice.Type,
                _extensionTextBox.Text,
                ToBytes(_minimumSize.Value),
                ToBytes(_maximumSize.Value),
                _createdFrom.Checked ? DateOnly.FromDateTime(_createdFrom.Value) : null,
                _createdTo.Checked ? DateOnly.FromDateTime(_createdTo.Value) : null);

            IReadOnlyList<GlobalFileSearchResult> results =
                await GlobalFileSearch.SearchAsync(criteria, _searchCancellation.Token);
            foreach (GlobalFileSearchResult result in results)
            {
                ListViewItem item = new(result.Name) { Name = result.ID.ToString(), Tag = result };
                item.SubItems.Add(ToDisplayName(result.Type));
                item.SubItems.Add(string.IsNullOrEmpty(result.Extension) ? "-" : "." + result.Extension);
                item.SubItems.Add(result.Size.Sizeify());
                item.SubItems.Add(result.CreatedAt.ToString());
                item.SubItems.Add(result.FolderPath);
                _results.Items.Add(item);
            }
            _statusLabel.Text = $"{results.Count} file(s) found. Double-click a result to open it.";
        }
        catch (OperationCanceledException)
        {
            _statusLabel.Text = "Search cancelled.";
        }
        catch (Exception ex)
        {
            _statusLabel.Text = "Search failed.";
            MessageBox.Show(
                $"The files could not be searched: {ex.Message}",
                "Search failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            _searchButton.Enabled = true;
        }
    }

    private async void Results_DoubleClick(object? sender, EventArgs e) => await OpenSelectedAsync();

    private async void Results_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter)
            return;
        e.Handled = true;
        await OpenSelectedAsync();
    }

    private async Task OpenSelectedAsync()
    {
        if (_results.SelectedItems.Count == 0)
            return;

        _results.Enabled = false;
        try
        {
            GlobalFileSearchResult result = (GlobalFileSearchResult)_results.SelectedItems[0].Tag!;
            await _openFileAsync(result.ID);
        }
        finally
        {
            _results.Enabled = true;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _searchCancellation?.Cancel();
            _searchCancellation?.Dispose();
        }
        base.Dispose(disposing);
    }

    private static NumericUpDown CreateSizeInput() => new()
    {
        DecimalPlaces = 2,
        Increment = 0.25M,
        Maximum = 1_000_000_000,
        ThousandsSeparator = true
    };

    private static DateTimePicker CreateDateInput() => new()
    {
        Format = DateTimePickerFormat.Short,
        ShowCheckBox = true,
        Checked = false
    };

    private static long? ToBytes(decimal megabytes)
    {
        if (megabytes <= 0)
            return null;
        return decimal.ToInt64(decimal.Round(megabytes * 1024 * 1024));
    }

    private static string ToDisplayName(FileType type) => type switch
    {
        FileType.audio => "Audio",
        FileType.image => "Image",
        FileType.text => "Text",
        FileType.video => "Video",
        FileType.other => "Other",
        _ => type.ToString()
    };

    private sealed record TypeChoice(string Name, FileType? Type)
    {
        public override string ToString() => Name;
    }
}
