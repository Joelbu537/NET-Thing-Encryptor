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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CreateFileForm));
            tableLayoutPanel = new TableLayoutPanel();
            flowLayoutPanelControlls = new FlowLayoutPanel();
            buttonAddFiles = new Button();
            buttonRemoveSelected = new Button();
            flowLayoutPanelActions = new FlowLayoutPanel();
            buttonImport = new Button();
            buttonCancel = new Button();
            listViewFiles = new ListView();
            columnHeaderName = new ColumnHeader();
            columnHeaderPath = new ColumnHeader();
            imageListFileIcons = new ImageList(components);
            tableLayoutPanel.SuspendLayout();
            flowLayoutPanelControlls.SuspendLayout();
            flowLayoutPanelActions.SuspendLayout();
            SuspendLayout();
            // 
            // tableLayoutPanel
            // 
            tableLayoutPanel.ColumnCount = 1;
            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel.Controls.Add(flowLayoutPanelControlls, 0, 0);
            tableLayoutPanel.Controls.Add(flowLayoutPanelActions, 0, 2);
            tableLayoutPanel.Controls.Add(listViewFiles, 0, 1);
            tableLayoutPanel.Dock = DockStyle.Fill;
            tableLayoutPanel.Location = new Point(0, 0);
            tableLayoutPanel.Name = "tableLayoutPanel";
            tableLayoutPanel.RowCount = 3;
            tableLayoutPanel.RowStyles.Add(new RowStyle());
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel.RowStyles.Add(new RowStyle());
            tableLayoutPanel.Size = new Size(1028, 584);
            tableLayoutPanel.TabIndex = 0;
            // 
            // flowLayoutPanelControlls
            // 
            flowLayoutPanelControlls.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            flowLayoutPanelControlls.AutoSize = true;
            flowLayoutPanelControlls.Controls.Add(buttonAddFiles);
            flowLayoutPanelControlls.Controls.Add(buttonRemoveSelected);
            flowLayoutPanelControlls.Location = new Point(3, 3);
            flowLayoutPanelControlls.Name = "flowLayoutPanelControlls";
            flowLayoutPanelControlls.Size = new Size(1022, 48);
            flowLayoutPanelControlls.TabIndex = 3;
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
            buttonAddFiles.Click += buttonAddFiles_Click;
            // 
            // buttonRemoveSelected
            // 
            buttonRemoveSelected.AutoSize = true;
            buttonRemoveSelected.Enabled = false;
            buttonRemoveSelected.Location = new Point(130, 3);
            buttonRemoveSelected.Name = "buttonRemoveSelected";
            buttonRemoveSelected.Size = new Size(208, 42);
            buttonRemoveSelected.TabIndex = 1;
            buttonRemoveSelected.Text = "Remove Selected";
            buttonRemoveSelected.UseVisualStyleBackColor = true;
            buttonRemoveSelected.Click += buttonRemoveSelected_Click;
            // 
            // flowLayoutPanelActions
            // 
            flowLayoutPanelActions.AutoSize = true;
            flowLayoutPanelActions.Controls.Add(buttonImport);
            flowLayoutPanelActions.Controls.Add(buttonCancel);
            flowLayoutPanelActions.Dock = DockStyle.Fill;
            flowLayoutPanelActions.FlowDirection = FlowDirection.RightToLeft;
            flowLayoutPanelActions.Location = new Point(3, 533);
            flowLayoutPanelActions.Name = "flowLayoutPanelActions";
            flowLayoutPanelActions.Size = new Size(1022, 48);
            flowLayoutPanelActions.TabIndex = 2;
            // 
            // buttonImport
            // 
            buttonImport.AutoSize = true;
            buttonImport.Enabled = false;
            buttonImport.Location = new Point(879, 3);
            buttonImport.Name = "buttonImport";
            buttonImport.Size = new Size(140, 42);
            buttonImport.TabIndex = 0;
            buttonImport.Text = "Import";
            buttonImport.UseVisualStyleBackColor = true;
            buttonImport.Click += buttonImport_Click;
            // 
            // buttonCancel
            // 
            buttonCancel.AutoSize = true;
            buttonCancel.Location = new Point(733, 3);
            buttonCancel.Name = "buttonCancel";
            buttonCancel.Size = new Size(140, 42);
            buttonCancel.TabIndex = 1;
            buttonCancel.Text = "Cancel";
            buttonCancel.UseVisualStyleBackColor = true;
            buttonCancel.Click += buttonCancel_Click;
            // 
            // listViewFiles
            // 
            listViewFiles.Columns.AddRange(new ColumnHeader[] { columnHeaderName, columnHeaderPath });
            listViewFiles.Dock = DockStyle.Fill;
            listViewFiles.Font = new Font("Segoe UI", 10F);
            listViewFiles.FullRowSelect = true;
            listViewFiles.GridLines = true;
            listViewFiles.Location = new Point(3, 57);
            listViewFiles.Name = "listViewFiles";
            listViewFiles.Size = new Size(1022, 470);
            listViewFiles.SmallImageList = imageListFileIcons;
            listViewFiles.TabIndex = 4;
            listViewFiles.UseCompatibleStateImageBehavior = false;
            listViewFiles.View = View.Details;
            listViewFiles.SelectedIndexChanged += listViewFiles_SelectedIndexChanged;
            // 
            // columnHeaderName
            // 
            columnHeaderName.Text = "Name";
            columnHeaderName.Width = 500;
            // 
            // columnHeaderPath
            // 
            columnHeaderPath.Text = "Path";
            columnHeaderPath.Width = 500;
            // 
            // imageListFileIcons
            // 
            imageListFileIcons.ColorDepth = ColorDepth.Depth32Bit;
            imageListFileIcons.ImageStream = (ImageListStreamer)resources.GetObject("imageListFileIcons.ImageStream");
            imageListFileIcons.TransparentColor = Color.Transparent;
            imageListFileIcons.Images.SetKeyName(0, "audio");
            imageListFileIcons.Images.SetKeyName(1, "image");
            imageListFileIcons.Images.SetKeyName(2, "other");
            imageListFileIcons.Images.SetKeyName(3, "video");
            imageListFileIcons.Images.SetKeyName(4, "text");
            // 
            // CreateFileForm
            // 
            AcceptButton = buttonImport;
            AutoScaleDimensions = new SizeF(13F, 32F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = buttonCancel;
            ClientSize = new Size(1028, 584);
            Controls.Add(tableLayoutPanel);
            Font = new Font("Segoe UI", 12F);
            Margin = new Padding(4);
            MinimizeBox = false;
            MinimumSize = new Size(1050, 640);
            Name = "CreateFileForm";
            ShowIcon = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Create new File";
            Load += CreateFileForm_Load;
            tableLayoutPanel.ResumeLayout(false);
            tableLayoutPanel.PerformLayout();
            flowLayoutPanelControlls.ResumeLayout(false);
            flowLayoutPanelControlls.PerformLayout();
            flowLayoutPanelActions.ResumeLayout(false);
            flowLayoutPanelActions.PerformLayout();
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
        private FlowLayoutPanel flowLayoutPanelActions;
        private Button buttonImport;
        private Button buttonCancel;
        private FlowLayoutPanel flowLayoutPanelControlls;
        private Button buttonAddFiles;
        private Button buttonRemoveSelected;
        private ListView listViewFiles;
        private ImageList imageListFileIcons;
        private ColumnHeader columnHeaderName;
        private ColumnHeader columnHeaderPath;
    }
}