namespace NET_Thing_Encryptor
{
    partial class FormMain
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormMain));
            tableLayoutPanelMain = new TableLayoutPanel();
            tableLayoutPanelNavigation = new TableLayoutPanel();
            flowLayoutPanelNavigationButtons = new FlowLayoutPanel();
            buttonNavigationBack = new Button();
            buttonNavigationCreateFile = new Button();
            buttonNavigationCreateFolder = new Button();
            buttonNavigationDeleteSelected = new Button();
            buttonNavigationExport = new Button();
            buttonNavigationSettings = new Button();
            buttonNavigationRoot = new Button();
            textBoxNavigation = new TextBox();
            flowLayoutPanelInfo = new FlowLayoutPanel();
            labelInfoVersion = new Label();
            labelInfoFileCount = new Label();
            labelInfoFolderCount = new Label();
            labelInfoTotalSize = new Label();
            labelInfoSaving = new Label();
            tableLayoutPanelDiv = new TableLayoutPanel();
            listViewMain = new ListView();
            columnHeaderName = new ColumnHeader();
            columnHeaderSize = new ColumnHeader();
            columnHeaderCreated = new ColumnHeader();
            imageListFileIcons = new ImageList(components);
            toolTip = new ToolTip(components);
            contextMenuStrip = new ContextMenuStrip(components);
            toolStripMenuItemRename = new ToolStripMenuItem();
            toolStripSeparator1 = new ToolStripSeparator();
            toolStripMenuItemCopy = new ToolStripMenuItem();
            tableLayoutPanelMain.SuspendLayout();
            tableLayoutPanelNavigation.SuspendLayout();
            flowLayoutPanelNavigationButtons.SuspendLayout();
            flowLayoutPanelInfo.SuspendLayout();
            tableLayoutPanelDiv.SuspendLayout();
            contextMenuStrip.SuspendLayout();
            SuspendLayout();
            // 
            // tableLayoutPanelMain
            // 
            tableLayoutPanelMain.AutoSize = true;
            tableLayoutPanelMain.ColumnCount = 1;
            tableLayoutPanelMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanelMain.Controls.Add(tableLayoutPanelNavigation, 0, 0);
            tableLayoutPanelMain.Controls.Add(flowLayoutPanelInfo, 0, 2);
            tableLayoutPanelMain.Controls.Add(tableLayoutPanelDiv, 0, 1);
            tableLayoutPanelMain.Dock = DockStyle.Fill;
            tableLayoutPanelMain.Location = new Point(0, 0);
            tableLayoutPanelMain.Margin = new Padding(0);
            tableLayoutPanelMain.Name = "tableLayoutPanelMain";
            tableLayoutPanelMain.RowCount = 3;
            tableLayoutPanelMain.RowStyles.Add(new RowStyle());
            tableLayoutPanelMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanelMain.RowStyles.Add(new RowStyle());
            tableLayoutPanelMain.Size = new Size(1828, 944);
            tableLayoutPanelMain.TabIndex = 0;
            // 
            // tableLayoutPanelNavigation
            // 
            tableLayoutPanelNavigation.AutoSize = true;
            tableLayoutPanelNavigation.ColumnCount = 2;
            tableLayoutPanelNavigation.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanelNavigation.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanelNavigation.Controls.Add(flowLayoutPanelNavigationButtons, 0, 0);
            tableLayoutPanelNavigation.Controls.Add(textBoxNavigation, 1, 0);
            tableLayoutPanelNavigation.Dock = DockStyle.Fill;
            tableLayoutPanelNavigation.Location = new Point(3, 3);
            tableLayoutPanelNavigation.Name = "tableLayoutPanelNavigation";
            tableLayoutPanelNavigation.RowCount = 1;
            tableLayoutPanelNavigation.RowStyles.Add(new RowStyle());
            tableLayoutPanelNavigation.Size = new Size(1822, 60);
            tableLayoutPanelNavigation.TabIndex = 0;
            // 
            // flowLayoutPanelNavigationButtons
            // 
            flowLayoutPanelNavigationButtons.AutoSize = true;
            flowLayoutPanelNavigationButtons.Controls.Add(buttonNavigationBack);
            flowLayoutPanelNavigationButtons.Controls.Add(buttonNavigationCreateFile);
            flowLayoutPanelNavigationButtons.Controls.Add(buttonNavigationCreateFolder);
            flowLayoutPanelNavigationButtons.Controls.Add(buttonNavigationDeleteSelected);
            flowLayoutPanelNavigationButtons.Controls.Add(buttonNavigationExport);
            flowLayoutPanelNavigationButtons.Controls.Add(buttonNavigationSettings);
            flowLayoutPanelNavigationButtons.Controls.Add(buttonNavigationRoot);
            flowLayoutPanelNavigationButtons.Dock = DockStyle.Fill;
            flowLayoutPanelNavigationButtons.Location = new Point(3, 3);
            flowLayoutPanelNavigationButtons.Name = "flowLayoutPanelNavigationButtons";
            flowLayoutPanelNavigationButtons.Size = new Size(395, 54);
            flowLayoutPanelNavigationButtons.TabIndex = 1;
            flowLayoutPanelNavigationButtons.WrapContents = false;
            // 
            // buttonNavigationBack
            // 
            buttonNavigationBack.Anchor = AnchorStyles.Left;
            buttonNavigationBack.AutoSize = true;
            buttonNavigationBack.BackColor = Color.Transparent;
            buttonNavigationBack.BackgroundImage = Properties.Resources.imageres_return;
            buttonNavigationBack.BackgroundImageLayout = ImageLayout.Stretch;
            buttonNavigationBack.Location = new Point(3, 3);
            buttonNavigationBack.MinimumSize = new Size(48, 48);
            buttonNavigationBack.Name = "buttonNavigationBack";
            buttonNavigationBack.Size = new Size(48, 48);
            buttonNavigationBack.TabIndex = 0;
            buttonNavigationBack.TextImageRelation = TextImageRelation.ImageAboveText;
            buttonNavigationBack.UseVisualStyleBackColor = false;
            buttonNavigationBack.Click += buttonNavigationBack_Click;
            // 
            // buttonNavigationCreateFile
            // 
            buttonNavigationCreateFile.Anchor = AnchorStyles.Left;
            buttonNavigationCreateFile.AutoSize = true;
            buttonNavigationCreateFile.BackColor = Color.Transparent;
            buttonNavigationCreateFile.BackgroundImage = Properties.Resources.imageres_new_file;
            buttonNavigationCreateFile.BackgroundImageLayout = ImageLayout.Stretch;
            buttonNavigationCreateFile.Location = new Point(57, 3);
            buttonNavigationCreateFile.MinimumSize = new Size(48, 48);
            buttonNavigationCreateFile.Name = "buttonNavigationCreateFile";
            buttonNavigationCreateFile.Size = new Size(48, 48);
            buttonNavigationCreateFile.TabIndex = 5;
            buttonNavigationCreateFile.TextImageRelation = TextImageRelation.ImageAboveText;
            toolTip.SetToolTip(buttonNavigationCreateFile, "Create new file");
            buttonNavigationCreateFile.UseVisualStyleBackColor = false;
            buttonNavigationCreateFile.Click += buttonNavigationCreateFile_Click;
            // 
            // buttonNavigationCreateFolder
            // 
            buttonNavigationCreateFolder.Anchor = AnchorStyles.Left;
            buttonNavigationCreateFolder.AutoSize = true;
            buttonNavigationCreateFolder.BackColor = Color.Transparent;
            buttonNavigationCreateFolder.BackgroundImage = Properties.Resources.imageres_folder_empty;
            buttonNavigationCreateFolder.BackgroundImageLayout = ImageLayout.Stretch;
            buttonNavigationCreateFolder.Location = new Point(111, 3);
            buttonNavigationCreateFolder.MinimumSize = new Size(48, 48);
            buttonNavigationCreateFolder.Name = "buttonNavigationCreateFolder";
            buttonNavigationCreateFolder.Size = new Size(48, 48);
            buttonNavigationCreateFolder.TabIndex = 3;
            buttonNavigationCreateFolder.TextImageRelation = TextImageRelation.ImageAboveText;
            toolTip.SetToolTip(buttonNavigationCreateFolder, "Create new folder");
            buttonNavigationCreateFolder.UseVisualStyleBackColor = false;
            buttonNavigationCreateFolder.Click += buttonNavigationCreateFolder_Click;
            // 
            // buttonNavigationDeleteSelected
            // 
            buttonNavigationDeleteSelected.Anchor = AnchorStyles.Left;
            buttonNavigationDeleteSelected.AutoSize = true;
            buttonNavigationDeleteSelected.BackColor = Color.Transparent;
            buttonNavigationDeleteSelected.BackgroundImage = Properties.Resources.imageres_cross_red;
            buttonNavigationDeleteSelected.BackgroundImageLayout = ImageLayout.Stretch;
            buttonNavigationDeleteSelected.Location = new Point(165, 3);
            buttonNavigationDeleteSelected.MinimumSize = new Size(48, 48);
            buttonNavigationDeleteSelected.Name = "buttonNavigationDeleteSelected";
            buttonNavigationDeleteSelected.Size = new Size(48, 48);
            buttonNavigationDeleteSelected.TabIndex = 4;
            buttonNavigationDeleteSelected.TextImageRelation = TextImageRelation.ImageAboveText;
            toolTip.SetToolTip(buttonNavigationDeleteSelected, "Delete");
            buttonNavigationDeleteSelected.UseVisualStyleBackColor = false;
            buttonNavigationDeleteSelected.Click += buttonNavigationDeleteSelected_Click;
            // 
            // buttonNavigationExport
            // 
            buttonNavigationExport.Anchor = AnchorStyles.Left;
            buttonNavigationExport.AutoSize = true;
            buttonNavigationExport.BackColor = Color.Transparent;
            buttonNavigationExport.BackgroundImage = Properties.Resources.imageres_export;
            buttonNavigationExport.BackgroundImageLayout = ImageLayout.Stretch;
            buttonNavigationExport.Location = new Point(219, 3);
            buttonNavigationExport.MinimumSize = new Size(48, 48);
            buttonNavigationExport.Name = "buttonNavigationExport";
            buttonNavigationExport.Size = new Size(48, 48);
            buttonNavigationExport.TabIndex = 7;
            buttonNavigationExport.TextImageRelation = TextImageRelation.ImageAboveText;
            toolTip.SetToolTip(buttonNavigationExport, "Export to file system");
            buttonNavigationExport.UseVisualStyleBackColor = false;
            buttonNavigationExport.Click += buttonNavigationExport_Click;
            // 
            // buttonNavigationSettings
            // 
            buttonNavigationSettings.Anchor = AnchorStyles.Left;
            buttonNavigationSettings.AutoSize = true;
            buttonNavigationSettings.BackColor = Color.Transparent;
            buttonNavigationSettings.BackgroundImage = Properties.Resources.shell32_gear;
            buttonNavigationSettings.BackgroundImageLayout = ImageLayout.Stretch;
            buttonNavigationSettings.Location = new Point(273, 3);
            buttonNavigationSettings.MinimumSize = new Size(48, 48);
            buttonNavigationSettings.Name = "buttonNavigationSettings";
            buttonNavigationSettings.Size = new Size(48, 48);
            buttonNavigationSettings.TabIndex = 6;
            buttonNavigationSettings.TextImageRelation = TextImageRelation.ImageAboveText;
            toolTip.SetToolTip(buttonNavigationSettings, "Open settings");
            buttonNavigationSettings.UseVisualStyleBackColor = false;
            buttonNavigationSettings.Click += buttonNavigationSettings_Click;
            // 
            // buttonNavigationRoot
            // 
            buttonNavigationRoot.Anchor = AnchorStyles.Left;
            buttonNavigationRoot.AutoSize = true;
            buttonNavigationRoot.BackColor = Color.Transparent;
            buttonNavigationRoot.BackgroundImage = Properties.Resources.imageres_explorer;
            buttonNavigationRoot.BackgroundImageLayout = ImageLayout.Stretch;
            buttonNavigationRoot.Location = new Point(344, 3);
            buttonNavigationRoot.Margin = new Padding(20, 3, 3, 3);
            buttonNavigationRoot.MinimumSize = new Size(48, 48);
            buttonNavigationRoot.Name = "buttonNavigationRoot";
            buttonNavigationRoot.Size = new Size(48, 48);
            buttonNavigationRoot.TabIndex = 2;
            buttonNavigationRoot.TextImageRelation = TextImageRelation.ImageAboveText;
            toolTip.SetToolTip(buttonNavigationRoot, "Return to root directory");
            buttonNavigationRoot.UseVisualStyleBackColor = false;
            buttonNavigationRoot.Click += buttonNavigationRoot_Click;
            // 
            // textBoxNavigation
            // 
            textBoxNavigation.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            textBoxNavigation.Location = new Point(421, 10);
            textBoxNavigation.Margin = new Padding(20, 3, 20, 3);
            textBoxNavigation.Name = "textBoxNavigation";
            textBoxNavigation.ReadOnly = true;
            textBoxNavigation.Size = new Size(1381, 39);
            textBoxNavigation.TabIndex = 2;
            textBoxNavigation.Text = "/Root";
            toolTip.SetToolTip(textBoxNavigation, "Current directory");
            // 
            // flowLayoutPanelInfo
            // 
            flowLayoutPanelInfo.AutoSize = true;
            flowLayoutPanelInfo.Controls.Add(labelInfoVersion);
            flowLayoutPanelInfo.Controls.Add(labelInfoFileCount);
            flowLayoutPanelInfo.Controls.Add(labelInfoFolderCount);
            flowLayoutPanelInfo.Controls.Add(labelInfoTotalSize);
            flowLayoutPanelInfo.Controls.Add(labelInfoSaving);
            flowLayoutPanelInfo.Dock = DockStyle.Fill;
            flowLayoutPanelInfo.Font = new Font("Consolas", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
            flowLayoutPanelInfo.Location = new Point(3, 908);
            flowLayoutPanelInfo.Name = "flowLayoutPanelInfo";
            flowLayoutPanelInfo.Size = new Size(1822, 33);
            flowLayoutPanelInfo.TabIndex = 2;
            // 
            // labelInfoVersion
            // 
            labelInfoVersion.AutoSize = true;
            labelInfoVersion.Location = new Point(8, 5);
            labelInfoVersion.Margin = new Padding(8, 5, 8, 5);
            labelInfoVersion.Name = "labelInfoVersion";
            labelInfoVersion.Size = new Size(186, 23);
            labelInfoVersion.TabIndex = 4;
            labelInfoVersion.Text = "labelInfoVersion";
            // 
            // labelInfoFileCount
            // 
            labelInfoFileCount.AutoSize = true;
            labelInfoFileCount.Location = new Point(210, 5);
            labelInfoFileCount.Margin = new Padding(8, 5, 8, 5);
            labelInfoFileCount.Name = "labelInfoFileCount";
            labelInfoFileCount.Size = new Size(208, 23);
            labelInfoFileCount.TabIndex = 0;
            labelInfoFileCount.Text = "labelInfoFileCount";
            // 
            // labelInfoFolderCount
            // 
            labelInfoFolderCount.AutoSize = true;
            labelInfoFolderCount.Location = new Point(434, 5);
            labelInfoFolderCount.Margin = new Padding(8, 5, 8, 5);
            labelInfoFolderCount.Name = "labelInfoFolderCount";
            labelInfoFolderCount.Size = new Size(230, 23);
            labelInfoFolderCount.TabIndex = 1;
            labelInfoFolderCount.Text = "labelInfoFolderCount";
            // 
            // labelInfoTotalSize
            // 
            labelInfoTotalSize.AutoSize = true;
            labelInfoTotalSize.Location = new Point(680, 5);
            labelInfoTotalSize.Margin = new Padding(8, 5, 8, 5);
            labelInfoTotalSize.Name = "labelInfoTotalSize";
            labelInfoTotalSize.Size = new Size(208, 23);
            labelInfoTotalSize.TabIndex = 2;
            labelInfoTotalSize.Text = "labelInfoTotalSize";
            // 
            // labelInfoSaving
            // 
            labelInfoSaving.AutoSize = true;
            labelInfoSaving.BackColor = Color.Red;
            labelInfoSaving.ForeColor = Color.Black;
            labelInfoSaving.Location = new Point(901, 5);
            labelInfoSaving.Margin = new Padding(5);
            labelInfoSaving.Name = "labelInfoSaving";
            labelInfoSaving.Size = new Size(109, 23);
            labelInfoSaving.TabIndex = 3;
            labelInfoSaving.Text = "Saving...";
            labelInfoSaving.Visible = false;
            // 
            // tableLayoutPanelDiv
            // 
            tableLayoutPanelDiv.ColumnCount = 2;
            tableLayoutPanelDiv.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));
            tableLayoutPanelDiv.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
            tableLayoutPanelDiv.Controls.Add(listViewMain, 0, 0);
            tableLayoutPanelDiv.Dock = DockStyle.Fill;
            tableLayoutPanelDiv.Location = new Point(3, 69);
            tableLayoutPanelDiv.Name = "tableLayoutPanelDiv";
            tableLayoutPanelDiv.RowCount = 1;
            tableLayoutPanelDiv.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanelDiv.Size = new Size(1822, 833);
            tableLayoutPanelDiv.TabIndex = 3;
            // 
            // listViewMain
            // 
            listViewMain.Columns.AddRange(new ColumnHeader[] { columnHeaderName, columnHeaderSize, columnHeaderCreated });
            listViewMain.Dock = DockStyle.Fill;
            listViewMain.FullRowSelect = true;
            listViewMain.Location = new Point(3, 3);
            listViewMain.MultiSelect = false;
            listViewMain.Name = "listViewMain";
            listViewMain.Size = new Size(1178, 827);
            listViewMain.SmallImageList = imageListFileIcons;
            listViewMain.TabIndex = 3;
            listViewMain.UseCompatibleStateImageBehavior = false;
            listViewMain.View = View.Details;
            listViewMain.DoubleClick += listViewMain_DoubleClick;
            listViewMain.MouseDown += listViewMain_MouseDown;
            // 
            // columnHeaderName
            // 
            columnHeaderName.Text = "Name";
            columnHeaderName.Width = 600;
            // 
            // columnHeaderSize
            // 
            columnHeaderSize.Text = "Size";
            columnHeaderSize.Width = 250;
            // 
            // columnHeaderCreated
            // 
            columnHeaderCreated.Text = "Created At";
            columnHeaderCreated.Width = 300;
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
            imageListFileIcons.Images.SetKeyName(4, "folder");
            imageListFileIcons.Images.SetKeyName(5, "text");
            // 
            // contextMenuStrip
            // 
            contextMenuStrip.Font = new Font("Segoe UI", 12F);
            contextMenuStrip.ImageScalingSize = new Size(24, 24);
            contextMenuStrip.Items.AddRange(new ToolStripItem[] { toolStripMenuItemRename, toolStripSeparator1, toolStripMenuItemCopy });
            contextMenuStrip.Name = "contextMenuStrip";
            contextMenuStrip.Size = new Size(175, 86);
            // 
            // toolStripMenuItemRename
            // 
            toolStripMenuItemRename.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripMenuItemRename.Name = "toolStripMenuItemRename";
            toolStripMenuItemRename.Size = new Size(174, 38);
            toolStripMenuItemRename.Text = "Rename";
            toolStripMenuItemRename.TextAlign = ContentAlignment.MiddleLeft;
            toolStripMenuItemRename.TextImageRelation = TextImageRelation.TextAboveImage;
            toolStripMenuItemRename.Click += toolStripMenuItemRename_Click;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(171, 6);
            // 
            // toolStripMenuItemCopy
            // 
            toolStripMenuItemCopy.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripMenuItemCopy.Name = "toolStripMenuItemCopy";
            toolStripMenuItemCopy.Size = new Size(174, 38);
            toolStripMenuItemCopy.Text = "Copy";
            toolStripMenuItemCopy.TextAlign = ContentAlignment.MiddleLeft;
            toolStripMenuItemCopy.TextImageRelation = TextImageRelation.TextAboveImage;
            // 
            // FormMain
            // 
            AutoScaleDimensions = new SizeF(13F, 32F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1828, 944);
            Controls.Add(tableLayoutPanelMain);
            Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(4);
            MinimumSize = new Size(1850, 1000);
            Name = "FormMain";
            Text = ".NET Thing Encryptor";
            WindowState = FormWindowState.Maximized;
            FormClosing += FormMain_FormClosing;
            FormClosed += FormMain_FormClosed;
            Load += FormMain_Load;
            tableLayoutPanelMain.ResumeLayout(false);
            tableLayoutPanelMain.PerformLayout();
            tableLayoutPanelNavigation.ResumeLayout(false);
            tableLayoutPanelNavigation.PerformLayout();
            flowLayoutPanelNavigationButtons.ResumeLayout(false);
            flowLayoutPanelNavigationButtons.PerformLayout();
            flowLayoutPanelInfo.ResumeLayout(false);
            flowLayoutPanelInfo.PerformLayout();
            tableLayoutPanelDiv.ResumeLayout(false);
            contextMenuStrip.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TableLayoutPanel tableLayoutPanelMain;
        private TableLayoutPanel tableLayoutPanelNavigation;
        private Button buttonNavigationBack;
        private FlowLayoutPanel flowLayoutPanelNavigationButtons;
        private Button buttonNavigationRoot;
        private TextBox textBoxNavigation;
        private ImageList imageListFileIcons;
        private Button buttonNavigationCreateFile;
        private Button buttonNavigationCreateFolder;
        private Button buttonNavigationDeleteSelected;
        private Button buttonNavigationSettings;
        private FlowLayoutPanel flowLayoutPanelInfo;
        private Label labelInfoFileCount;
        private Label labelInfoFolderCount;
        private Label labelInfoTotalSize;
        private Label labelInfoSaving;
        private TableLayoutPanel tableLayoutPanelDiv;
        private ListView listViewMain;
        private ColumnHeader columnHeaderName;
        private ColumnHeader columnHeaderSize;
        private ColumnHeader columnHeaderCreated;
        private ToolTip toolTip;
        private Button buttonNavigationExport;
        private Label labelInfoVersion;
        private ContextMenuStrip contextMenuStrip;
        private ToolStripMenuItem toolStripMenuItemRename;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripMenuItem toolStripMenuItemCopy;
    }
}
