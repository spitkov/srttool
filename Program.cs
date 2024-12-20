using System;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Diagnostics;
using System.Drawing;
using System.IO;

namespace SrtTool;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        // Set default icon for all forms
        try
        {
            var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "512.ico");
            if (File.Exists(iconPath))
            {
                using var icon = new Icon(iconPath);
                Application.OpenForms.Cast<Form>().ToList().ForEach(form => form.Icon = icon);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load icon: {ex.Message}");
        }

        // Register file association if requested
        if (args.Length > 0 && args[0].Equals("--register", StringComparison.OrdinalIgnoreCase))
        {
            RegisterFileAssociation();
            return;
        }

        // Create main form with args (for file opening)
        var mainForm = new MainForm();
        
        // If a file was passed as argument, open it
        if (args.Length > 0 && System.IO.File.Exists(args[0]))
        {
            mainForm.OpenFile(args[0]);
        }

        Application.Run(mainForm);
    }

    private static void RegisterFileAssociation()
    {
        try
        {
            var process = Process.GetCurrentProcess();
            var executablePath = Application.ExecutablePath;
            string fileType = ".srt";

            // Create file type registry key
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\" + fileType))
            {
                key.SetValue("", "SrtTool.SrtFile");
            }

            // Create progID registry key
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\SrtTool.SrtFile"))
            {
                key.SetValue("", "SRT Subtitle File");
                key.CreateSubKey("DefaultIcon").SetValue("", $"\"{executablePath}\",0");

                using (RegistryKey shellKey = key.CreateSubKey("shell"))
                using (RegistryKey openKey = shellKey.CreateSubKey("open"))
                using (RegistryKey commandKey = openKey.CreateSubKey("command"))
                {
                    commandKey.SetValue("", $"\"{executablePath}\" \"%1\"");
                }
            }

            MessageBox.Show("SrtTool has been registered as a handler for .srt files.", 
                "Registration Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error registering file association: {ex.Message}\n\nTry running the application as administrator.", 
                "Registration Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}