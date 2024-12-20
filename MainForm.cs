using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Diagnostics;

namespace SrtTool;

public partial class MainForm : Form
{
    private List<SrtEntry> subtitles = new();
    private Stack<List<SrtEntry>> undoStack = new();
    private Stack<List<SrtEntry>> redoStack = new();
    private string? currentFile = null;
    private bool isDirty = false;

    // Add drag selection variables
    private bool isDragging = false;
    private int dragStartIndex = -1;

    public MainForm()
    {
        InitializeComponent();
        InitializeListView();

        // Set application and form icon
        try
        {
            var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "512.ico");
            if (File.Exists(iconPath))
            {
                this.Icon = new Icon(iconPath);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load icon: {ex.Message}");
        }

        // Handle form closing
        this.FormClosing += (s, e) => {
            if (isDirty)
            {
                var result = MessageBox.Show(
                    "Do you want to save changes before closing?",
                    "Unsaved Changes",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    if (currentFile != null)
                        SaveFile(currentFile);
                    else
                    {
                        using var dialog = new SaveFileDialog();
                        dialog.Filter = "SRT files (*.srt)|*.srt|All files (*.*)|*.*";
                        if (dialog.ShowDialog() == DialogResult.OK)
                        {
                            SaveFile(dialog.FileName);
                        }
                        else
                        {
                            e.Cancel = true;
                        }
                    }
                }
                else if (result == DialogResult.Cancel)
                {
                    e.Cancel = true;
                }
            }
        };
    }

