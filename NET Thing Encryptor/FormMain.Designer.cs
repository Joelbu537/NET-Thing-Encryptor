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
            buttonNavigationRoot = new Button();
            textBoxNavigation = new TextBox();
            listViewMain = new ListView();
            columnHeaderName = new ColumnHeader();
            columnHeaderSize = new ColumnHeader();
            columnHeaderCreated = new ColumnHeader();
            imageListFileIcons = new ImageList(components);
            tableLayoutPanelMain.SuspendLayout();
            tableLayoutPanelNavigation.SuspendLayout();
            flowLayoutPanelNavigationButtons.SuspendLayout();
            SuspendLayout();
            // 
            // tableLayoutPanelMain
            // 
            tableLayoutPanelMain.AutoSize = true;
            tableLayoutPanelMain.ColumnCount = 1;
            tableLayoutPanelMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanelMain.Controls.Add(tableLayoutPanelNavigation, 0, 0);
            tableLayoutPanelMain.Controls.Add(listViewMain, 0, 1);
            tableLayoutPanelMain.Dock = DockStyle.Fill;
            tableLayoutPanelMain.Location = new Point(0, 0);
            tableLayoutPanelMain.Margin = new Padding(0);
            tableLayoutPanelMain.Name = "tableLayoutPanelMain";
            tableLayoutPanelMain.RowCount = 2;
            tableLayoutPanelMain.RowStyles.Add(new RowStyle());
            tableLayoutPanelMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanelMain.Size = new Size(1778, 944);
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
            tableLayoutPanelNavigation.Size = new Size(1772, 68);
            tableLayoutPanelNavigation.TabIndex = 0;
            // 
            // flowLayoutPanelNavigationButtons
            // 
            flowLayoutPanelNavigationButtons.AutoSize = true;
            flowLayoutPanelNavigationButtons.Controls.Add(buttonNavigationBack);
            flowLayoutPanelNavigationButtons.Controls.Add(buttonNavigationCreateFile);
            flowLayoutPanelNavigationButtons.Controls.Add(buttonNavigationCreateFolder);
            flowLayoutPanelNavigationButtons.Controls.Add(buttonNavigationDeleteSelected);
            flowLayoutPanelNavigationButtons.Controls.Add(buttonNavigationRoot);
            flowLayoutPanelNavigationButtons.Dock = DockStyle.Fill;
            flowLayoutPanelNavigationButtons.Location = new Point(3, 3);
            flowLayoutPanelNavigationButtons.Name = "flowLayoutPanelNavigationButtons";
            flowLayoutPanelNavigationButtons.Size = new Size(327, 62);
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
            buttonNavigationBack.Size = new Size(56, 56);
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
            buttonNavigationCreateFile.Location = new Point(65, 3);
            buttonNavigationCreateFile.MinimumSize = new Size(48, 48);
            buttonNavigationCreateFile.Name = "buttonNavigationCreateFile";
            buttonNavigationCreateFile.Size = new Size(56, 56);
            buttonNavigationCreateFile.TabIndex = 5;
            buttonNavigationCreateFile.TextImageRelation = TextImageRelation.ImageAboveText;
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
            buttonNavigationCreateFolder.Location = new Point(127, 3);
            buttonNavigationCreateFolder.MinimumSize = new Size(48, 48);
            buttonNavigationCreateFolder.Name = "buttonNavigationCreateFolder";
            buttonNavigationCreateFolder.Size = new Size(56, 56);
            buttonNavigationCreateFolder.TabIndex = 3;
            buttonNavigationCreateFolder.TextImageRelation = TextImageRelation.ImageAboveText;
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
            buttonNavigationDeleteSelected.Location = new Point(189, 3);
            buttonNavigationDeleteSelected.MinimumSize = new Size(48, 48);
            buttonNavigationDeleteSelected.Name = "buttonNavigationDeleteSelected";
            buttonNavigationDeleteSelected.Size = new Size(56, 56);
            buttonNavigationDeleteSelected.TabIndex = 4;
            buttonNavigationDeleteSelected.TextImageRelation = TextImageRelation.ImageAboveText;
            buttonNavigationDeleteSelected.UseVisualStyleBackColor = false;
            buttonNavigationDeleteSelected.Click += buttonNavigationDeleteSelected_Click;
            // 
            // buttonNavigationRoot
            // 
            buttonNavigationRoot.Anchor = AnchorStyles.Left;
            buttonNavigationRoot.AutoSize = true;
            buttonNavigationRoot.BackColor = Color.Transparent;
            buttonNavigationRoot.BackgroundImage = Properties.Resources.imageres_explorer;
            buttonNavigationRoot.BackgroundImageLayout = ImageLayout.Stretch;
            buttonNavigationRoot.Location = new Point(268, 3);
            buttonNavigationRoot.Margin = new Padding(20, 3, 3, 3);
            buttonNavigationRoot.MinimumSize = new Size(48, 48);
            buttonNavigationRoot.Name = "buttonNavigationRoot";
            buttonNavigationRoot.Size = new Size(56, 56);
            buttonNavigationRoot.TabIndex = 2;
            buttonNavigationRoot.TextImageRelation = TextImageRelation.ImageAboveText;
            buttonNavigationRoot.UseVisualStyleBackColor = false;
            buttonNavigationRoot.Click += buttonNavigationRoot_Click;
            // 
            // textBoxNavigation
            // 
            textBoxNavigation.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            textBoxNavigation.Location = new Point(353, 14);
            textBoxNavigation.Margin = new Padding(20, 3, 10, 3);
            textBoxNavigation.Name = "textBoxNavigation";
            textBoxNavigation.ReadOnly = true;
            textBoxNavigation.Size = new Size(1409, 39);
            textBoxNavigation.TabIndex = 2;
            // 
            // listViewMain
            // 
            listViewMain.Columns.AddRange(new ColumnHeader[] { columnHeaderName, columnHeaderSize, columnHeaderCreated });
            listViewMain.Dock = DockStyle.Fill;
            listViewMain.FullRowSelect = true;
            listViewMain.Location = new Point(3, 77);
            listViewMain.MultiSelect = false;
            listViewMain.Name = "listViewMain";
            listViewMain.Size = new Size(1772, 864);
            listViewMain.SmallImageList = imageListFileIcons;
            listViewMain.TabIndex = 1;
            listViewMain.UseCompatibleStateImageBehavior = false;
            listViewMain.View = View.Details;
            listViewMain.DoubleClick += listViewMain_DoubleClick;
            // 
            // columnHeaderName
            // 
            columnHeaderName.Text = "Name";
            columnHeaderName.Width = 500;
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
            // FormMain
            // 
            AutoScaleDimensions = new SizeF(13F, 32F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1778, 944);
            Controls.Add(tableLayoutPanelMain);
            Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(4);
            MinimumSize = new Size(1800, 1000);
            Name = "FormMain";
            Text = ".NET Thing Encryptor";
            WindowState = FormWindowState.Maximized;
            Load += FormMain_Load;
            tableLayoutPanelMain.ResumeLayout(false);
            tableLayoutPanelMain.PerformLayout();
            tableLayoutPanelNavigation.ResumeLayout(false);
            tableLayoutPanelNavigation.PerformLayout();
            flowLayoutPanelNavigationButtons.ResumeLayout(false);
            flowLayoutPanelNavigationButtons.PerformLayout();
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
        private ListView listViewMain;
        private ColumnHeader columnHeaderName;
        private ColumnHeader columnHeaderSize;
        private ColumnHeader columnHeaderCreated;
        private ImageList imageListFileIcons;
        private Button buttonNavigationCreateFile;
        private Button buttonNavigationCreateFolder;
        private Button buttonNavigationDeleteSelected;
    }
}
