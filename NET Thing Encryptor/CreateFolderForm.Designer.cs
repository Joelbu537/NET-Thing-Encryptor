namespace NET_Thing_Encryptor
{
    partial class CreateFolderForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CreateFolderForm));
            tableLayoutPanel = new TableLayoutPanel();
            label = new Label();
            textBox = new TextBox();
            tableLayoutPanelButtons = new TableLayoutPanel();
            buttonCancel = new Button();
            buttonOK = new Button();
            tableLayoutPanel.SuspendLayout();
            tableLayoutPanelButtons.SuspendLayout();
            SuspendLayout();
            // 
            // tableLayoutPanel
            // 
            tableLayoutPanel.AutoSize = true;
            tableLayoutPanel.ColumnCount = 1;
            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel.Controls.Add(label, 0, 0);
            tableLayoutPanel.Controls.Add(textBox, 0, 1);
            tableLayoutPanel.Controls.Add(tableLayoutPanelButtons, 0, 2);
            tableLayoutPanel.Dock = DockStyle.Fill;
            tableLayoutPanel.Location = new Point(0, 0);
            tableLayoutPanel.Margin = new Padding(4);
            tableLayoutPanel.Name = "tableLayoutPanel";
            tableLayoutPanel.RowCount = 3;
            tableLayoutPanel.RowStyles.Add(new RowStyle());
            tableLayoutPanel.RowStyles.Add(new RowStyle());
            tableLayoutPanel.RowStyles.Add(new RowStyle());
            tableLayoutPanel.Size = new Size(1040, 204);
            tableLayoutPanel.TabIndex = 0;
            // 
            // label
            // 
            label.AutoSize = true;
            label.Dock = DockStyle.Fill;
            label.Location = new Point(3, 30);
            label.Margin = new Padding(3, 30, 3, 0);
            label.Name = "label";
            label.Size = new Size(1034, 32);
            label.TabIndex = 0;
            label.Text = "Enter name of new folder to create";
            label.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // textBox
            // 
            textBox.Dock = DockStyle.Fill;
            textBox.Location = new Point(50, 72);
            textBox.Margin = new Padding(50, 10, 50, 3);
            textBox.Name = "textBox";
            textBox.Size = new Size(940, 39);
            textBox.TabIndex = 1;
            textBox.TextChanged += textBox1_TextChanged;
            // 
            // tableLayoutPanelButtons
            // 
            tableLayoutPanelButtons.AutoSize = true;
            tableLayoutPanelButtons.ColumnCount = 2;
            tableLayoutPanelButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanelButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanelButtons.Controls.Add(buttonCancel, 0, 0);
            tableLayoutPanelButtons.Controls.Add(buttonOK, 1, 0);
            tableLayoutPanelButtons.Dock = DockStyle.Fill;
            tableLayoutPanelButtons.Location = new Point(3, 134);
            tableLayoutPanelButtons.Margin = new Padding(3, 20, 3, 3);
            tableLayoutPanelButtons.Name = "tableLayoutPanelButtons";
            tableLayoutPanelButtons.RowCount = 1;
            tableLayoutPanelButtons.RowStyles.Add(new RowStyle());
            tableLayoutPanelButtons.Size = new Size(1034, 67);
            tableLayoutPanelButtons.TabIndex = 2;
            // 
            // buttonCancel
            // 
            buttonCancel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonCancel.AutoSize = true;
            buttonCancel.Location = new Point(400, 3);
            buttonCancel.Margin = new Padding(3, 3, 5, 3);
            buttonCancel.Name = "buttonCancel";
            buttonCancel.Size = new Size(112, 42);
            buttonCancel.TabIndex = 0;
            buttonCancel.Text = "Cancel";
            buttonCancel.UseVisualStyleBackColor = true;
            buttonCancel.Click += buttonCancel_Click;
            // 
            // buttonOK
            // 
            buttonOK.AutoSize = true;
            buttonOK.Enabled = false;
            buttonOK.Location = new Point(522, 3);
            buttonOK.Margin = new Padding(5, 3, 3, 3);
            buttonOK.Name = "buttonOK";
            buttonOK.Size = new Size(112, 42);
            buttonOK.TabIndex = 1;
            buttonOK.Text = "OK";
            buttonOK.UseVisualStyleBackColor = true;
            buttonOK.Click += buttonOK_Click;
            // 
            // CreateFolderForm
            // 
            AcceptButton = buttonOK;
            AutoScaleDimensions = new SizeF(13F, 32F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoSize = true;
            CancelButton = buttonCancel;
            ClientSize = new Size(1040, 204);
            Controls.Add(tableLayoutPanel);
            Font = new Font("Segoe UI", 12F);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(4);
            Name = "CreateFolderForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Create Folder";
            tableLayoutPanel.ResumeLayout(false);
            tableLayoutPanel.PerformLayout();
            tableLayoutPanelButtons.ResumeLayout(false);
            tableLayoutPanelButtons.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TableLayoutPanel tableLayoutPanel;
        private Label label;
        private TextBox textBox;
        private TableLayoutPanel tableLayoutPanelButtons;
        private Button buttonCancel;
        private Button buttonOK;
    }
}