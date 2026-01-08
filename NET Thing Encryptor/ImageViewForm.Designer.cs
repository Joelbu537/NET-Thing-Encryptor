namespace NET_Thing_Encryptor
{
    partial class ImageViewForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ImageViewForm));
            pictureBox = new PictureBox();
            textBoxIndex = new TextBox();
            ((System.ComponentModel.ISupportInitialize)pictureBox).BeginInit();
            SuspendLayout();
            // 
            // pictureBox
            // 
            pictureBox.BackColor = Color.Transparent;
            pictureBox.Dock = DockStyle.Fill;
            pictureBox.ErrorImage = Properties.Resources.fileError;
            pictureBox.InitialImage = Properties.Resources.loading;
            pictureBox.Location = new Point(0, 0);
            pictureBox.Name = "pictureBox";
            pictureBox.Size = new Size(2231, 1274);
            pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox.TabIndex = 0;
            pictureBox.TabStop = false;
            pictureBox.MouseClick += pictureBox_MouseClick;
            // 
            // textBoxIndex
            // 
            textBoxIndex.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            textBoxIndex.BackColor = Color.FromArgb(31, 31, 31);
            textBoxIndex.BorderStyle = BorderStyle.None;
            textBoxIndex.Font = new Font("Segoe UI", 15F);
            textBoxIndex.ForeColor = Color.White;
            textBoxIndex.Location = new Point(2062, 1215);
            textBoxIndex.Margin = new Padding(10);
            textBoxIndex.MaxLength = 16;
            textBoxIndex.Name = "textBoxIndex";
            textBoxIndex.Size = new Size(150, 40);
            textBoxIndex.TabIndex = 0;
            textBoxIndex.TabStop = false;
            textBoxIndex.Text = "0/0";
            textBoxIndex.TextAlign = HorizontalAlignment.Right;
            textBoxIndex.Enter += textBoxIndex_Enter;
            textBoxIndex.Leave += textBoxIndex_Leave;
            // 
            // ImageViewForm
            // 
            AutoScaleDimensions = new SizeF(13F, 32F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(31, 31, 31);
            ClientSize = new Size(2231, 1274);
            Controls.Add(textBoxIndex);
            Controls.Add(pictureBox);
            Font = new Font("Segoe UI", 12F);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(4);
            Name = "ImageViewForm";
            ShowIcon = false;
            Text = ".NET Thing Image Viewer";
            WindowState = FormWindowState.Maximized;
            FormClosing += ImageViewForm_FormClosing;
            KeyDown += ImageViewForm_KeyDown;
            ((System.ComponentModel.ISupportInitialize)pictureBox).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private PictureBox pictureBox;
        private TextBox textBoxIndex;
    }
}