namespace SrtTool;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;
    private System.Windows.Forms.MenuStrip menuStrip;
    private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem saveAsToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem registerFileAssociationToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem editToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem undoToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem redoToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem selectAllToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem deleteToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem toolsToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem shiftSelectedToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem fixOverlapsToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
    private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
    private System.Windows.Forms.ListView listView;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        menuStrip = new System.Windows.Forms.MenuStrip();
        listView = new System.Windows.Forms.ListView();

        // Initialize MenuStrip
        fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        registerFileAssociationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        undoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        redoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        selectAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        deleteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        shiftSelectedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        fixOverlapsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();

        menuStrip.SuspendLayout();

        // MenuStrip
        menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            fileToolStripMenuItem,
            editToolStripMenuItem,
            toolsToolStripMenuItem,
            helpToolStripMenuItem
        });
        menuStrip.Location = new System.Drawing.Point(0, 0);
        menuStrip.Size = new System.Drawing.Size(800, 24);

        // File Menu
        fileToolStripMenuItem.Text = "&File";
        fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            openToolStripMenuItem,
            saveToolStripMenuItem,
            saveAsToolStripMenuItem,
            new ToolStripSeparator(),
            CreateImportMenu(),
            CreateExportMenu(),
            new ToolStripSeparator(),
            registerFileAssociationToolStripMenuItem
        });

        openToolStripMenuItem.Text = "&Open";
        openToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.O;
        openToolStripMenuItem.Click += (s, e) => {
            using var dialog = new OpenFileDialog();
            dialog.Filter = "SRT files (*.srt)|*.srt|All files (*.*)|*.*";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                OpenFile(dialog.FileName);
            }
        };

        saveToolStripMenuItem.Text = "&Save";
        saveToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.S;
        saveToolStripMenuItem.Click += (s, e) => {
            if (currentFile != null)
                SaveFile(currentFile);
            else
                saveAsToolStripMenuItem.PerformClick();
        };

        saveAsToolStripMenuItem.Text = "Save &As...";
        saveAsToolStripMenuItem.Click += SaveAsToolStripMenuItem_Click;

        registerFileAssociationToolStripMenuItem.Text = "Register as .srt handler";
        registerFileAssociationToolStripMenuItem.Click += (s, e) => {
            try
            {
                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = Application.ExecutablePath,
                    Arguments = "--register",
                    UseShellExecute = true,
                    Verb = "runas"
                };
                System.Diagnostics.Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error launching registration: {ex.Message}", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };

        // Edit Menu
        editToolStripMenuItem.Text = "&Edit";
        editToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            undoToolStripMenuItem,
            redoToolStripMenuItem,
            new ToolStripSeparator(),
            deleteToolStripMenuItem,
            new ToolStripSeparator(),
            selectAllToolStripMenuItem
        });

        undoToolStripMenuItem.Text = "&Undo";
        undoToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.Z;
        undoToolStripMenuItem.Click += (s, e) => Undo();

        redoToolStripMenuItem.Text = "&Redo";
        redoToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.Y;
        redoToolStripMenuItem.Click += (s, e) => Redo();

        deleteToolStripMenuItem.Text = "&Delete";
        deleteToolStripMenuItem.ShortcutKeys = Keys.Delete;
        deleteToolStripMenuItem.Click += (s, e) => {
            if (listView.SelectedIndices.Count > 0)
            {
                SaveState();
                var indices = listView.SelectedIndices.Cast<int>().OrderByDescending(i => i).ToList();
                foreach (var index in indices)
                {
                    subtitles.RemoveAt(index);
                }
                // Renumber remaining entries
                for (int i = 0; i < subtitles.Count; i++)
                {
                    subtitles[i].Index = i + 1;
                }
                RefreshListView();
            }
        };

        selectAllToolStripMenuItem.Text = "Select &All";
        selectAllToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.A;
        selectAllToolStripMenuItem.Click += (s, e) => {
            for (int i = 0; i < listView.Items.Count; i++)
            {
                listView.Items[i].Selected = true;
            }
        };

        // Tools Menu
        toolsToolStripMenuItem.Text = "&Tools";
        toolsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            shiftSelectedToolStripMenuItem,
            fixOverlapsToolStripMenuItem,
            new ToolStripSeparator(),
            CreateTimingMenu(),
            CreateBatchMenu(),
            CreateTextMenu()
        });

        shiftSelectedToolStripMenuItem.Text = "&Shift Timestamps";
        shiftSelectedToolStripMenuItem.Click += (s, e) => {
            var form = new InputForm("Shift Timestamps", 
                "Enter time shift in seconds:\n(use negative number to shift backwards)");
            
            if (form.ShowDialog() == DialogResult.OK)
            {
                if (double.TryParse(form.Value, out double seconds))
                {
                    ShiftTimestamps(TimeSpan.FromSeconds(seconds), listView.SelectedIndices.Count > 0);
                }
                else
                {
                    MessageBox.Show("Please enter a valid number.", "Invalid Input", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        };

        fixOverlapsToolStripMenuItem.Text = "Fix &Overlaps";
        fixOverlapsToolStripMenuItem.Click += (s, e) => FixOverlaps();

        // Help Menu
        helpToolStripMenuItem.Text = "&Help";
        helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            aboutToolStripMenuItem
        });

        aboutToolStripMenuItem.Text = "&About";
        aboutToolStripMenuItem.Click += (s, e) => {
            string message = "SrtTool\n\n" +
                "A professional .srt subtitle file editor.\n\n" +
                "Features:\n" +
                "- Edit subtitles and timestamps\n" +
                "- Advanced timing operations\n" +
                "- Batch processing\n" +
                "- Text cleanup and formatting\n" +
                "- Smart timing adjustment\n" +
                "- Undo/Redo support\n\n" +
                "Keyboard Shortcuts:\n" +
                "F2 - Edit selected subtitle\n" +
                "Ctrl+Z - Undo\n" +
                "Ctrl+Y - Redo\n" +
                "Ctrl+S - Save\n" +
                "Ctrl+O - Open\n" +
                "Delete - Delete selected\n" +
                "Ctrl+A - Select all";

            MessageBox.Show(message, "About SrtTool", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        };

        // ListView
        listView.Dock = DockStyle.Fill;
        listView.HideSelection = false;
        listView.DoubleClick += (s, e) => EditSelectedEntry();
        listView.GridLines = true;
        listView.FullRowSelect = true;
        listView.MultiSelect = true;

        // MainForm
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(800, 450);
        Controls.Add(listView);
        Controls.Add(menuStrip);
        MainMenuStrip = menuStrip;
        Text = "SrtTool";

        menuStrip.ResumeLayout(false);
        menuStrip.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    private void SaveAsToolStripMenuItem_Click(object sender, EventArgs e)
    {
        using var dialog = new SaveFileDialog();
        dialog.Filter = "SRT files (*.srt)|*.srt|All files (*.*)|*.*";
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            SaveFile(dialog.FileName);
        }
    }

    private void ShiftSelectedToolStripMenuItem_Click(object sender, EventArgs e)
    {
        var form = new InputForm("Shift Timestamps", 
            "Enter time shift in format: [-]hh:mm:ss,fff\nUse minus sign for backward shift");
        
        if (form.ShowDialog() == DialogResult.OK)
        {
            if (TimeSpan.TryParse(form.Value.Replace(',', '.'), out var shift))
            {
                ShiftTimestamps(shift, listView.SelectedIndices.Count > 0);
            }
        }
    }

    private ToolStripMenuItem CreateTimingMenu()
    {
        var menu = new ToolStripMenuItem("&Timing");
        
        var scaleItem = new ToolStripMenuItem("&Scale Timing...");
        scaleItem.Click += (s, e) => {
            var form = new InputForm("Scale Timing", 
                "Enter scale factor:\n(e.g., 1.1 to stretch by 10%, 0.9 to compress by 10%)");
            if (form.ShowDialog() == DialogResult.OK && double.TryParse(form.Value, out double factor))
            {
                ScaleTimestamps(factor);
            }
        };

        var snapItem = new ToolStripMenuItem("Snap to &Intervals...");
        snapItem.Click += (s, e) => {
            var form = new InputForm("Snap to Intervals", 
                "Enter interval in milliseconds:\n(e.g., 500 for half-second intervals)");
            if (form.ShowDialog() == DialogResult.OK && int.TryParse(form.Value, out int interval))
            {
                SnapToIntervals(interval);
            }
        };

        var smartAdjustItem = new ToolStripMenuItem("Smart &Duration Adjustment");
        smartAdjustItem.Click += (s, e) => SmartDurationAdjustment();

        var frameRateItem = new ToolStripMenuItem("Sync to Frame Rate...");
        frameRateItem.Click += (s, e) => {
            var form = new InputForm("Sync to Frame Rate", 
                "Enter target frame rate:\n(e.g., 23.976, 25, 29.97)");
            if (form.ShowDialog() == DialogResult.OK && double.TryParse(form.Value, out double fps))
            {
                SyncToFrameRate(fps);
            }
        };

        var adjustDisplayTimeItem = new ToolStripMenuItem("Adjust Display Time...");
        adjustDisplayTimeItem.Click += (s, e) => AdjustDisplayTime();

        menu.DropDownItems.AddRange(new ToolStripItem[] {
            scaleItem,
            snapItem,
            smartAdjustItem,
            frameRateItem,
            new ToolStripSeparator(),
            adjustDisplayTimeItem
        });

        return menu;
    }

    private ToolStripMenuItem CreateBatchMenu()
    {
        var menu = new ToolStripMenuItem("&Batch");
        
        var mergeItem = new ToolStripMenuItem("&Merge SRT Files...");
        mergeItem.Click += (s, e) => MergeSrtFiles();

        var splitItem = new ToolStripMenuItem("&Split at Selection");
        splitItem.Click += (s, e) => SplitAtSelection();

        var reindexItem = new ToolStripMenuItem("Re&index Subtitles");
        reindexItem.Click += (s, e) => ReindexSubtitles();

        var fixGapsItem = new ToolStripMenuItem("Fix &Gaps");
        fixGapsItem.Click += (s, e) => FixGaps();

        menu.DropDownItems.AddRange(new ToolStripItem[] {
            mergeItem,
            splitItem,
            new ToolStripSeparator(),
            reindexItem,
            fixGapsItem
        });

        return menu;
    }

    private ToolStripMenuItem CreateTextMenu()
    {
        var menu = new ToolStripMenuItem("Te&xt");
        
        var upperItem = new ToolStripMenuItem("Convert to &UPPERCASE");
        upperItem.Click += (s, e) => ConvertCase(TextCase.Upper);

        var lowerItem = new ToolStripMenuItem("Convert to &lowercase");
        lowerItem.Click += (s, e) => ConvertCase(TextCase.Lower);

        var titleItem = new ToolStripMenuItem("Convert to &Title Case");
        titleItem.Click += (s, e) => ConvertCase(TextCase.Title);

        var cleanTagsItem = new ToolStripMenuItem("Remove &Formatting Tags");
        cleanTagsItem.Click += (s, e) => RemoveFormattingTags();

        var fixOcrItem = new ToolStripMenuItem("Fix Common &OCR Errors");
        fixOcrItem.Click += (s, e) => FixOcrErrors();

        var encodingItem = new ToolStripMenuItem("Convert &Encoding...");
        encodingItem.Click += (s, e) => ConvertEncoding();

        var hiItem = new ToolStripMenuItem("Remove &Hearing Impaired Text");
        hiItem.Click += (s, e) => RemoveHearingImpairedText();

        var mergeShortItem = new ToolStripMenuItem("&Merge Short Lines");
        mergeShortItem.Click += (s, e) => MergeShortLines();

        var splitLongItem = new ToolStripMenuItem("&Split Long Lines");
        splitLongItem.Click += (s, e) => SplitLongLines();

        menu.DropDownItems.AddRange(new ToolStripItem[] {
            upperItem,
            lowerItem,
            titleItem,
            new ToolStripSeparator(),
            cleanTagsItem,
            fixOcrItem,
            encodingItem,
            new ToolStripSeparator(),
            hiItem,
            mergeShortItem,
            splitLongItem
        });

        return menu;
    }

    private ToolStripMenuItem CreateImportMenu()
    {
        var menu = new ToolStripMenuItem("&Import");
        
        var subViewerItem = new ToolStripMenuItem("SubViewer (*.sub)");
        subViewerItem.Click += (s, e) => ImportSubtitles("SubViewer");

        var microDvdItem = new ToolStripMenuItem("MicroDVD (*.sub)");
        microDvdItem.Click += (s, e) => ImportSubtitles("MicroDVD");

        var samiItem = new ToolStripMenuItem("SAMI (*.smi)");
        samiItem.Click += (s, e) => ImportSubtitles("SAMI");

        var ssaItem = new ToolStripMenuItem("Sub Station Alpha (*.ssa)");
        ssaItem.Click += (s, e) => ImportSubtitles("SSA");

        var assItem = new ToolStripMenuItem("Advanced Sub Station Alpha (*.ass)");
        assItem.Click += (s, e) => ImportSubtitles("ASS");

        var webVttItem = new ToolStripMenuItem("WebVTT (*.vtt)");
        webVttItem.Click += (s, e) => ImportSubtitles("WebVTT");

        menu.DropDownItems.AddRange(new ToolStripItem[] {
            subViewerItem,
            microDvdItem,
            samiItem,
            ssaItem,
            assItem,
            webVttItem
        });

        return menu;
    }

    private ToolStripMenuItem CreateExportMenu()
    {
        var menu = new ToolStripMenuItem("&Export");
        
        var subViewerItem = new ToolStripMenuItem("SubViewer (*.sub)");
        subViewerItem.Click += (s, e) => ExportSubtitles("SubViewer");

        var microDvdItem = new ToolStripMenuItem("MicroDVD (*.sub)");
        microDvdItem.Click += (s, e) => ExportSubtitles("MicroDVD");

        var samiItem = new ToolStripMenuItem("SAMI (*.smi)");
        samiItem.Click += (s, e) => ExportSubtitles("SAMI");

        var ssaItem = new ToolStripMenuItem("Sub Station Alpha (*.ssa)");
        ssaItem.Click += (s, e) => ExportSubtitles("SSA");

        var assItem = new ToolStripMenuItem("Advanced Sub Station Alpha (*.ass)");
        assItem.Click += (s, e) => ExportSubtitles("ASS");

        var webVttItem = new ToolStripMenuItem("WebVTT (*.vtt)");
        webVttItem.Click += (s, e) => ExportSubtitles("WebVTT");

        menu.DropDownItems.AddRange(new ToolStripItem[] {
            subViewerItem,
            microDvdItem,
            samiItem,
            ssaItem,
            assItem,
            webVttItem
        });

        return menu;
    }
} 