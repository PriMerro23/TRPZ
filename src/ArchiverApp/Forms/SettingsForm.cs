using Microsoft.EntityFrameworkCore;

namespace ArchiverApp.Forms;

public partial class SettingsForm : Form
{
    public AppSettings Settings { get; private set; }

    private TextBox txtConnectionString;
    private TextBox txtLogDirectory;
    private NumericUpDown numVolumeSizeMB;
    private Button btnOK;
    private Button btnCancel;
    private Button btnTest;

    public SettingsForm(AppSettings settings)
    {
        Settings = new AppSettings
        {
            ConnectionString = settings.ConnectionString,
            LogDirectory = settings.LogDirectory,
            DefaultVolumeSizeMB = settings.DefaultVolumeSizeMB
        };

        InitializeComponent();
        LoadSettings();
    }

    private void InitializeComponent()
    {
        this.Text = "Settings";
        this.Size = new Size(600, 300);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;

        int y = 20;

        // Рядок підключення
        var lblConnection = new Label
        {
            Text = "PostgreSQL Connection String:",
            Location = new Point(20, y),
            AutoSize = true
        };
        this.Controls.Add(lblConnection);

        y += 25;
        txtConnectionString = new TextBox
        {
            Location = new Point(20, y),
            Width = 540,
            Text = Settings.ConnectionString
        };
        this.Controls.Add(txtConnectionString);

        y += 40;

        // Директорія логів
        var lblLog = new Label
        {
            Text = "Log Directory:",
            Location = new Point(20, y),
            AutoSize = true
        };
        this.Controls.Add(lblLog);

        y += 25;
        txtLogDirectory = new TextBox
        {
            Location = new Point(20, y),
            Width = 540,
            Text = Settings.LogDirectory
        };
        this.Controls.Add(txtLogDirectory);

        y += 40;

        // Розмір тому за замовчуванням
        var lblVolume = new Label
        {
            Text = "Default Volume Size (MB):",
            Location = new Point(20, y),
            AutoSize = true
        };
        this.Controls.Add(lblVolume);

        y += 25;
        numVolumeSizeMB = new NumericUpDown
        {
            Location = new Point(20, y),
            Width = 150,
            Minimum = 1,
            Maximum = 10000,
            Value = Settings.DefaultVolumeSizeMB
        };
        this.Controls.Add(numVolumeSizeMB);

        // Кнопки
        y = 220;
        btnTest = new Button
        {
            Text = "Test Connection",
            Location = new Point(20, y),
            Width = 120
        };
        btnTest.Click += BtnTest_Click;
        this.Controls.Add(btnTest);

        btnOK = new Button
        {
            Text = "OK",
            Location = new Point(360, y),
            Width = 100,
            DialogResult = DialogResult.OK
        };
        btnOK.Click += BtnOK_Click;
        this.Controls.Add(btnOK);

        btnCancel = new Button
        {
            Text = "Cancel",
            Location = new Point(470, y),
            Width = 100,
            DialogResult = DialogResult.Cancel
        };
        this.Controls.Add(btnCancel);

        this.AcceptButton = btnOK;
        this.CancelButton = btnCancel;
    }

    private void LoadSettings()
    {
        txtConnectionString.Text = Settings.ConnectionString;
        txtLogDirectory.Text = Settings.LogDirectory;
        numVolumeSizeMB.Value = Settings.DefaultVolumeSizeMB;
    }

    private void BtnOK_Click(object? sender, EventArgs e)
    {
        Settings.ConnectionString = txtConnectionString.Text;
        Settings.LogDirectory = txtLogDirectory.Text;
        Settings.DefaultVolumeSizeMB = (int)numVolumeSizeMB.Value;
    }

    private async void BtnTest_Click(object? sender, EventArgs e)
    {
        btnTest.Enabled = false;
        try
        {
            using var context = new ArchiverInfrastructure.Data.ArchiverDbContext(
                new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<ArchiverInfrastructure.Data.ArchiverDbContext>()
                    .UseNpgsql(txtConnectionString.Text)
                    .Options);

            await context.Database.CanConnectAsync();
            MessageBox.Show("Connection successful!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Connection failed:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnTest.Enabled = true;
        }
    }
}
