namespace NET_Thing_Encryptor
{
    partial class VideoViewForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new(typeof(VideoViewForm));
            rootLayout = new TableLayoutPanel();
            videoView = new LibVLCSharp.WinForms.VideoView();
            controlLayout = new TableLayoutPanel();
            labelCurrentTime = new Label();
            timeline = new VideoTimeline();
            labelDuration = new Label();
            transportLayout = new TableLayoutPanel();
            buttonSeekBackward = new Button();
            buttonPlayPause = new Button();
            buttonSeekForward = new Button();
            toolTip = new ToolTip(components);
            rootLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)videoView).BeginInit();
            controlLayout.SuspendLayout();
            transportLayout.SuspendLayout();
            SuspendLayout();
            //
            // rootLayout
            //
            rootLayout.BackColor = Color.Black;
            rootLayout.ColumnCount = 1;
            rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rootLayout.Controls.Add(videoView, 0, 0);
            rootLayout.Controls.Add(controlLayout, 0, 1);
            rootLayout.Dock = DockStyle.Fill;
            rootLayout.Location = new Point(0, 0);
            rootLayout.Margin = new Padding(0);
            rootLayout.Name = "rootLayout";
            rootLayout.RowCount = 2;
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 92F));
            rootLayout.Size = new Size(1280, 720);
            rootLayout.TabIndex = 0;
            //
            // videoView
            //
            videoView.BackColor = Color.Black;
            videoView.Dock = DockStyle.Fill;
            videoView.Location = new Point(0, 0);
            videoView.Margin = new Padding(0);
            videoView.MediaPlayer = null;
            videoView.Name = "videoView";
            videoView.Size = new Size(1280, 628);
            videoView.TabIndex = 0;
            //
            // controlLayout
            //
            controlLayout.BackColor = Color.FromArgb(24, 24, 24);
            controlLayout.ColumnCount = 3;
            controlLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 84F));
            controlLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            controlLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 84F));
            controlLayout.Controls.Add(labelCurrentTime, 0, 0);
            controlLayout.Controls.Add(timeline, 1, 0);
            controlLayout.Controls.Add(labelDuration, 2, 0);
            controlLayout.Controls.Add(transportLayout, 0, 1);
            controlLayout.Dock = DockStyle.Fill;
            controlLayout.Location = new Point(0, 628);
            controlLayout.Margin = new Padding(0);
            controlLayout.Name = "controlLayout";
            controlLayout.RowCount = 2;
            controlLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            controlLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            controlLayout.SetColumnSpan(transportLayout, 3);
            controlLayout.Size = new Size(1280, 92);
            controlLayout.TabIndex = 1;
            //
            // labelCurrentTime
            //
            labelCurrentTime.Dock = DockStyle.Fill;
            labelCurrentTime.Font = new Font("Segoe UI", 9.5F);
            labelCurrentTime.ForeColor = Color.White;
            labelCurrentTime.Location = new Point(8, 0);
            labelCurrentTime.Margin = new Padding(8, 0, 0, 0);
            labelCurrentTime.Name = "labelCurrentTime";
            labelCurrentTime.Size = new Size(76, 42);
            labelCurrentTime.TabIndex = 0;
            labelCurrentTime.Text = "00:00";
            labelCurrentTime.TextAlign = ContentAlignment.MiddleLeft;
            //
            // timeline
            //
            timeline.BackColor = Color.FromArgb(24, 24, 24);
            timeline.Cursor = Cursors.Hand;
            timeline.Dock = DockStyle.Fill;
            timeline.Location = new Point(84, 5);
            timeline.Margin = new Padding(0, 5, 0, 5);
            timeline.MinimumSize = new Size(120, 28);
            timeline.Name = "timeline";
            timeline.Size = new Size(1112, 32);
            timeline.TabIndex = 1;
            //
            // labelDuration
            //
            labelDuration.Dock = DockStyle.Fill;
            labelDuration.Font = new Font("Segoe UI", 9.5F);
            labelDuration.ForeColor = Color.FromArgb(190, 190, 190);
            labelDuration.Location = new Point(1196, 0);
            labelDuration.Margin = new Padding(0, 0, 8, 0);
            labelDuration.Name = "labelDuration";
            labelDuration.Size = new Size(76, 42);
            labelDuration.TabIndex = 2;
            labelDuration.Text = "00:00";
            labelDuration.TextAlign = ContentAlignment.MiddleRight;
            //
            // transportLayout
            //
            transportLayout.ColumnCount = 5;
            transportLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            transportLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 66F));
            transportLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 54F));
            transportLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 66F));
            transportLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            transportLayout.Controls.Add(buttonSeekBackward, 1, 0);
            transportLayout.Controls.Add(buttonPlayPause, 2, 0);
            transportLayout.Controls.Add(buttonSeekForward, 3, 0);
            transportLayout.Dock = DockStyle.Fill;
            transportLayout.Location = new Point(0, 42);
            transportLayout.Margin = new Padding(0);
            transportLayout.Name = "transportLayout";
            transportLayout.RowCount = 1;
            transportLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            transportLayout.Size = new Size(1280, 50);
            transportLayout.TabIndex = 3;
            //
            // buttonSeekBackward
            //
            buttonSeekBackward.BackColor = Color.FromArgb(45, 45, 45);
            buttonSeekBackward.Dock = DockStyle.Fill;
            buttonSeekBackward.FlatAppearance.BorderSize = 0;
            buttonSeekBackward.FlatAppearance.MouseDownBackColor = Color.FromArgb(70, 70, 70);
            buttonSeekBackward.FlatAppearance.MouseOverBackColor = Color.FromArgb(58, 58, 58);
            buttonSeekBackward.FlatStyle = FlatStyle.Flat;
            buttonSeekBackward.Font = new Font("Segoe UI Semibold", 9F);
            buttonSeekBackward.ForeColor = Color.White;
            buttonSeekBackward.Location = new Point(550, 5);
            buttonSeekBackward.Margin = new Padding(3, 5, 3, 5);
            buttonSeekBackward.Name = "buttonSeekBackward";
            buttonSeekBackward.Size = new Size(60, 40);
            buttonSeekBackward.TabIndex = 0;
            buttonSeekBackward.Text = "−10s";
            buttonSeekBackward.UseVisualStyleBackColor = false;
            buttonSeekBackward.Click += buttonSeekBackward_Click;
            //
            // buttonPlayPause
            //
            buttonPlayPause.BackColor = Color.FromArgb(45, 45, 45);
            buttonPlayPause.BackgroundImage = Properties.Resources.pause_icon;
            buttonPlayPause.BackgroundImageLayout = ImageLayout.Zoom;
            buttonPlayPause.Dock = DockStyle.Fill;
            buttonPlayPause.FlatAppearance.BorderSize = 0;
            buttonPlayPause.FlatAppearance.MouseDownBackColor = Color.FromArgb(70, 70, 70);
            buttonPlayPause.FlatAppearance.MouseOverBackColor = Color.FromArgb(58, 58, 58);
            buttonPlayPause.FlatStyle = FlatStyle.Flat;
            buttonPlayPause.Location = new Point(616, 3);
            buttonPlayPause.Margin = new Padding(3);
            buttonPlayPause.Name = "buttonPlayPause";
            buttonPlayPause.Padding = new Padding(9);
            buttonPlayPause.Size = new Size(48, 44);
            buttonPlayPause.TabIndex = 1;
            buttonPlayPause.UseVisualStyleBackColor = false;
            buttonPlayPause.Click += buttonPlayPause_Click;
            //
            // buttonSeekForward
            //
            buttonSeekForward.BackColor = Color.FromArgb(45, 45, 45);
            buttonSeekForward.Dock = DockStyle.Fill;
            buttonSeekForward.FlatAppearance.BorderSize = 0;
            buttonSeekForward.FlatAppearance.MouseDownBackColor = Color.FromArgb(70, 70, 70);
            buttonSeekForward.FlatAppearance.MouseOverBackColor = Color.FromArgb(58, 58, 58);
            buttonSeekForward.FlatStyle = FlatStyle.Flat;
            buttonSeekForward.Font = new Font("Segoe UI Semibold", 9F);
            buttonSeekForward.ForeColor = Color.White;
            buttonSeekForward.Location = new Point(670, 5);
            buttonSeekForward.Margin = new Padding(3, 5, 3, 5);
            buttonSeekForward.Name = "buttonSeekForward";
            buttonSeekForward.Size = new Size(60, 40);
            buttonSeekForward.TabIndex = 2;
            buttonSeekForward.Text = "+10s";
            buttonSeekForward.UseVisualStyleBackColor = false;
            buttonSeekForward.Click += buttonSeekForward_Click;
            //
            // VideoViewForm
            //
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Black;
            ClientSize = new Size(1280, 720);
            Controls.Add(rootLayout);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MinimumSize = new Size(640, 360);
            Name = "VideoViewForm";
            ShowIcon = false;
            Text = ".NET Thing Video Viewer";
            WindowState = FormWindowState.Maximized;
            FormClosing += VideoViewForm_FormClosing;
            Load += VideoViewForm_Load;
            KeyDown += VideoViewForm_KeyDown;
            rootLayout.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)videoView).EndInit();
            controlLayout.ResumeLayout(false);
            transportLayout.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel rootLayout;
        private LibVLCSharp.WinForms.VideoView videoView;
        private TableLayoutPanel controlLayout;
        private Label labelCurrentTime;
        private VideoTimeline timeline;
        private Label labelDuration;
        private TableLayoutPanel transportLayout;
        private Button buttonSeekBackward;
        private Button buttonPlayPause;
        private Button buttonSeekForward;
        private ToolTip toolTip;
    }
}
