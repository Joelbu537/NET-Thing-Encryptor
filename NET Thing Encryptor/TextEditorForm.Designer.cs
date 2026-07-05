namespace NET_Thing_Encryptor
{
    partial class TextEditorForm
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
            toolStrip = new ToolStrip();
            toolStripButtonReadOnly = new ToolStripButton();
            toolStripSeparatorReadOnly = new ToolStripSeparator();
            toolStripButtonSave = new ToolStripButton();
            toolStripSeparator1 = new ToolStripSeparator();
            toolStripButtonUndo = new ToolStripButton();
            toolStripButtonRedo = new ToolStripButton();
            toolStripSeparator2 = new ToolStripSeparator();
            toolStripLabelFont = new ToolStripLabel();
            toolStripComboBoxFontFamily = new ToolStripComboBox();
            toolStripLabelFontSize = new ToolStripLabel();
            toolStripComboBoxFontSize = new ToolStripComboBox();
            toolStripSeparator3 = new ToolStripSeparator();
            toolStripLabelSearch = new ToolStripLabel();
            toolStripTextBoxSearch = new ToolStripTextBox();
            toolStripButtonFindNext = new ToolStripButton();
            editor = new RichTextBox();
            statusStrip = new StatusStrip();
            statusLabelState = new ToolStripStatusLabel();
            statusLabelPosition = new ToolStripStatusLabel();
            statusLabelCharacters = new ToolStripStatusLabel();
            statusLabelEncoding = new ToolStripStatusLabel();
            toolStrip.SuspendLayout();
            statusStrip.SuspendLayout();
            SuspendLayout();
            //
            // toolStrip
            //
            toolStrip.BackColor = Color.FromArgb(38, 38, 38);
            toolStrip.GripStyle = ToolStripGripStyle.Hidden;
            toolStrip.ImageScalingSize = new Size(20, 20);
            toolStrip.Items.AddRange(new ToolStripItem[] {
                toolStripButtonReadOnly,
                toolStripSeparatorReadOnly,
                toolStripButtonSave,
                toolStripSeparator1,
                toolStripButtonUndo,
                toolStripButtonRedo,
                toolStripSeparator2,
                toolStripLabelFont,
                toolStripComboBoxFontFamily,
                toolStripLabelFontSize,
                toolStripComboBoxFontSize,
                toolStripSeparator3,
                toolStripLabelSearch,
                toolStripTextBoxSearch,
                toolStripButtonFindNext
            });
            toolStrip.Location = new Point(0, 0);
            toolStrip.Name = "toolStrip";
            toolStrip.Padding = new Padding(8, 5, 8, 5);
            toolStrip.Size = new Size(1200, 40);
            toolStrip.TabIndex = 0;
            //
            // toolStripButtonReadOnly
            //
            toolStripButtonReadOnly.Checked = true;
            toolStripButtonReadOnly.CheckOnClick = true;
            toolStripButtonReadOnly.CheckState = CheckState.Checked;
            toolStripButtonReadOnly.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripButtonReadOnly.ForeColor = Color.FromArgb(255, 205, 120);
            toolStripButtonReadOnly.Name = "toolStripButtonReadOnly";
            toolStripButtonReadOnly.Size = new Size(86, 27);
            toolStripButtonReadOnly.Text = "Read only";
            toolStripButtonReadOnly.ToolTipText = "Toggle editing";
            toolStripButtonReadOnly.Click += toolStripButtonReadOnly_Click;
            //
            // toolStripButtonSave
            //
            toolStripButtonSave.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripButtonSave.ForeColor = Color.White;
            toolStripButtonSave.Name = "toolStripButtonSave";
            toolStripButtonSave.Size = new Size(72, 27);
            toolStripButtonSave.Text = "Save";
            toolStripButtonSave.ToolTipText = "Save (Ctrl+S)";
            toolStripButtonSave.Click += toolStripButtonSave_Click;
            //
            // toolStripButtonUndo
            //
            toolStripButtonUndo.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripButtonUndo.ForeColor = Color.White;
            toolStripButtonUndo.Name = "toolStripButtonUndo";
            toolStripButtonUndo.Size = new Size(58, 27);
            toolStripButtonUndo.Text = "Undo";
            toolStripButtonUndo.Click += toolStripButtonUndo_Click;
            //
            // toolStripButtonRedo
            //
            toolStripButtonRedo.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripButtonRedo.ForeColor = Color.White;
            toolStripButtonRedo.Name = "toolStripButtonRedo";
            toolStripButtonRedo.Size = new Size(57, 27);
            toolStripButtonRedo.Text = "Redo";
            toolStripButtonRedo.Click += toolStripButtonRedo_Click;
            //
            // toolStripLabelFont
            //
            toolStripLabelFont.ForeColor = Color.FromArgb(210, 210, 210);
            toolStripLabelFont.Name = "toolStripLabelFont";
            toolStripLabelFont.Size = new Size(40, 27);
            toolStripLabelFont.Text = "Font";
            //
            // toolStripComboBoxFontFamily
            //
            toolStripComboBoxFontFamily.BackColor = Color.FromArgb(55, 55, 55);
            toolStripComboBoxFontFamily.DropDownStyle = ComboBoxStyle.DropDownList;
            toolStripComboBoxFontFamily.FlatStyle = FlatStyle.Flat;
            toolStripComboBoxFontFamily.ForeColor = Color.White;
            toolStripComboBoxFontFamily.Name = "toolStripComboBoxFontFamily";
            toolStripComboBoxFontFamily.Size = new Size(150, 30);
            toolStripComboBoxFontFamily.SelectedIndexChanged += toolStripComboBoxFont_SelectedIndexChanged;
            //
            // toolStripLabelFontSize
            //
            toolStripLabelFontSize.ForeColor = Color.FromArgb(210, 210, 210);
            toolStripLabelFontSize.Name = "toolStripLabelFontSize";
            toolStripLabelFontSize.Size = new Size(36, 27);
            toolStripLabelFontSize.Text = "Size";
            //
            // toolStripComboBoxFontSize
            //
            toolStripComboBoxFontSize.BackColor = Color.FromArgb(55, 55, 55);
            toolStripComboBoxFontSize.FlatStyle = FlatStyle.Flat;
            toolStripComboBoxFontSize.ForeColor = Color.White;
            toolStripComboBoxFontSize.Name = "toolStripComboBoxFontSize";
            toolStripComboBoxFontSize.Size = new Size(62, 30);
            toolStripComboBoxFontSize.SelectedIndexChanged += toolStripComboBoxFont_SelectedIndexChanged;
            toolStripComboBoxFontSize.KeyDown += toolStripComboBoxFontSize_KeyDown;
            //
            // toolStripLabelSearch
            //
            toolStripLabelSearch.ForeColor = Color.FromArgb(210, 210, 210);
            toolStripLabelSearch.Name = "toolStripLabelSearch";
            toolStripLabelSearch.Size = new Size(44, 27);
            toolStripLabelSearch.Text = "Find";
            //
            // toolStripTextBoxSearch
            //
            toolStripTextBoxSearch.BackColor = Color.FromArgb(55, 55, 55);
            toolStripTextBoxSearch.BorderStyle = BorderStyle.FixedSingle;
            toolStripTextBoxSearch.ForeColor = Color.White;
            toolStripTextBoxSearch.Name = "toolStripTextBoxSearch";
            toolStripTextBoxSearch.Size = new Size(220, 30);
            toolStripTextBoxSearch.KeyDown += toolStripTextBoxSearch_KeyDown;
            //
            // toolStripButtonFindNext
            //
            toolStripButtonFindNext.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStripButtonFindNext.ForeColor = Color.White;
            toolStripButtonFindNext.Name = "toolStripButtonFindNext";
            toolStripButtonFindNext.Size = new Size(77, 27);
            toolStripButtonFindNext.Text = "Find next";
            toolStripButtonFindNext.ToolTipText = "Find next (F3)";
            toolStripButtonFindNext.Click += toolStripButtonFindNext_Click;
            //
            // editor
            //
            editor.AcceptsTab = true;
            editor.BackColor = Color.FromArgb(30, 30, 30);
            editor.BorderStyle = BorderStyle.None;
            editor.DetectUrls = false;
            editor.Dock = DockStyle.Fill;
            editor.Font = new Font("Consolas", 11F);
            editor.ForeColor = Color.FromArgb(235, 235, 235);
            editor.Location = new Point(0, 40);
            editor.Name = "editor";
            editor.ReadOnly = true;
            editor.ScrollBars = RichTextBoxScrollBars.Vertical;
            editor.Size = new Size(1200, 732);
            editor.TabIndex = 1;
            editor.Text = "";
            editor.WordWrap = true;
            editor.SelectionChanged += editor_SelectionChanged;
            editor.TextChanged += editor_TextChanged;
            //
            // statusStrip
            //
            statusStrip.BackColor = Color.FromArgb(37, 37, 38);
            statusStrip.Items.AddRange(new ToolStripItem[] {
                statusLabelState,
                statusLabelPosition,
                statusLabelCharacters,
                statusLabelEncoding
            });
            statusStrip.Location = new Point(0, 772);
            statusStrip.Name = "statusStrip";
            statusStrip.Size = new Size(1200, 28);
            statusStrip.SizingGrip = false;
            statusStrip.TabIndex = 2;
            //
            // statusLabelState
            //
            statusLabelState.ForeColor = Color.FromArgb(210, 210, 210);
            statusLabelState.Name = "statusLabelState";
            statusLabelState.Size = new Size(807, 22);
            statusLabelState.Spring = true;
            statusLabelState.Text = "Saved";
            statusLabelState.TextAlign = ContentAlignment.MiddleLeft;
            //
            // statusLabelPosition
            //
            statusLabelPosition.ForeColor = Color.FromArgb(210, 210, 210);
            statusLabelPosition.Name = "statusLabelPosition";
            statusLabelPosition.Size = new Size(85, 22);
            statusLabelPosition.Text = "Ln 1, Col 1";
            //
            // statusLabelCharacters
            //
            statusLabelCharacters.ForeColor = Color.FromArgb(210, 210, 210);
            statusLabelCharacters.Name = "statusLabelCharacters";
            statusLabelCharacters.Size = new Size(103, 22);
            statusLabelCharacters.Text = "0 characters";
            //
            // statusLabelEncoding
            //
            statusLabelEncoding.ForeColor = Color.FromArgb(210, 210, 210);
            statusLabelEncoding.Name = "statusLabelEncoding";
            statusLabelEncoding.Size = new Size(88, 22);
            statusLabelEncoding.Text = "UTF-8";
            //
            // TextEditorForm
            //
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(30, 30, 30);
            ClientSize = new Size(1200, 800);
            Controls.Add(editor);
            Controls.Add(statusStrip);
            Controls.Add(toolStrip);
            MinimumSize = new Size(640, 420);
            Name = "TextEditorForm";
            ShowIcon = false;
            Text = "Text Editor";
            FormClosing += TextEditorForm_FormClosing;
            KeyDown += TextEditorForm_KeyDown;
            toolStrip.ResumeLayout(false);
            toolStrip.PerformLayout();
            statusStrip.ResumeLayout(false);
            statusStrip.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ToolStrip toolStrip;
        private ToolStripButton toolStripButtonReadOnly;
        private ToolStripSeparator toolStripSeparatorReadOnly;
        private ToolStripButton toolStripButtonSave;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripButton toolStripButtonUndo;
        private ToolStripButton toolStripButtonRedo;
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripLabel toolStripLabelFont;
        private ToolStripComboBox toolStripComboBoxFontFamily;
        private ToolStripLabel toolStripLabelFontSize;
        private ToolStripComboBox toolStripComboBoxFontSize;
        private ToolStripSeparator toolStripSeparator3;
        private ToolStripLabel toolStripLabelSearch;
        private ToolStripTextBox toolStripTextBoxSearch;
        private ToolStripButton toolStripButtonFindNext;
        private RichTextBox editor;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabelState;
        private ToolStripStatusLabel statusLabelPosition;
        private ToolStripStatusLabel statusLabelCharacters;
        private ToolStripStatusLabel statusLabelEncoding;
    }
}