    private void InitializeListView()
    {
        listView.Columns.Add("Index", 60);
        listView.Columns.Add("Start Time", 100);
        listView.Columns.Add("End Time", 100);
        listView.Columns.Add("Text", 400);
        listView.FullRowSelect = true;
        listView.MultiSelect = true;
        listView.View = View.Details;

        // Add context menu
        var contextMenu = new ContextMenuStrip();

        // Edit submenu
        var editMenu = new ToolStripMenuItem("Edit");
        editMenu.DropDownItems.Add("Edit Selected", null, (s, e) => EditSelectedEntry());
        editMenu.DropDownItems.Add(new ToolStripSeparator());
        editMenu.DropDownItems.Add("Undo", null, (s, e) => Undo());
        editMenu.DropDownItems.Add("Redo", null, (s, e) => Redo());
        editMenu.DropDownItems.Add(new ToolStripSeparator());
        editMenu.DropDownItems.Add("Delete", null, (s, e) => deleteToolStripMenuItem.PerformClick());
        editMenu.DropDownItems.Add("Select All", null, (s, e) => selectAllToolStripMenuItem.PerformClick());
        contextMenu.Items.Add(editMenu);

        // Tools submenu
        var toolsMenu = new ToolStripMenuItem("Tools");
        toolsMenu.DropDownItems.Add("Shift Timestamps", null, (s, e) => shiftSelectedToolStripMenuItem.PerformClick());
        toolsMenu.DropDownItems.Add("Fix Overlaps", null, (s, e) => fixOverlapsToolStripMenuItem.PerformClick());
        toolsMenu.DropDownItems.Add(new ToolStripSeparator());

        // Timing submenu
        var timingMenu = new ToolStripMenuItem("Timing");
        timingMenu.DropDownItems.Add("Scale Timing...", null, (s, e) => {
            var form = new InputForm("Scale Timing", 
                "Enter scale factor:\n(e.g., 1.1 to stretch by 10%, 0.9 to compress by 10%)");
            if (form.ShowDialog() == DialogResult.OK && double.TryParse(form.Value, out double factor))
            {
                ScaleTimestamps(factor);
            }
        });
        timingMenu.DropDownItems.Add("Snap to Intervals...", null, (s, e) => {
            var form = new InputForm("Snap to Intervals", 
                "Enter interval in milliseconds:\n(e.g., 500 for half-second intervals)");
            if (form.ShowDialog() == DialogResult.OK && int.TryParse(form.Value, out int interval))
            {
                SnapToIntervals(interval);
            }
        });
        timingMenu.DropDownItems.Add("Smart Duration Adjustment", null, (s, e) => SmartDurationAdjustment());
        timingMenu.DropDownItems.Add("Sync to Frame Rate...", null, (s, e) => {
            var form = new InputForm("Sync to Frame Rate", 
                "Enter target frame rate:\n(e.g., 23.976, 25, 29.97)");
            if (form.ShowDialog() == DialogResult.OK && double.TryParse(form.Value, out double fps))
            {
                SyncToFrameRate(fps);
            }
        });
        timingMenu.DropDownItems.Add(new ToolStripSeparator());
        timingMenu.DropDownItems.Add("Adjust Display Time...", null, (s, e) => AdjustDisplayTime());
        toolsMenu.DropDownItems.Add(timingMenu);

        // Text operations submenu
        var textMenu = new ToolStripMenuItem("Text");
        textMenu.DropDownItems.Add("Convert to UPPERCASE", null, (s, e) => ConvertCase(TextCase.Upper));
        textMenu.DropDownItems.Add("Convert to lowercase", null, (s, e) => ConvertCase(TextCase.Lower));
        textMenu.DropDownItems.Add("Convert to Title Case", null, (s, e) => ConvertCase(TextCase.Title));
        textMenu.DropDownItems.Add(new ToolStripSeparator());
        textMenu.DropDownItems.Add("Remove Formatting Tags", null, (s, e) => RemoveFormattingTags());
        textMenu.DropDownItems.Add("Fix Common OCR Errors", null, (s, e) => FixOcrErrors());
        textMenu.DropDownItems.Add("Convert Encoding...", null, (s, e) => ConvertEncoding());
        textMenu.DropDownItems.Add(new ToolStripSeparator());
        textMenu.DropDownItems.Add("Remove Hearing Impaired Text", null, (s, e) => RemoveHearingImpairedText());
        textMenu.DropDownItems.Add("Merge Short Lines", null, (s, e) => MergeShortLines());
        textMenu.DropDownItems.Add("Split Long Lines", null, (s, e) => SplitLongLines());
        toolsMenu.DropDownItems.Add(textMenu);

        // Batch operations submenu
        var batchMenu = new ToolStripMenuItem("Batch");
        batchMenu.DropDownItems.Add("Merge SRT Files...", null, (s, e) => MergeSrtFiles());
        batchMenu.DropDownItems.Add("Split at Selection", null, (s, e) => SplitAtSelection());
        batchMenu.DropDownItems.Add(new ToolStripSeparator());
        batchMenu.DropDownItems.Add("Reindex Subtitles", null, (s, e) => ReindexSubtitles());
        batchMenu.DropDownItems.Add("Fix Gaps", null, (s, e) => FixGaps());
        toolsMenu.DropDownItems.Add(batchMenu);

        contextMenu.Items.Add(toolsMenu);

        // Update context menu items enabled state based on selection
        contextMenu.Opening += (s, e) => {
            bool hasSelection = listView.SelectedIndices.Count > 0;
            editMenu.DropDownItems[0].Enabled = listView.SelectedIndices.Count == 1; // Edit Selected
            editMenu.DropDownItems[2].Enabled = undoStack.Count > 0; // Undo
            editMenu.DropDownItems[3].Enabled = redoStack.Count > 0; // Redo
            editMenu.DropDownItems[5].Enabled = hasSelection; // Delete
            batchMenu.DropDownItems[1].Enabled = listView.SelectedIndices.Count == 1; // Split at Selection
        };

        listView.ContextMenuStrip = contextMenu;

        // Prevent the context menu from changing selection
        listView.MouseUp += (s, e) => {
            if (e.Button == MouseButtons.Right)
            {
                // Get the item under the mouse
                var item = listView.GetItemAt(e.X, e.Y);
                if (item != null)
                {
                    // If clicking on an unselected item, select only that item
                    if (!item.Selected)
                    {
                        listView.SelectedItems.Clear();
                        item.Selected = true;
                    }
                    // If clicking on a selected item, maintain the current selection
                }
                // Show the context menu
                contextMenu.Show(listView, e.Location);
            }
        };

        // Add drag selection event handlers
        listView.MouseDown += (s, e) => {
            if (e.Button == MouseButtons.Left)
            {
                var item = listView.GetItemAt(e.X, e.Y);
                if (item != null)
                {
                    isDragging = true;
                    dragStartIndex = item.Index;
                    if (!ModifierKeys.HasFlag(Keys.Control))
                    {
                        listView.SelectedIndices.Clear();
                    }
                    item.Selected = true;
                }
            }
        };

        listView.MouseMove += (s, e) => {
            if (isDragging && e.Button == MouseButtons.Left)
            {
                var item = listView.GetItemAt(e.X, e.Y);
                if (item != null)
                {
                    int currentIndex = item.Index;
                    int start = Math.Min(dragStartIndex, currentIndex);
                    int end = Math.Max(dragStartIndex, currentIndex);

                    // Clear previous selection if not using Ctrl key
                    if (!ModifierKeys.HasFlag(Keys.Control))
                    {
                        listView.SelectedIndices.Clear();
                    }

                    // Select all items in the range
                    for (int i = start; i <= end; i++)
                    {
                        listView.Items[i].Selected = true;
                    }
                }
            }
        };

        listView.MouseUp += (s, e) => {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = false;
                dragStartIndex = -1;
            }
        };

