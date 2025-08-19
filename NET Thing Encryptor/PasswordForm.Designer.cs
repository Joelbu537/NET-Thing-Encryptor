namespace NET_Thing_Encryptor
{
    partial class PasswordForm
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
            tableLayoutPanelPassword = new TableLayoutPanel();
            textBoxPassword = new TextBox();
            labelPassword = new Label();
            buttonContinue = new Button();
            tableLayoutPanelPassword.SuspendLayout();
            SuspendLayout();
            // 
            // tableLayoutPanelPassword
            // 
            tableLayoutPanelPassword.ColumnCount = 1;
            tableLayoutPanelPassword.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanelPassword.Controls.Add(textBoxPassword, 0, 1);
            tableLayoutPanelPassword.Controls.Add(labelPassword, 0, 0);
            tableLayoutPanelPassword.Controls.Add(buttonContinue, 0, 2);
            tableLayoutPanelPassword.Dock = DockStyle.Fill;
            tableLayoutPanelPassword.Location = new Point(0, 0);
            tableLayoutPanelPassword.Name = "tableLayoutPanelPassword";
            tableLayoutPanelPassword.RowCount = 3;
            tableLayoutPanelPassword.RowStyles.Add(new RowStyle());
            tableLayoutPanelPassword.RowStyles.Add(new RowStyle());
            tableLayoutPanelPassword.RowStyles.Add(new RowStyle());
            tableLayoutPanelPassword.Size = new Size(1040, 172);
            tableLayoutPanelPassword.TabIndex = 0;
            // 
            // textBoxPassword
            // 
            textBoxPassword.BackColor = Color.Silver;
            textBoxPassword.Dock = DockStyle.Fill;
            textBoxPassword.Location = new Point(30, 68);
            textBoxPassword.Margin = new Padding(30, 30, 30, 10);
            textBoxPassword.MaxLength = 256;
            textBoxPassword.Name = "textBoxPassword";
            textBoxPassword.PasswordChar = '*';
            textBoxPassword.ShortcutsEnabled = false;
            textBoxPassword.Size = new Size(980, 39);
            textBoxPassword.TabIndex = 0;
            textBoxPassword.TextChanged += textBoxPassword_TextChanged;
            // 
            // labelPassword
            // 
            labelPassword.AutoSize = true;
            labelPassword.Dock = DockStyle.Top;
            labelPassword.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            labelPassword.Location = new Point(3, 3);
            labelPassword.Margin = new Padding(3);
            labelPassword.Name = "labelPassword";
            labelPassword.RightToLeft = RightToLeft.No;
            labelPassword.Size = new Size(1034, 32);
            labelPassword.TabIndex = 1;
            labelPassword.Text = "Enter your password to decrypt your files\r\n";
            labelPassword.TextAlign = ContentAlignment.TopCenter;
            // 
            // buttonContinue
            // 
            buttonContinue.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonContinue.AutoSize = true;
            buttonContinue.Enabled = false;
            buttonContinue.Location = new Point(912, 123);
            buttonContinue.Margin = new Padding(6);
            buttonContinue.Name = "buttonContinue";
            buttonContinue.Size = new Size(122, 42);
            buttonContinue.TabIndex = 2;
            buttonContinue.TabStop = false;
            buttonContinue.Text = "Continue";
            buttonContinue.UseVisualStyleBackColor = true;
            buttonContinue.Click += buttonContinue_Click;
            // 
            // PasswordForm
            // 
            AcceptButton = buttonContinue;
            AutoScaleDimensions = new SizeF(13F, 32F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1040, 172);
            ControlBox = false;
            Controls.Add(tableLayoutPanelPassword);
            Font = new Font("Segoe UI", 12F);
            FormBorderStyle = FormBorderStyle.None;
            Margin = new Padding(4);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "PasswordForm";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "PasswordForm";
            TopMost = true;
            tableLayoutPanelPassword.ResumeLayout(false);
            tableLayoutPanelPassword.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel tableLayoutPanelPassword;
        private TextBox textBoxPassword;
        private Label labelPassword;
        private Button buttonContinue;
    }
}