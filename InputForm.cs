namespace SrtTool;

using System;
using System.Windows.Forms;
using System.Drawing;

public class InputForm : Form
{
    private TextBox inputTextBox;
    private Button okButton;
    private Button cancelButton;
    private Label promptLabel;

    public string Value => inputTextBox.Text;

    public InputForm(string title, string prompt)
    {
        InitializeComponent();
        Text = title;
        promptLabel.Text = prompt;
    }

    private void InitializeComponent()
    {
        promptLabel = new Label();
        inputTextBox = new TextBox();
        okButton = new Button();
        cancelButton = new Button();

        // Prompt Label
        promptLabel.AutoSize = true;
        promptLabel.Location = new Point(12, 15);
        promptLabel.MaximumSize = new Size(300, 0);
        promptLabel.MinimumSize = new Size(300, 0);
        promptLabel.Font = new Font(promptLabel.Font.FontFamily, 9);

        // Input TextBox
        inputTextBox.Location = new Point(12, promptLabel.Bottom + 20);
        inputTextBox.Size = new Size(300, 25);
        inputTextBox.Font = new Font(inputTextBox.Font.FontFamily, 12);

        // OK Button
        okButton.DialogResult = DialogResult.OK;
        okButton.Location = new Point(147, inputTextBox.Bottom + 20);
        okButton.Size = new Size(80, 30);
        okButton.Text = "OK";

        // Cancel Button
        cancelButton.DialogResult = DialogResult.Cancel;
        cancelButton.Location = new Point(232, inputTextBox.Bottom + 20);
        cancelButton.Size = new Size(80, 30);
        cancelButton.Text = "Cancel";

        // Form
        AcceptButton = okButton;
        CancelButton = cancelButton;
        ClientSize = new Size(324, inputTextBox.Bottom + 70);
        MinimumSize = new Size(324, inputTextBox.Bottom + 70);
        Controls.AddRange(new Control[] {
            promptLabel,
            inputTextBox,
            okButton,
            cancelButton
        });
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;

        // Handle Enter key in TextBox
        inputTextBox.KeyPress += (s, e) => {
            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true;
                okButton.PerformClick();
            }
        };
    }
} 