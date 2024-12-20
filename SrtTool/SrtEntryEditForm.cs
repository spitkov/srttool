namespace SrtTool;

using System;
using System.Windows.Forms;
using System.Drawing;

public class SrtEntryEditForm : Form
{
    private TextBox startTimeTextBox = null!;
    private TextBox endTimeTextBox = null!;
    private TextBox textTextBox = null!;
    private Button okButton = null!;
    private Button cancelButton = null!;
    private Label startTimeLabel = null!;
    private Label endTimeLabel = null!;
    private Label textLabel = null!;

    public string StartTime 
    { 
        get => startTimeTextBox.Text;
        set => startTimeTextBox.Text = value;
    }
    
    public string EndTime 
    { 
        get => endTimeTextBox.Text;
        set => endTimeTextBox.Text = value;
    }
    
    public string SubtitleText 
    { 
        get => textTextBox.Text;
        set => textTextBox.Text = value;
    }

    public SrtEntryEditForm()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        startTimeLabel = new Label();
        startTimeTextBox = new TextBox();
        endTimeLabel = new Label();
        endTimeTextBox = new TextBox();
        textLabel = new Label();
        textTextBox = new TextBox();
        okButton = new Button();
        cancelButton = new Button();

        // Start Time
        startTimeLabel.Text = "Start Time (hh:mm:ss,fff):";
        startTimeLabel.Location = new Point(12, 15);
        startTimeLabel.AutoSize = true;

        startTimeTextBox.Location = new Point(12, 35);
        startTimeTextBox.Size = new Size(200, 23);

        // End Time
        endTimeLabel.Text = "End Time (hh:mm:ss,fff):";
        endTimeLabel.Location = new Point(12, 65);
        endTimeLabel.AutoSize = true;

        endTimeTextBox.Location = new Point(12, 85);
        endTimeTextBox.Size = new Size(200, 23);

        // Text
        textLabel.Text = "Subtitle Text:";
        textLabel.Location = new Point(12, 115);
        textLabel.AutoSize = true;

        textTextBox.Location = new Point(12, 135);
        textTextBox.Size = new Size(360, 100);
        textTextBox.Multiline = true;
        textTextBox.ScrollBars = ScrollBars.Vertical;

        // Buttons
        okButton.Text = "OK";
        okButton.DialogResult = DialogResult.OK;
        okButton.Location = new Point(197, 245);
        okButton.Size = new Size(75, 23);

        cancelButton.Text = "Cancel";
        cancelButton.DialogResult = DialogResult.Cancel;
        cancelButton.Location = new Point(297, 245);
        cancelButton.Size = new Size(75, 23);

        // Form
        AcceptButton = okButton;
        CancelButton = cancelButton;
        ClientSize = new Size(384, 281);
        Controls.AddRange(new Control[] {
            startTimeLabel,
            startTimeTextBox,
            endTimeLabel,
            endTimeTextBox,
            textLabel,
            textTextBox,
            okButton,
            cancelButton
        });
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Text = "Edit Subtitle";
        StartPosition = FormStartPosition.CenterParent;
    }
} 