        // Add double-click handler for editing
        listView.DoubleClick += (s, e) => EditSelectedEntry();
    }

    private void SaveState()
    {
        var state = subtitles.Select(s => new SrtEntry
        {
            Index = s.Index,
            StartTime = s.StartTime,
            EndTime = s.EndTime,
            Text = s.Text
        }).ToList();
        
        undoStack.Push(state);
        redoStack.Clear();
        UpdateUndoRedoMenuItems();
        isDirty = true;
        UpdateTitle();
    }

    private void UpdateTitle()
    {
        string fileName = currentFile != null ? Path.GetFileName(currentFile) : "Untitled";
        Text = $"SrtTool - {fileName}{(isDirty ? "*" : "")}";
    }

    private void Undo()
    {
        if (undoStack.Count > 0)
        {
            var currentState = subtitles.Select(s => new SrtEntry
            {
                Index = s.Index,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                Text = s.Text
            }).ToList();
            
            redoStack.Push(currentState);
            subtitles = undoStack.Pop();
            RefreshListView();
            UpdateUndoRedoMenuItems();
        }
    }

    private void Redo()
    {
        if (redoStack.Count > 0)
        {
            var currentState = subtitles.Select(s => new SrtEntry
            {
                Index = s.Index,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                Text = s.Text
            }).ToList();
            
            undoStack.Push(currentState);
            subtitles = redoStack.Pop();
            RefreshListView();
            UpdateUndoRedoMenuItems();
        }
    }

    private void UpdateUndoRedoMenuItems()
    {
        undoToolStripMenuItem.Enabled = undoStack.Count > 0;
        redoToolStripMenuItem.Enabled = redoStack.Count > 0;
    }

    public void OpenFile(string path)
    {
        try
        {
            if (isDirty)
            {
                var result = MessageBox.Show(
                    "Do you want to save changes before opening another file?",
                    "Unsaved Changes",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    if (currentFile != null)
                        SaveFile(currentFile);
                    else
                    {
                        using var dialog = new SaveFileDialog();
                        dialog.Filter = "SRT files (*.srt)|*.srt|All files (*.*)|*.*";
                        if (dialog.ShowDialog() == DialogResult.OK)
                        {
                            SaveFile(dialog.FileName);
                        }
                        else
                            return;
                    }
                }
                else if (result == DialogResult.Cancel)
                {
                    return;
                }
            }

            var lines = File.ReadAllLines(path);
            subtitles.Clear();
            undoStack.Clear();
            redoStack.Clear();

            for (int i = 0; i < lines.Length;)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) 
                {
                    i++;
                    continue;
                }

                var entry = new SrtEntry();
                
                // Parse index
                if (!int.TryParse(lines[i], out int index))
                {
                    i++;
                    continue;
                }
                entry.Index = index;
                i++;

                // Parse timestamps
                if (i >= lines.Length) break;
                var timeLine = lines[i];
                var times = timeLine.Split(new[] { " --> " }, StringSplitOptions.None);
                if (times.Length == 2)
                {
                    entry.StartTime = ParseTimeStamp(times[0]);
                    entry.EndTime = ParseTimeStamp(times[1]);
                }
                i++;

                // Parse text
                var textLines = new List<string>();
                while (i < lines.Length && !string.IsNullOrWhiteSpace(lines[i]))
                {
                    textLines.Add(lines[i]);
                    i++;
                }
                entry.Text = string.Join("\r\n", textLines);

                subtitles.Add(entry);
            }

            currentFile = path;
            isDirty = false;
            RefreshListView();
            UpdateTitle();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error opening file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private TimeSpan ParseTimeStamp(string timestamp)
    {
        timestamp = timestamp.Trim();
        string[] parts = timestamp.Replace(",", ":").Split(':');
        if (parts.Length == 4)
        {
            return new TimeSpan(0, 
                int.Parse(parts[0]), // hours
                int.Parse(parts[1]), // minutes
                int.Parse(parts[2]), // seconds
                int.Parse(parts[3])); // milliseconds
        }
        throw new FormatException("Invalid timestamp format. Expected hh:mm:ss,fff");
    }

    private void SaveFile(string path)
    {
        try
        {
            using var writer = new StreamWriter(path);
            foreach (var entry in subtitles)
            {
                writer.WriteLine(entry.ToString());
                writer.WriteLine();
            }
            currentFile = path;
            isDirty = false;
            UpdateTitle();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RefreshListView()
    {
        listView.Items.Clear();
        foreach (var entry in subtitles)
        {
            var item = new ListViewItem(entry.Index.ToString());
            item.SubItems.Add(entry.StartTime.ToString(@"hh\:mm\:ss\,fff"));
            item.SubItems.Add(entry.EndTime.ToString(@"hh\:mm\:ss\,fff"));
            item.SubItems.Add(entry.Text.Replace("\r\n", " "));
            listView.Items.Add(item);
        }
    }

    private void ShiftTimestamps(TimeSpan shift, bool selectedOnly = false)
    {
        SaveState();
        var entriesToShift = selectedOnly ? 
            listView.SelectedIndices.Cast<int>().Select(i => subtitles[i]) : 
            subtitles;

        foreach (var entry in entriesToShift)
        {
            entry.StartTime += shift;
            entry.EndTime += shift;
        }
        RefreshListView();
    }

    private void FixOverlaps()
    {
        SaveState();
        for (int i = 0; i < subtitles.Count - 1; i++)
        {
            if (subtitles[i].EndTime > subtitles[i + 1].StartTime)
            {
                subtitles[i].EndTime = subtitles[i + 1].StartTime;
            }
        }
        RefreshListView();
    }

    private void EditSelectedEntry()
    {
        if (listView.SelectedIndices.Count != 1) return;

        var index = listView.SelectedIndices[0];
        var entry = subtitles[index];
        var form = new SrtEntryEditForm
        {
            StartTime = entry.StartTime.ToString(@"hh\:mm\:ss\,fff"),
            EndTime = entry.EndTime.ToString(@"hh\:mm\:ss\,fff"),
            SubtitleText = entry.Text
        };

        if (form.ShowDialog() == DialogResult.OK)
        {
            SaveState();
            if (TimeSpan.TryParse(form.StartTime.Replace(',', '.'), out var startTime) &&
                TimeSpan.TryParse(form.EndTime.Replace(',', '.'), out var endTime))
            {
                entry.StartTime = startTime;
                entry.EndTime = endTime;
                entry.Text = form.SubtitleText;
                RefreshListView();
            }
        }
    }

    private enum TextCase { Upper, Lower, Title }

    private void ScaleTimestamps(double factor)
    {
        if (factor <= 0) return;
        SaveState();

        foreach (var entry in subtitles)
        {
            entry.StartTime = TimeSpan.FromTicks((long)(entry.StartTime.Ticks * factor));
            entry.EndTime = TimeSpan.FromTicks((long)(entry.EndTime.Ticks * factor));
        }
        RefreshListView();
    }

    private void SnapToIntervals(int intervalMs)
    {
        if (intervalMs <= 0) return;
        SaveState();

        foreach (var entry in subtitles)
        {
            var startTicks = entry.StartTime.Ticks;
            var endTicks = entry.EndTime.Ticks;
            var intervalTicks = TimeSpan.FromMilliseconds(intervalMs).Ticks;

            entry.StartTime = TimeSpan.FromTicks((startTicks / intervalTicks) * intervalTicks);
            entry.EndTime = TimeSpan.FromTicks((endTicks / intervalTicks) * intervalTicks);
        }
        RefreshListView();
    }

    private void SmartDurationAdjustment()
    {
        SaveState();
        const int charsPerSecond = 15; // Average reading speed

        foreach (var entry in subtitles)
        {
            int charCount = entry.Text.Count(c => !char.IsWhiteSpace(c));
            double idealDuration = charCount / (double)charsPerSecond;
            var minDuration = TimeSpan.FromSeconds(Math.Max(1, idealDuration));
            var maxDuration = TimeSpan.FromSeconds(Math.Min(10, idealDuration * 1.5));

            var currentDuration = entry.EndTime - entry.StartTime;
            if (currentDuration < minDuration)
            {
                entry.EndTime = entry.StartTime + minDuration;
            }
            else if (currentDuration > maxDuration)
            {
                entry.EndTime = entry.StartTime + maxDuration;
            }
        }
        RefreshListView();
    }

    private void SyncToFrameRate(double fps)
    {
        if (fps <= 0) return;
        SaveState();

        var frameTime = TimeSpan.FromSeconds(1.0 / fps);
        foreach (var entry in subtitles)
        {
            var startFrames = (long)(entry.StartTime.TotalSeconds * fps);
            var endFrames = (long)(entry.EndTime.TotalSeconds * fps);
            entry.StartTime = TimeSpan.FromTicks(startFrames * frameTime.Ticks);
            entry.EndTime = TimeSpan.FromTicks(endFrames * frameTime.Ticks);
        }
        RefreshListView();
    }

    private void MergeSrtFiles()
    {
        using var dialog = new OpenFileDialog();
        dialog.Filter = "SRT files (*.srt)|*.srt|All files (*.*)|*.*";
        dialog.Multiselect = true;
        if (dialog.ShowDialog() != DialogResult.OK) return;

        SaveState();
        var originalCount = subtitles.Count;
        var lastEndTime = subtitles.Count > 0 ? subtitles.Last().EndTime : TimeSpan.Zero;

        foreach (var file in dialog.FileNames)
        {
            try
            {
                var lines = File.ReadAllLines(file);
                var newEntries = new List<SrtEntry>();
                SrtEntry? currentEntry = null;
                List<string> textLines = new();

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        if (currentEntry != null)
                        {
                            currentEntry.Text = string.Join("\r\n", textLines);
                            newEntries.Add(currentEntry);
                            currentEntry = null;
                            textLines.Clear();
                        }
                        continue;
                    }

                    if (currentEntry == null)
                    {
                        currentEntry = new SrtEntry();
                        if (int.TryParse(line, out _)) continue;
                    }

                    if (currentEntry.StartTime == TimeSpan.Zero && line.Contains("-->"))
                    {
                        var times = line.Split(new[] { " --> " }, StringSplitOptions.None);
                        if (times.Length == 2)
                        {
                            currentEntry.StartTime = ParseTimeStamp(times[0]);
                            currentEntry.EndTime = ParseTimeStamp(times[1]);
                        }
                        continue;
                    }

                    textLines.Add(line);
                }

                // Add last entry if exists
                if (currentEntry != null)
                {
                    currentEntry.Text = string.Join("\r\n", textLines);
                    newEntries.Add(currentEntry);
                }

                // Adjust timing for merged entries
                foreach (var entry in newEntries)
                {
                    entry.StartTime += lastEndTime + TimeSpan.FromSeconds(1);
                    entry.EndTime += lastEndTime + TimeSpan.FromSeconds(1);
                }

                subtitles.AddRange(newEntries);
                lastEndTime = newEntries.Last().EndTime;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error merging file {Path.GetFileName(file)}: {ex.Message}",
                    "Merge Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        ReindexSubtitles();
        RefreshListView();
    }

    private void SplitAtSelection()
    {
        if (listView.SelectedIndices.Count != 1) return;

        var index = listView.SelectedIndices[0];
        if (index <= 0 || index >= subtitles.Count - 1) return;

        using var dialog = new SaveFileDialog();
        dialog.Filter = "SRT files (*.srt)|*.srt|All files (*.*)|*.*";
        if (dialog.ShowDialog() != DialogResult.OK) return;

        try
        {
            // Save second part to new file
            using (var writer = new StreamWriter(dialog.FileName))
            {
                var secondPart = subtitles.Skip(index).ToList();
                var timeOffset = secondPart[0].StartTime;

                foreach (var entry in secondPart)
                {
                    entry.StartTime -= timeOffset;
                    entry.EndTime -= timeOffset;
                    entry.Index = secondPart.IndexOf(entry) + 1;
                    writer.WriteLine(entry.ToString());
                    writer.WriteLine();
                }
            }

            // Update current file
            SaveState();
            subtitles.RemoveRange(index, subtitles.Count - index);
            ReindexSubtitles();
            RefreshListView();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error splitting file: {ex.Message}",
                "Split Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ReindexSubtitles()
    {
        SaveState();
        for (int i = 0; i < subtitles.Count; i++)
        {
            subtitles[i].Index = i + 1;
        }
        RefreshListView();
    }

    private void FixGaps()
    {
        SaveState();
        const double maxGap = 0.25; // seconds

        for (int i = 0; i < subtitles.Count - 1; i++)
        {
            var gap = (subtitles[i + 1].StartTime - subtitles[i].EndTime).TotalSeconds;
            if (gap > maxGap)
            {
                var midPoint = subtitles[i].EndTime + TimeSpan.FromSeconds(gap / 2);
                subtitles[i].EndTime = midPoint;
                subtitles[i + 1].StartTime = midPoint;
            }
        }
        RefreshListView();
    }

    private void ConvertCase(TextCase textCase)
    {
        SaveState();
        var selectedIndices = listView.SelectedIndices.Count > 0 ?
            listView.SelectedIndices.Cast<int>().ToList() :
            Enumerable.Range(0, subtitles.Count).ToList();

        foreach (var index in selectedIndices)
        {
            var subtitleText = subtitles[index].Text;
            subtitles[index].Text = textCase switch
            {
                TextCase.Upper => subtitleText.ToUpperInvariant(),
                TextCase.Lower => subtitleText.ToLowerInvariant(),
                TextCase.Title => System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(subtitleText.ToLower()),
                _ => subtitleText
            };
        }
        RefreshListView();
    }

    private void RemoveFormattingTags()
    {
        SaveState();
        var selectedIndices = listView.SelectedIndices.Count > 0 ?
            listView.SelectedIndices.Cast<int>().ToList() :
            Enumerable.Range(0, subtitles.Count).ToList();

        foreach (var index in selectedIndices)
        {
            var subtitle = subtitles[index];
            var cleanedText = subtitle.Text;
            // Remove common subtitle formatting tags
            cleanedText = System.Text.RegularExpressions.Regex.Replace(cleanedText, @"<[^>]+>", string.Empty);
            cleanedText = System.Text.RegularExpressions.Regex.Replace(cleanedText, @"\{[^}]+\}", string.Empty);
            subtitle.Text = cleanedText.Trim();
        }
        RefreshListView();
    }

    private void FixOcrErrors()
    {
        SaveState();
        var selectedIndices = listView.SelectedIndices.Count > 0 ?
            listView.SelectedIndices.Cast<int>().ToList() :
            Enumerable.Range(0, subtitles.Count).ToList();

        var commonErrors = new Dictionary<string, string>
        {
            {"l", "I"},
            {"0", "O"},
            {"1", "I"},
            {"rn", "m"},
            {"vv", "w"},
            {"|", "I"},
            {"[", "("},
            {"]", ")"},
            {"¡", "!"},
            {"¿", "?"}
        };

        foreach (var index in selectedIndices)
        {
            var subtitleText = subtitles[index].Text;
            foreach (var error in commonErrors)
            {
                subtitleText = subtitleText.Replace(error.Key, error.Value);
            }
            subtitles[index].Text = subtitleText;
        }
        RefreshListView();
    }

    private void ConvertEncoding()
    {
        var encodings = new[] {
            System.Text.Encoding.UTF8,
            System.Text.Encoding.Unicode,
            System.Text.Encoding.ASCII,
            System.Text.Encoding.GetEncoding("iso-8859-1")
        };

        var form = new Form
        {
            Text = "Convert Encoding",
            Size = new Size(300, 150),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MaximizeBox = false,
            MinimizeBox = false
        };

        var combo = new ComboBox
        {
            Location = new Point(10, 10),
            Width = 260,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        combo.Items.AddRange(encodings.Select(e => e.EncodingName).ToArray());
        combo.SelectedIndex = 0;

        var okButton = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Location = new Point(115, 50)
        };

        form.Controls.AddRange(new Control[] { combo, okButton });
        form.AcceptButton = okButton;

        if (form.ShowDialog() != DialogResult.OK) return;

        SaveState();
        var targetEncoding = encodings[combo.SelectedIndex];
        var selectedIndices = listView.SelectedIndices.Count > 0 ?
            listView.SelectedIndices.Cast<int>().ToList() :
            Enumerable.Range(0, subtitles.Count).ToList();

        foreach (var index in selectedIndices)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(subtitles[index].Text);
            var text = targetEncoding.GetString(bytes);
            subtitles[index].Text = text;
        }
        RefreshListView();
    }

    private void ImportSubtitles(string format)
    {
        using var dialog = new OpenFileDialog();
        string extension = format.ToLower() switch
        {
            "subviewer" => "sub",
            "microdvd" => "sub",
            "sami" => "smi",
            "ssa" => "ssa",
            "ass" => "ass",
            "webvtt" => "vtt",
            _ => "*"
        };
        dialog.Filter = $"{format} files (*.{extension})|*.{extension}|All files (*.*)|*.*";
        
        if (dialog.ShowDialog() != DialogResult.OK) return;

        try
        {
            SaveState();
            var lines = File.ReadAllLines(dialog.FileName);
            subtitles.Clear();

            switch (format.ToLower())
            {
                case "subviewer":
                    ImportSubViewer(lines);
                    break;
                case "microdvd":
                    ImportMicroDVD(lines);
                    break;
                case "sami":
                    ImportSAMI(lines);
                    break;
                case "ssa":
                case "ass":
                    ImportSSA(lines);
                    break;
                case "webvtt":
                    ImportWebVTT(lines);
                    break;
            }

            ReindexSubtitles();
            RefreshListView();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error importing {format} file: {ex.Message}",
                "Import Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ExportSubtitles(string format)
    {
        using var dialog = new SaveFileDialog();
        string extension = format.ToLower() switch
        {
            "subviewer" => "sub",
            "microdvd" => "sub",
            "sami" => "smi",
            "ssa" => "ssa",
            "ass" => "ass",
            "webvtt" => "vtt",
            _ => "*"
        };
        dialog.Filter = $"{format} files (*.{extension})|*.{extension}|All files (*.*)|*.*";
        
        if (dialog.ShowDialog() != DialogResult.OK) return;

        try
        {
            using var writer = new StreamWriter(dialog.FileName);
            switch (format.ToLower())
            {
                case "subviewer":
                    ExportSubViewer(writer);
                    break;
                case "microdvd":
                    ExportMicroDVD(writer);
                    break;
                case "sami":
                    ExportSAMI(writer);
                    break;
                case "ssa":
                case "ass":
                    ExportSSA(writer, format);
                    break;
                case "webvtt":
                    ExportWebVTT(writer);
                    break;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error exporting to {format}: {ex.Message}",
                "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ImportSubViewer(string[] lines)
    {
        for (int i = 0; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            if (lines[i].StartsWith("[INFORMATION]") || lines[i].StartsWith("[STYLE]")) continue;

            var timeParts = lines[i].Split(',');
            if (timeParts.Length != 2) continue;

            var entry = new SrtEntry();
            entry.StartTime = TimeSpan.Parse(timeParts[0]);
            entry.EndTime = TimeSpan.Parse(timeParts[1]);
            i++;

            if (i < lines.Length)
            {
                entry.Text = lines[i].Replace("|", "\r\n");
                subtitles.Add(entry);
            }
        }
    }

    private void ImportMicroDVD(string[] lines)
    {
        const double fps = 23.976; // Default frame rate
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (!line.StartsWith("{")) continue;

            var match = System.Text.RegularExpressions.Regex.Match(line, @"\{(\d+)\}\{(\d+)\}(.*)");
            if (!match.Success) continue;

            var entry = new SrtEntry();
            entry.StartTime = TimeSpan.FromSeconds(int.Parse(match.Groups[1].Value) / fps);
            entry.EndTime = TimeSpan.FromSeconds(int.Parse(match.Groups[2].Value) / fps);
            entry.Text = match.Groups[3].Value.Replace("|", "\r\n");
            subtitles.Add(entry);
        }
    }

    private void ImportSAMI(string[] lines)
    {
        var content = string.Join("\n", lines);
        var matches = System.Text.RegularExpressions.Regex.Matches(content,
            @"<SYNC Start=(\d+)>\s*<P[^>]*>(.*?)(?=<SYNC|$)", 
            System.Text.RegularExpressions.RegexOptions.Singleline);

        for (int i = 0; i < matches.Count; i++)
        {
            var entry = new SrtEntry();
            entry.StartTime = TimeSpan.FromMilliseconds(int.Parse(matches[i].Groups[1].Value));
            entry.EndTime = i < matches.Count - 1 ? 
                TimeSpan.FromMilliseconds(int.Parse(matches[i + 1].Groups[1].Value)) :
                entry.StartTime + TimeSpan.FromSeconds(3);

            var subtitleText = matches[i].Groups[2].Value;
            subtitleText = System.Text.RegularExpressions.Regex.Replace(subtitleText, "<[^>]+>", "");
            subtitleText = System.Web.HttpUtility.HtmlDecode(subtitleText);
            entry.Text = subtitleText.Trim();

            if (!string.IsNullOrWhiteSpace(entry.Text))
            {
                subtitles.Add(entry);
            }
        }
    }

    private void ImportSSA(string[] lines)
    {
        bool inEvents = false;
        var formats = new Dictionary<int, string>();

        foreach (var line in lines)
        {
            if (line.StartsWith("[Events]"))
            {
                inEvents = true;
                continue;
            }

            if (inEvents)
            {
                if (line.StartsWith("Format:"))
                {
                    var parts = line.Substring(7).Split(',');
                    for (int i = 0; i < parts.Length; i++)
                    {
                        formats[i] = parts[i].Trim();
                    }
                }
                else if (line.StartsWith("Dialogue:"))
                {
                    var parts = line.Substring(9).Split(',');
                    if (parts.Length < 4) continue;

                    var entry = new SrtEntry();
                    entry.StartTime = TimeSpan.Parse(parts[1]);
                    entry.EndTime = TimeSpan.Parse(parts[2]);
                    entry.Text = string.Join(",", parts.Skip(formats.Count - 1))
                        .Replace("\\N", "\r\n")
                        .Replace("\\n", "\r\n");

                    subtitles.Add(entry);
                }
            }
        }
    }

    private void ImportWebVTT(string[] lines)
    {
        bool headerPassed = false;
        for (int i = 0; i < lines.Length; i++)
        {
            if (!headerPassed)
            {
                if (lines[i].StartsWith("WEBVTT"))
                {
                    headerPassed = true;
                    continue;
                }
            }

            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            var timeParts = lines[i].Split("-->");
            if (timeParts.Length != 2) continue;

            var entry = new SrtEntry();
            entry.StartTime = ParseVTTTime(timeParts[0]);
            entry.EndTime = ParseVTTTime(timeParts[1]);

            var textLines = new List<string>();
            i++;
            while (i < lines.Length && !string.IsNullOrWhiteSpace(lines[i]))
            {
                textLines.Add(lines[i]);
                i++;
            }
            entry.Text = string.Join("\r\n", textLines);
            subtitles.Add(entry);
        }
    }

    private TimeSpan ParseVTTTime(string time)
    {
        time = time.Trim();
        if (time.Contains('.'))
        {
            time = time.Replace('.', ',');
        }
        return ParseTimeStamp(time);
    }

    private void ExportSubViewer(StreamWriter writer)
    {
        writer.WriteLine("[INFORMATION]");
        writer.WriteLine("[TITLE]");
        writer.WriteLine("[AUTHOR]");
        writer.WriteLine("[SOURCE]");
        writer.WriteLine("[PRG]");
        writer.WriteLine("[FILEPATH]");
        writer.WriteLine("[DELAY]0");
        writer.WriteLine("[CD TRACK]0");
        writer.WriteLine("[COMMENT]");
        writer.WriteLine("[END INFORMATION]");
        writer.WriteLine("[SUBTITLE]");
        writer.WriteLine("[COLF]&HFFFFFF,[STYLE]bd,[SIZE]18,[FONT]Arial");

        foreach (var entry in subtitles)
        {
            writer.WriteLine($"{entry.StartTime:hh:mm:ss.fff},{entry.EndTime:hh:mm:ss.fff}");
            writer.WriteLine(entry.Text.Replace("\r\n", "|"));
            writer.WriteLine();
        }
    }

    private void ExportMicroDVD(StreamWriter writer)
    {
        const double fps = 23.976;
        foreach (var entry in subtitles)
        {
            int startFrame = (int)(entry.StartTime.TotalSeconds * fps);
            int endFrame = (int)(entry.EndTime.TotalSeconds * fps);
            writer.WriteLine($"{{{startFrame}}}{{{endFrame}}}{entry.Text.Replace("\r\n", "|")}");
        }
    }

    private void ExportSAMI(StreamWriter writer)
    {
        writer.WriteLine("<SAMI>");
        writer.WriteLine("<HEAD>");
        writer.WriteLine("<TITLE>Subtitle</TITLE>");
        writer.WriteLine("<SAMIParam>");
        writer.WriteLine("  Metrics {time:ms;}");
        writer.WriteLine("  Spec {MSFT:1.0;}");
        writer.WriteLine("</SAMIParam>");
        writer.WriteLine("<STYLE TYPE=\"text/css\">");
        writer.WriteLine("<!--");
        writer.WriteLine("  P { font-family: Arial; font-weight: normal; color: white; background-color: black; text-align: center; }");
        writer.WriteLine("  .ENCC { Name: English; lang: en-US; }");
        writer.WriteLine("-->");
        writer.WriteLine("</STYLE>");
        writer.WriteLine("</HEAD>");
        writer.WriteLine("<BODY>");

        foreach (var entry in subtitles)
        {
            writer.WriteLine($"<SYNC Start={(int)entry.StartTime.TotalMilliseconds}>");
            writer.WriteLine($"<P Class=ENCC>{System.Web.HttpUtility.HtmlEncode(entry.Text).Replace("\r\n", "<br>")}</P>");
            writer.WriteLine($"<SYNC Start={(int)entry.EndTime.TotalMilliseconds}>");
            writer.WriteLine("<P Class=ENCC>&nbsp;</P>");
        }

        writer.WriteLine("</BODY>");
        writer.WriteLine("</SAMI>");
    }

    private void ExportSSA(StreamWriter writer, string format)
    {
        writer.WriteLine($"[Script Info]");
        writer.WriteLine($"Title: Subtitle");
        writer.WriteLine($"ScriptType: v4.00{(format.ToLower() == "ass" ? "+" : "")}");
        writer.WriteLine($"Collisions: Normal");
        writer.WriteLine($"PlayResX: 384");
        writer.WriteLine($"PlayResY: 288");
        writer.WriteLine($"Timer: 100.0000");
        writer.WriteLine();
        writer.WriteLine($"[V4+ Styles]");
        writer.WriteLine($"Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding");
        writer.WriteLine($"Style: Default,Arial,20,&H00FFFFFF,&H000000FF,&H00000000,&H00000000,0,0,0,0,100,100,0,0,1,2,2,2,10,10,10,1");
        writer.WriteLine();
        writer.WriteLine($"[Events]");
        writer.WriteLine($"Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text");

        foreach (var entry in subtitles)
        {
            writer.WriteLine($"Dialogue: 0,{entry.StartTime:H:mm:ss.ff},{entry.EndTime:H:mm:ss.ff},Default,,0,0,0,,{entry.Text.Replace("\r\n", "\\N")}");
        }
    }

    private void ExportWebVTT(StreamWriter writer)
    {
        writer.WriteLine("WEBVTT");
        writer.WriteLine();

        foreach (var entry in subtitles)
        {
            writer.WriteLine($"{entry.Index}");
            writer.WriteLine($"{entry.StartTime:hh\\:mm\\:ss\\.fff} --> {entry.EndTime:hh\\:mm\\:ss\\.fff}");
            writer.WriteLine(entry.Text);
            writer.WriteLine();
        }
    }

    private void RemoveHearingImpairedText()
    {
        SaveState();
        var hiPatterns = new[]
        {
            @"\[.*?\]",
            @"\(.*?\)",
            @"♪.*?♪",
            @"#.*?#",
            @"\{.*?\}",
            @"<.*?>",
            @"\[.*$",
            @"\(.*$"
        };

        foreach (var entry in subtitles)
        {
            var subtitleText = entry.Text;
            foreach (var pattern in hiPatterns)
            {
                subtitleText = System.Text.RegularExpressions.Regex.Replace(subtitleText, pattern, "");
            }
            entry.Text = subtitleText.Trim();
        }
        RefreshListView();
    }

    private void MergeShortLines()
    {
        SaveState();
        const int minChars = 40; // Minimum characters for a "long enough" line

        for (int i = 0; i < subtitles.Count - 1; i++)
        {
            var current = subtitles[i];
            var next = subtitles[i + 1];

            if (current.Text.Length + next.Text.Length <= minChars &&
                (next.StartTime - current.EndTime).TotalSeconds <= 0.5)
            {
                current.Text += " " + next.Text;
                current.EndTime = next.EndTime;
                subtitles.RemoveAt(i + 1);
                i--;
            }
        }

        ReindexSubtitles();
        RefreshListView();
    }

    private void SplitLongLines()
    {
        SaveState();
        const int maxChars = 42; // Maximum characters per line
        var newSubtitles = new List<SrtEntry>();

        foreach (var entry in subtitles)
        {
            var lines = entry.Text.Split(new[] { "\r\n" }, StringSplitOptions.None);
            bool needsSplit = lines.Any(line => line.Length > maxChars);

            if (!needsSplit)
            {
                newSubtitles.Add(entry);
                continue;
            }

            var words = entry.Text.Split(' ');
            var currentLine = new System.Text.StringBuilder();
            var newLines = new List<string>();

            foreach (var word in words)
            {
                if (currentLine.Length + word.Length + 1 > maxChars && currentLine.Length > 0)
                {
                    newLines.Add(currentLine.ToString());
                    currentLine.Clear();
                }
                if (currentLine.Length > 0) currentLine.Append(' ');
                currentLine.Append(word);
            }
            if (currentLine.Length > 0)
                newLines.Add(currentLine.ToString());

            if (newLines.Count <= 2)
            {
                entry.Text = string.Join("\r\n", newLines);
                newSubtitles.Add(entry);
            }
            else
            {
                // Split into multiple entries
                var duration = (entry.EndTime - entry.StartTime).TotalSeconds;
                var timePerLine = duration / newLines.Count;

                for (int i = 0; i < newLines.Count; i += 2)
                {
                    var newEntry = new SrtEntry
                    {
                        StartTime = entry.StartTime + TimeSpan.FromSeconds(i * timePerLine),
                        EndTime = entry.StartTime + TimeSpan.FromSeconds((i + 2 > newLines.Count ? newLines.Count : i + 2) * timePerLine),
                        Text = string.Join("\r\n", newLines.Skip(i).Take(2))
                    };
                    newSubtitles.Add(newEntry);
                }
            }
        }

        subtitles = newSubtitles;
        ReindexSubtitles();
        RefreshListView();
    }

    private void AdjustDisplayTime()
    {
        var form = new InputForm("Adjust Display Time",
            "Enter minimum display time in seconds:\n(e.g., 1.5 for one and a half seconds)");
        
        if (form.ShowDialog() == DialogResult.OK && double.TryParse(form.Value, out double minSeconds))
        {
            SaveState();
            var minDuration = TimeSpan.FromSeconds(minSeconds);

            foreach (var entry in subtitles)
            {
                var duration = entry.EndTime - entry.StartTime;
                if (duration < minDuration)
                {
                    entry.EndTime = entry.StartTime + minDuration;
                }
            }

            RefreshListView();
        }
    }
} 