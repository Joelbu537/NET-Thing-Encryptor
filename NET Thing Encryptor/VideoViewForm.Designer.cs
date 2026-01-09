namespace NET_Thing_Encryptor
{
    partial class VideoViewForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(VideoViewForm));
            videoView = new LibVLCSharp.WinForms.VideoView();
            tableLayoutPanel = new TableLayoutPanel();
            tableLayoutPanel1 = new TableLayoutPanel();
            trackBar = new TrackBar();
            buttonPause = new Button();
            ((System.ComponentModel.ISupportInitialize)videoView).BeginInit();
            tableLayoutPanel.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)trackBar).BeginInit();
            SuspendLayout();
            // 
            // videoView
            // 
            videoView.BackColor = Color.Black;
            videoView.Dock = DockStyle.Fill;
            videoView.Location = new Point(3, 3);
            videoView.MediaPlayer = null;
            videoView.Name = "videoView";
            videoView.Size = new Size(1892, 958);
            videoView.TabIndex = 0;
            // 
            // tableLayoutPanel
            // 
            tableLayoutPanel.ColumnCount = 1;
            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel.Controls.Add(videoView, 0, 0);
            tableLayoutPanel.Controls.Add(tableLayoutPanel1, 0, 1);
            tableLayoutPanel.Dock = DockStyle.Fill;
            tableLayoutPanel.Location = new Point(0, 0);
            tableLayoutPanel.Name = "tableLayoutPanel";
            tableLayoutPanel.RowCount = 2;
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));
            tableLayoutPanel.Size = new Size(1898, 1024);
            tableLayoutPanel.TabIndex = 1;
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 3;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            tableLayoutPanel1.Controls.Add(trackBar, 1, 0);
            tableLayoutPanel1.Controls.Add(buttonPause, 0, 0);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(3, 967);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 1;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Size = new Size(1892, 54);
            tableLayoutPanel1.TabIndex = 1;
            // 
            // trackBar
            // 
            trackBar.Dock = DockStyle.Fill;
            trackBar.Location = new Point(57, 3);
            trackBar.Maximum = 1000;
            trackBar.Name = "trackBar";
            trackBar.Size = new Size(1812, 48);
            trackBar.TabIndex = 0;
            trackBar.TickStyle = TickStyle.None;
            trackBar.MouseDown += trackBar_MouseDown;
            trackBar.MouseUp += trackBar_MouseUp;
            // 
            // buttonPause
            // 
            buttonPause.AutoSize = true;
            buttonPause.BackgroundImage = Properties.Resources.pause_icon;
            buttonPause.BackgroundImageLayout = ImageLayout.Stretch;
            buttonPause.Dock = DockStyle.Fill;
            buttonPause.Location = new Point(3, 3);
            buttonPause.Name = "buttonPause";
            buttonPause.Size = new Size(48, 48);
            buttonPause.TabIndex = 1;
            buttonPause.UseVisualStyleBackColor = true;
            buttonPause.Click += buttonPause_Click;
            // 
            // VideoViewForm
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1898, 1024);
            Controls.Add(tableLayoutPanel);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "VideoViewForm";
            ShowIcon = false;
            Text = ".NET Thing Video Viewer";
            WindowState = FormWindowState.Maximized;
            FormClosing += VideoViewForm_FormClosing;
            Load += VideoViewForm_Load;
            ((System.ComponentModel.ISupportInitialize)videoView).EndInit();
            tableLayoutPanel.ResumeLayout(false);
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)trackBar).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private LibVLCSharp.WinForms.VideoView videoView;
        private TableLayoutPanel tableLayoutPanel;
        private TableLayoutPanel tableLayoutPanel1;
        private TrackBar trackBar;
        private Button buttonPause;
    }
}