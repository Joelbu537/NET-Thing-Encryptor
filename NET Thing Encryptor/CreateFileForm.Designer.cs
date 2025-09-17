namespace NET_Thing_Encryptor
{
    partial class CreateFileForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            tableLayoutPanel = new TableLayoutPanel();
            flowLayoutPanel = new FlowLayoutPanel();
            labelSelect = new Label();
            comboBox = new ComboBox();
            tableLayoutPanelImage = new TableLayoutPanel();
            flowLayoutPanelImage = new FlowLayoutPanel();
            listViewImage = new ListView();
            buttonAddFiles = new Button();
            buttonRemoveSelected = new Button();
            flowLayoutPanel1 = new FlowLayoutPanel();
            buttonImport = new Button();
            buttonCancel = new Button();
            tableLayoutPanel.SuspendLayout();
            flowLayoutPanel.SuspendLayout();
            tableLayoutPanelImage.SuspendLayout();
            flowLayoutPanelImage.SuspendLayout();
            flowLayoutPanel1.SuspendLayout();
            SuspendLayout();
            // 
            // tableLayoutPanel
            // 
            tableLayoutPanel.ColumnCount = 1;
            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel.Controls.Add(flowLayoutPanel, 0, 0);
            tableLayoutPanel.Controls.Add(tableLayoutPanelImage, 0, 1);
            tableLayoutPanel.Controls.Add(flowLayoutPanel1, 0, 2);
            tableLayoutPanel.Dock = DockStyle.Fill;
            tableLayoutPanel.Location = new Point(0, 0);
            tableLayoutPanel.Name = "tableLayoutPanel";
            tableLayoutPanel.RowCount = 3;
            tableLayoutPanel.RowStyles.Add(new RowStyle());
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel.RowStyles.Add(new RowStyle());
            tableLayoutPanel.Size = new Size(1040, 584);
            tableLayoutPanel.TabIndex = 0;
            // 
            // flowLayoutPanel
            // 
            flowLayoutPanel.Controls.Add(labelSelect);
            flowLayoutPanel.Controls.Add(comboBox);
            flowLayoutPanel.Dock = DockStyle.Fill;
            flowLayoutPanel.Location = new Point(3, 3);
            flowLayoutPanel.Name = "flowLayoutPanel";
            flowLayoutPanel.Size = new Size(1034, 47);
            flowLayoutPanel.TabIndex = 0;
            // 
            // labelSelect
            // 
            labelSelect.Anchor = AnchorStyles.Top;
            labelSelect.AutoSize = true;
            labelSelect.Location = new Point(5, 5);
            labelSelect.Margin = new Padding(5);
            labelSelect.Name = "labelSelect";
            labelSelect.Size = new Size(244, 32);
            labelSelect.TabIndex = 0;
            labelSelect.Text = "Select type to import:";
            // 
            // comboBox
            // 
            comboBox.Anchor = AnchorStyles.Top;
            comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox.FormattingEnabled = true;
            comboBox.Items.AddRange(new object[] { "Image", "Video", "Audio", "Text", "Other (Store encrypted only, no viewer)" });
            comboBox.Location = new Point(257, 3);
            comboBox.Name = "comboBox";
            comboBox.Size = new Size(742, 40);
            comboBox.TabIndex = 1;
            comboBox.SelectedIndexChanged += comboBox_SelectedIndexChanged;
            // 
            // tableLayoutPanelImage
            // 
            tableLayoutPanelImage.ColumnCount = 1;
            tableLayoutPanelImage.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanelImage.Controls.Add(flowLayoutPanelImage, 0, 0);
            tableLayoutPanelImage.Controls.Add(listViewImage, 0, 1);
            tableLayoutPanelImage.Dock = DockStyle.Fill;
            tableLayoutPanelImage.Location = new Point(3, 56);
            tableLayoutPanelImage.Name = "tableLayoutPanelImage";
            tableLayoutPanelImage.RowCount = 2;
            tableLayoutPanelImage.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));
            tableLayoutPanelImage.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanelImage.Size = new Size(1034, 471);
            tableLayoutPanelImage.TabIndex = 1;
            // 
            // flowLayoutPanelImage
            // 
            flowLayoutPanelImage.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            flowLayoutPanelImage.AutoSize = true;
            flowLayoutPanelImage.Controls.Add(buttonAddFiles);
            flowLayoutPanelImage.Controls.Add(buttonRemoveSelected);
            flowLayoutPanelImage.Location = new Point(3, 6);
            flowLayoutPanelImage.Name = "flowLayoutPanelImage";
            flowLayoutPanelImage.Size = new Size(1028, 48);
            flowLayoutPanelImage.TabIndex = 0;
            // 
            // listViewImage
            // 
            listViewImage.Dock = DockStyle.Fill;
            listViewImage.Font = new Font("Segoe UI", 10F);
            listViewImage.Location = new Point(3, 63);
            listViewImage.Name = "listViewImage";
            listViewImage.Size = new Size(1028, 405);
            listViewImage.TabIndex = 1;
            listViewImage.UseCompatibleStateImageBehavior = false;
            listViewImage.View = View.List;
            // 
            // buttonAddFiles
            // 
            buttonAddFiles.AutoSize = true;
            buttonAddFiles.Location = new Point(3, 3);
            buttonAddFiles.Name = "buttonAddFiles";
            buttonAddFiles.Size = new Size(121, 42);
            buttonAddFiles.TabIndex = 0;
            buttonAddFiles.Text = "Add Files";
            buttonAddFiles.UseVisualStyleBackColor = true;
            // 
            // buttonRemoveSelected
            // 
            buttonRemoveSelected.AutoSize = true;
            buttonRemoveSelected.Location = new Point(130, 3);
            buttonRemoveSelected.Name = "buttonRemoveSelected";
            buttonRemoveSelected.Size = new Size(208, 42);
            buttonRemoveSelected.TabIndex = 1;
            buttonRemoveSelected.Text = "Remove Selected";
            buttonRemoveSelected.UseVisualStyleBackColor = true;
            // 
            // flowLayoutPanel1
            // 
            flowLayoutPanel1.AutoSize = true;
            flowLayoutPanel1.Controls.Add(buttonImport);
            flowLayoutPanel1.Controls.Add(buttonCancel);
            flowLayoutPanel1.Dock = DockStyle.Fill;
            flowLayoutPanel1.FlowDirection = FlowDirection.RightToLeft;
            flowLayoutPanel1.Location = new Point(3, 533);
            flowLayoutPanel1.Name = "flowLayoutPanel1";
            flowLayoutPanel1.Size = new Size(1034, 48);
            flowLayoutPanel1.TabIndex = 2;
            // 
            // buttonImport
            // 
            buttonImport.AutoSize = true;
            buttonImport.Location = new Point(891, 3);
            buttonImport.Name = "buttonImport";
            buttonImport.Size = new Size(140, 42);
            buttonImport.TabIndex = 0;
            buttonImport.Text = "Import";
            buttonImport.UseVisualStyleBackColor = true;
            // 
            // buttonCancel
            // 
            buttonCancel.AutoSize = true;
            buttonCancel.Location = new Point(745, 3);
            buttonCancel.Name = "buttonCancel";
            buttonCancel.Size = new Size(140, 42);
            buttonCancel.TabIndex = 1;
            buttonCancel.Text = "Cancel";
            buttonCancel.UseVisualStyleBackColor = true;
            // 
            // CreateFileForm
            // 
            AcceptButton = buttonImport;
            AutoScaleDimensions = new SizeF(13F, 32F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = buttonCancel;
            ClientSize = new Size(1040, 584);
            Controls.Add(tableLayoutPanel);
            Font = new Font("Segoe UI", 12F);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Margin = new Padding(4);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "CreateFileForm";
            ShowIcon = false;
            Text = "Create new File";
            Load += CreateFileForm_Load;
            tableLayoutPanel.ResumeLayout(false);
            tableLayoutPanel.PerformLayout();
            flowLayoutPanel.ResumeLayout(false);
            flowLayoutPanel.PerformLayout();
            tableLayoutPanelImage.ResumeLayout(false);
            tableLayoutPanelImage.PerformLayout();
            flowLayoutPanelImage.ResumeLayout(false);
            flowLayoutPanelImage.PerformLayout();
            flowLayoutPanel1.ResumeLayout(false);
            flowLayoutPanel1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel tableLayoutPanel;
        private FlowLayoutPanel flowLayoutPanel;
        private Label labelSelect;
        private ComboBox comboBox;
        private TableLayoutPanel tableLayoutPanelImage;
        private FlowLayoutPanel flowLayoutPanelImage;
        private ListView listViewImage;
        private Button buttonAddFiles;
        private Button buttonRemoveSelected;
        private FlowLayoutPanel flowLayoutPanel1;
        private Button buttonImport;
        private Button buttonCancel;
    }
}