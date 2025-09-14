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
            labelSaveLocation = new Label();
            textBoxSaveLocation = new TextBox();
            buttonSaveLocation = new Button();
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
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            tableLayoutPanel1.Size = new Size(1178, 540);
            tableLayoutPanel1.TabIndex = 0;
            // 
            // tableLayoutPanel
            // 
            tableLayoutPanel.AutoSize = true;
            tableLayoutPanel.ColumnCount = 3;
            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel.Controls.Add(labelSaveLocation, 0, 0);
            tableLayoutPanel.Controls.Add(textBoxSaveLocation, 1, 0);
            tableLayoutPanel.Controls.Add(buttonSaveLocation, 2, 0);
            tableLayoutPanel.Dock = DockStyle.Fill;
            tableLayoutPanel.Location = new Point(12, 4);
            tableLayoutPanel.Margin = new Padding(12, 4, 4, 4);
            tableLayoutPanel.Name = "tableLayoutPanel";
            tableLayoutPanel.RowCount = 7;
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            tableLayoutPanel.Size = new Size(1162, 482);
            tableLayoutPanel.TabIndex = 1;
            // 
            // labelSaveLocation
            // 
            labelSaveLocation.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            labelSaveLocation.AutoSize = true;
            labelSaveLocation.Location = new Point(4, 10);
            labelSaveLocation.Margin = new Padding(4, 0, 4, 0);
            labelSaveLocation.Name = "labelSaveLocation";
            labelSaveLocation.Size = new Size(146, 30);
            labelSaveLocation.TabIndex = 1;
            labelSaveLocation.Text = "Save Location";
            labelSaveLocation.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // textBoxSaveLocation
            // 
            textBoxSaveLocation.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            textBoxSaveLocation.Location = new Point(157, 6);
            textBoxSaveLocation.Name = "textBoxSaveLocation";
            textBoxSaveLocation.Size = new Size(956, 37);
            textBoxSaveLocation.TabIndex = 2;
            // 
            // buttonSaveLocation
            // 
            buttonSaveLocation.Dock = DockStyle.Fill;
            buttonSaveLocation.Location = new Point(1119, 3);
            buttonSaveLocation.Name = "buttonSaveLocation";
            buttonSaveLocation.Size = new Size(40, 44);
            buttonSaveLocation.TabIndex = 3;
            buttonSaveLocation.Text = "...";
            buttonSaveLocation.UseVisualStyleBackColor = true;
            buttonSaveLocation.Click += buttonSaveLocation_Click;
            // 
            // flowLayoutPanel1
            // 
            flowLayoutPanel1.AutoSize = true;
            flowLayoutPanel1.Controls.Add(buttonApply);
            flowLayoutPanel1.Controls.Add(buttonCancel);
            flowLayoutPanel1.Dock = DockStyle.Fill;
            flowLayoutPanel1.FlowDirection = FlowDirection.RightToLeft;
            flowLayoutPanel1.Location = new Point(3, 493);
            flowLayoutPanel1.Name = "flowLayoutPanel1";
            flowLayoutPanel1.Size = new Size(1172, 44);
            flowLayoutPanel1.TabIndex = 2;
            // 
            // buttonApply
            // 
            buttonApply.AutoSize = true;
            buttonApply.Location = new Point(1057, 3);
            buttonApply.Name = "buttonApply";
            buttonApply.Size = new Size(112, 40);
            buttonApply.TabIndex = 0;
            buttonApply.Text = "Apply";
            buttonApply.UseVisualStyleBackColor = true;
            buttonApply.Click += buttonApply_Click;
            // 
            // buttonCancel
            // 
            buttonCancel.AutoSize = true;
            buttonCancel.Location = new Point(939, 3);
            buttonCancel.Name = "buttonCancel";
            buttonCancel.Size = new Size(112, 40);
            buttonCancel.TabIndex = 1;
            buttonCancel.Text = "Cancel";
            buttonCancel.UseVisualStyleBackColor = true;
            buttonCancel.Click += buttonCancel_Click;
            // 
            // SettingsForm
            // 
            AcceptButton = buttonApply;
            AutoScaleDimensions = new SizeF(12F, 30F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoSize = true;
            CancelButton = buttonCancel;
            ClientSize = new Size(1178, 540);
            Controls.Add(tableLayoutPanel1);
            Font = new Font("Segoe UI", 11F);
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
        private Label labelSaveLocation;
        private TextBox textBoxSaveLocation;
        private Button buttonSaveLocation;
        private FlowLayoutPanel flowLayoutPanel1;
        private Button buttonApply;
        private Button buttonCancel;
    }
}