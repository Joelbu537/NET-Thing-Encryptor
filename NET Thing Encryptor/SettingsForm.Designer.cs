namespace NET_Thing_Encryptor
{
    partial class SettingsForm
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
            tableLayoutPanel1 = new TableLayoutPanel();
            tableLayoutPanel = new TableLayoutPanel();
            buttonExportLocation = new Button();
            textBoxExportLocation = new TextBox();
            label2 = new Label();
            buttonImportLocation = new Button();
            textBoxImportLocation = new TextBox();
            label1 = new Label();
            label0 = new Label();
            textBoxSaveLocation = new TextBox();
            buttonSaveLocation = new Button();
            label3 = new Label();
            checkBoxDarkMode = new CheckBox();
            flowLayoutPanel1 = new FlowLayoutPanel();
            buttonApply = new Button();
            buttonCancel = new Button();
            tableLayoutPanel1.SuspendLayout();
            tableLayoutPanel.SuspendLayout();
            flowLayoutPanel1.SuspendLayout();
            SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 1;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Controls.Add(tableLayoutPanel, 0, 0);
            tableLayoutPanel1.Controls.Add(flowLayoutPanel1, 0, 1);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 2;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 47F));
            tableLayoutPanel1.Size = new Size(1080, 504);
            tableLayoutPanel1.TabIndex = 0;
            // 
            // tableLayoutPanel
            // 
            tableLayoutPanel.AutoSize = true;
            tableLayoutPanel.ColumnCount = 3;
            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel.Controls.Add(buttonExportLocation, 2, 2);
            tableLayoutPanel.Controls.Add(textBoxExportLocation, 1, 2);
            tableLayoutPanel.Controls.Add(label2, 0, 2);
            tableLayoutPanel.Controls.Add(buttonImportLocation, 2, 1);
            tableLayoutPanel.Controls.Add(textBoxImportLocation, 1, 1);
            tableLayoutPanel.Controls.Add(label1, 0, 1);
            tableLayoutPanel.Controls.Add(label0, 0, 0);
            tableLayoutPanel.Controls.Add(textBoxSaveLocation, 1, 0);
            tableLayoutPanel.Controls.Add(buttonSaveLocation, 2, 0);
            tableLayoutPanel.Controls.Add(label3, 0, 3);
            tableLayoutPanel.Controls.Add(checkBoxDarkMode, 1, 3);
            tableLayoutPanel.Dock = DockStyle.Fill;
            tableLayoutPanel.Location = new Point(11, 4);
            tableLayoutPanel.Margin = new Padding(11, 4, 4, 4);
            tableLayoutPanel.Name = "tableLayoutPanel";
            tableLayoutPanel.RowCount = 7;
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 47F));
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 47F));
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 47F));
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 47F));
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 47F));
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 47F));
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 47F));
            tableLayoutPanel.Size = new Size(1065, 449);
            tableLayoutPanel.TabIndex = 1;
            // 
            // buttonExportLocation
            // 
            buttonExportLocation.Dock = DockStyle.Fill;
            buttonExportLocation.Location = new Point(1025, 97);
            buttonExportLocation.Name = "buttonExportLocation";
            buttonExportLocation.Size = new Size(37, 41);
            buttonExportLocation.TabIndex = 9;
            buttonExportLocation.Text = "...";
            buttonExportLocation.UseVisualStyleBackColor = true;
            buttonExportLocation.Click += buttonExportLocation_Click;
            // 
            // textBoxExportLocation
            // 
            textBoxExportLocation.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            textBoxExportLocation.Location = new Point(212, 100);
            textBoxExportLocation.Name = "textBoxExportLocation";
            textBoxExportLocation.Size = new Size(807, 34);
            textBoxExportLocation.TabIndex = 8;
            // 
            // label2
            // 
            label2.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            label2.AutoSize = true;
            label2.Location = new Point(4, 103);
            label2.Margin = new Padding(4, 0, 4, 0);
            label2.Name = "label2";
            label2.Size = new Size(201, 28);
            label2.TabIndex = 7;
            label2.Text = "Default Export Folder";
            label2.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // buttonImportLocation
            // 
            buttonImportLocation.Dock = DockStyle.Fill;
            buttonImportLocation.Location = new Point(1025, 50);
            buttonImportLocation.Name = "buttonImportLocation";
            buttonImportLocation.Size = new Size(37, 41);
            buttonImportLocation.TabIndex = 6;
            buttonImportLocation.Text = "...";
            buttonImportLocation.UseVisualStyleBackColor = true;
            buttonImportLocation.Click += buttonImportLocation_Click;
            // 
            // textBoxImportLocation
            // 
            textBoxImportLocation.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            textBoxImportLocation.Location = new Point(212, 53);
            textBoxImportLocation.Name = "textBoxImportLocation";
            textBoxImportLocation.Size = new Size(807, 34);
            textBoxImportLocation.TabIndex = 5;
            // 
            // label1
            // 
            label1.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            label1.AutoSize = true;
            label1.Location = new Point(4, 56);
            label1.Margin = new Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new Size(201, 28);
            label1.TabIndex = 4;
            label1.Text = "Default Import Folder";
            label1.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // label0
            // 
            label0.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            label0.AutoSize = true;
            label0.Location = new Point(4, 9);
            label0.Margin = new Padding(4, 0, 4, 0);
            label0.Name = "label0";
            label0.Size = new Size(201, 28);
            label0.TabIndex = 1;
            label0.Text = "Save Data Location";
            label0.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // textBoxSaveLocation
            // 
            textBoxSaveLocation.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            textBoxSaveLocation.Location = new Point(212, 6);
            textBoxSaveLocation.Name = "textBoxSaveLocation";
            textBoxSaveLocation.Size = new Size(807, 34);
            textBoxSaveLocation.TabIndex = 2;
            // 
            // buttonSaveLocation
            // 
            buttonSaveLocation.Dock = DockStyle.Fill;
            buttonSaveLocation.Location = new Point(1025, 3);
            buttonSaveLocation.Name = "buttonSaveLocation";
            buttonSaveLocation.Size = new Size(37, 41);
            buttonSaveLocation.TabIndex = 3;
            buttonSaveLocation.Text = "...";
            buttonSaveLocation.UseVisualStyleBackColor = true;
            buttonSaveLocation.Click += buttonSaveLocation_Click;
            // 
            // label3
            // 
            label3.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            label3.AutoSize = true;
            label3.Location = new Point(4, 150);
            label3.Margin = new Padding(4, 0, 4, 0);
            label3.Name = "label3";
            label3.Size = new Size(201, 28);
            label3.TabIndex = 10;
            label3.Text = "Dark Mode?";
            label3.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // checkBoxDarkMode
            // 
            checkBoxDarkMode.AutoSize = true;
            checkBoxDarkMode.Dock = DockStyle.Fill;
            checkBoxDarkMode.Location = new Point(212, 144);
            checkBoxDarkMode.Name = "checkBoxDarkMode";
            checkBoxDarkMode.Size = new Size(807, 41);
            checkBoxDarkMode.TabIndex = 11;
            checkBoxDarkMode.Text = "Dark Mode! (Requires restart)";
            checkBoxDarkMode.UseVisualStyleBackColor = true;
            // 
            // flowLayoutPanel1
            // 
            flowLayoutPanel1.AutoSize = true;
            flowLayoutPanel1.Controls.Add(buttonApply);
            flowLayoutPanel1.Controls.Add(buttonCancel);
            flowLayoutPanel1.Dock = DockStyle.Fill;
            flowLayoutPanel1.FlowDirection = FlowDirection.RightToLeft;
            flowLayoutPanel1.Location = new Point(3, 460);
            flowLayoutPanel1.Name = "flowLayoutPanel1";
            flowLayoutPanel1.Size = new Size(1074, 41);
            flowLayoutPanel1.TabIndex = 2;
            // 
            // buttonApply
            // 
            buttonApply.AutoSize = true;
            buttonApply.Location = new Point(968, 3);
            buttonApply.Name = "buttonApply";
            buttonApply.Size = new Size(103, 38);
            buttonApply.TabIndex = 0;
            buttonApply.Text = "Apply";
            buttonApply.UseVisualStyleBackColor = true;
            buttonApply.Click += buttonApply_Click;
            // 
            // buttonCancel
            // 
            buttonCancel.AutoSize = true;
            buttonCancel.Location = new Point(859, 3);
            buttonCancel.Name = "buttonCancel";
            buttonCancel.Size = new Size(103, 38);
            buttonCancel.TabIndex = 1;
            buttonCancel.Text = "Cancel";
            buttonCancel.UseVisualStyleBackColor = true;
            buttonCancel.Click += buttonCancel_Click;
            // 
            // SettingsForm
            // 
            AcceptButton = buttonApply;
            AutoScaleDimensions = new SizeF(11F, 28F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoSize = true;
            CancelButton = buttonCancel;
            ClientSize = new Size(1080, 504);
            Controls.Add(tableLayoutPanel1);
            Font = new Font("Segoe UI", 10F);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Margin = new Padding(4);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "SettingsForm";
            ShowIcon = false;
            Text = "Settings";
            Load += SettingsForm_Load;
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            tableLayoutPanel.ResumeLayout(false);
            tableLayoutPanel.PerformLayout();
            flowLayoutPanel1.ResumeLayout(false);
            flowLayoutPanel1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel tableLayoutPanel1;
        private TableLayoutPanel tableLayoutPanel;
        private Label label0;
        private TextBox textBoxSaveLocation;
        private Button buttonSaveLocation;
        private FlowLayoutPanel flowLayoutPanel1;
        private Button buttonApply;
        private Button buttonCancel;
        private Label label1;
        private Button buttonExportLocation;
        private TextBox textBoxExportLocation;
        private Label label2;
        private Button buttonImportLocation;
        private TextBox textBoxImportLocation;
        private Label label3;
        private CheckBox checkBoxDarkMode;
    }
}