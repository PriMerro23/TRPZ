using ArchiverCore.Facade;
using ArchiverInfrastructure.Repositories;

namespace ArchiverApp.Forms;

public partial class ArchiveInfoForm : Form
{
    private readonly string _archivePath;
    private readonly ArchiveRepository? _repository;
    private readonly ArchiveManager? _archiveManager;

    private ListView listView;
    private TextBox txtInfo;
    private Button btnRefresh;
    private Button btnClose;

    public ArchiveInfoForm(string archivePath, ArchiveRepository? repository, ArchiveManager? archiveManager)
    {
        _archivePath = archivePath;
        _repository = repository;
        _archiveManager = archiveManager;
        InitializeComponent();
        LoadArchiveInfo();
    }

    private void InitializeComponent()
    {
        this.Text = $"Archive Info - {Path.GetFileName(_archivePath)}";
        this.Size = new Size(700, 550);
        this.StartPosition = FormStartPosition.CenterParent;

        // Текстове поле інформації
        txtInfo = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            Dock = DockStyle.Top,
            Height = 150,
            Font = new Font("Consolas", 9),
            ScrollBars = ScrollBars.Vertical
        };

        // Список записів
        var label = new Label
        {
            Text = "Archive Entries (from database):",
            Dock = DockStyle.Top,
            Height = 25,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(5, 5, 5, 0)
        };

        listView = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true
        };

        listView.Columns.Add("File Name", 300);
        listView.Columns.Add("Size", 100);
        listView.Columns.Add("Modified", 150);
        listView.Columns.Add("Type", 80);

        var buttonPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 50
        };

        btnRefresh = new Button
        {
            Text = "Refresh",
            Location = new Point(10, 10),
            Width = 100
        };
        btnRefresh.Click += (s, e) => LoadArchiveInfo();

        btnClose = new Button
        {
            Text = "Close",
            Location = new Point(120, 10),
            Width = 100
        };
        btnClose.Click += (s, e) => this.Close();

        buttonPanel.Controls.Add(btnRefresh);
        buttonPanel.Controls.Add(btnClose);

        this.Controls.Add(listView);
        this.Controls.Add(label);
        this.Controls.Add(txtInfo);
        this.Controls.Add(buttonPanel);
    }

    private async void LoadArchiveInfo()
    {
        if (_repository == null)
        {
            txtInfo.Text = "Database not available.";
            return;
        }

        try
        {
            var archive = await _repository.GetArchiveByPathAsync(_archivePath);

            if (archive == null)
            {
                txtInfo.Text = $"Archive Path: {_archivePath}\r\n" +
                              $"Status: Not found in database\r\n" +
                              $"\r\n" +
                              $"This archive has not been indexed in the database yet.\r\n" +
                              $"Perform any operation (e.g., recalculate checksum) to add it.";
                listView.Items.Clear();
                return;
            }

            // Сформувати текст інформації
            var infoText = $"Archive Path: {archive.FilePath}\r\n" +
                          $"Archive Type: {archive.ArchiveType}\r\n" +
                          $"Checksum (SHA256): {archive.Checksum ?? "Not calculated"}\r\n" +
                          $"Created At: {archive.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC\r\n" +
                          $"Modified At: {archive.ModifiedAt:yyyy-MM-dd HH:mm:ss} UTC\r\n" +
                          $"Total Entries in DB: {archive.Entries.Count}\r\n" +
                          $"Total Operations: {archive.Operations.Count}";

            // Обчислити поточну контрольну суму для порівняння
            if (_archiveManager != null && File.Exists(_archivePath))
            {
                try
                {
                    var currentChecksum = await _archiveManager.CalculateChecksumAsync(_archivePath);
                    infoText += $"\r\n\r\nCurrent Checksum: {currentChecksum}";
                    
                    if (archive.Checksum != null && archive.Checksum != currentChecksum)
                    {
                        infoText += "\r\n⚠️ WARNING: Stored and current checksums DO NOT match!";
                        infoText += "\r\nThe archive may have been modified outside the application.";
                    }
                    else if (archive.Checksum == currentChecksum)
                    {
                        infoText += "\r\n✓ Checksums match - archive integrity confirmed";
                    }
                }
                catch (Exception ex)
                {
                    infoText += $"\r\n\r\nError calculating current checksum: {ex.Message}";
                }
            }

            txtInfo.Text = infoText;

            // Завантажити записи
            listView.Items.Clear();
            foreach (var entry in archive.Entries.OrderBy(e => e.FileName))
            {
                var item = new ListViewItem(entry.FileName);
                item.SubItems.Add(FormatSize(entry.FileSize));
                item.SubItems.Add(entry.ModifiedDate.ToString("yyyy-MM-dd HH:mm:ss"));
                item.SubItems.Add(entry.IsDirectory ? "Folder" : "File");
                listView.Items.Add(item);
            }
        }
        catch (Exception ex)
        {
            txtInfo.Text = $"Error loading archive info: {ex.Message}";
            MessageBox.Show($"Error: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
