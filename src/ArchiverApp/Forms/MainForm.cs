using ArchiverCore.Facade;
using ArchiverCore.Models;
using ArchiverInfrastructure.Data;
using ArchiverInfrastructure.Repositories;
using ArchiverInfrastructure.Logging;
using Microsoft.EntityFrameworkCore;

namespace ArchiverApp.Forms;

public partial class MainForm : Form
{
    private ArchiveManager? _archiveManager;
    private ArchiveRepository? _repository;
    private ArchiverDbContext? _dbContext;
    private FileLogger? _logger;
    private AppSettings _settings;
    private string? _currentArchivePath;
    private CancellationTokenSource? _cancellationTokenSource;

    // Елементи інтерфейсу
    private MenuStrip menuStrip;
    private ToolStrip toolStrip;
    private ListView listViewArchiveContents;
    private ProgressBar progressBar;
    private Button btnCancel;
    private StatusStrip statusStrip;
    private ToolStripStatusLabel statusLabel;
    private TextBox txtLog;
    private SplitContainer splitContainer;

    public MainForm()
    {
        InitializeComponent();
        LoadSettings();
        InitializeDatabase();
    }

    private void InitializeComponent()
    {
        this.Text = "Maxim Archiver";
        this.Size = new Size(1000, 700);
        this.StartPosition = FormStartPosition.CenterScreen;

        // Створити головний лайаут
        splitContainer = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 450
        };

        // Верхня панель - вміст архіву
        var topPanel = new Panel { Dock = DockStyle.Fill };
        
        listViewArchiveContents = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            MultiSelect = true
        };
        listViewArchiveContents.Columns.Add("Name", 300);
        listViewArchiveContents.Columns.Add("Size", 100);
        listViewArchiveContents.Columns.Add("Compressed", 100);
        listViewArchiveContents.Columns.Add("Modified", 150);
        listViewArchiveContents.Columns.Add("Type", 80);

        topPanel.Controls.Add(listViewArchiveContents);
        splitContainer.Panel1.Controls.Add(topPanel);

        // Нижня панель - лог і прогрес
        var bottomPanel = new Panel { Dock =DockStyle.Fill };
        
        txtLog = new TextBox
        {
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            ReadOnly = true,
            Dock = DockStyle.Fill,
            Font = new Font("Consolas", 9)
        };

        var progressPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 40
        };

        progressBar = new ProgressBar
        {
            Dock = DockStyle.Fill,
            Visible = false
        };

        btnCancel = new Button
        {
            Text = "Cancel",
            Dock = DockStyle.Right,
            Width = 80,
            Visible = false
        };
        btnCancel.Click += BtnCancel_Click;

        progressPanel.Controls.Add(progressBar);
        progressPanel.Controls.Add(btnCancel);

        bottomPanel.Controls.Add(txtLog);
        bottomPanel.Controls.Add(progressPanel);
        splitContainer.Panel2.Controls.Add(bottomPanel);

        // Меню
        menuStrip = new MenuStrip();
        
        var fileMenu = new ToolStripMenuItem("File");
        fileMenu.DropDownItems.Add("Settings", null, Settings_Click);
        fileMenu.DropDownItems.Add("Exit", null, (s, e) => Application.Exit());
        
        var archiveMenu = new ToolStripMenuItem("Archive");
        archiveMenu.DropDownItems.Add("Create Archive", null, CreateArchive_Click);
        archiveMenu.DropDownItems.Add("Open Archive", null, OpenArchive_Click);
        archiveMenu.DropDownItems.Add("Close Archive", null, CloseArchive_Click);
        archiveMenu.DropDownItems.Add(new ToolStripSeparator());
        archiveMenu.DropDownItems.Add("Add Files", null, AddFiles_Click);
        archiveMenu.DropDownItems.Add("Delete Files", null, DeleteFiles_Click);
        archiveMenu.DropDownItems.Add("Extract All", null, ExtractAll_Click);
        archiveMenu.DropDownItems.Add(new ToolStripSeparator());
        archiveMenu.DropDownItems.Add("Archive Info...", null, ArchiveInfo_Click);
        archiveMenu.DropDownItems.Add("Delete Archive from Database", null, DeleteArchive_Click);
        
        var toolsMenu = new ToolStripMenuItem("Tools");
        toolsMenu.DropDownItems.Add("Test Archive", null, TestArchive_Click);
        toolsMenu.DropDownItems.Add("Calculate Checksum", null, CalculateChecksum_Click);
        toolsMenu.DropDownItems.Add(new ToolStripSeparator());
        toolsMenu.DropDownItems.Add("Split Archive", null, SplitArchive_Click);
        toolsMenu.DropDownItems.Add("Merge Volumes", null, MergeVolumes_Click);
        toolsMenu.DropDownItems.Add(new ToolStripSeparator());
        toolsMenu.DropDownItems.Add("View Operations Log", null, ViewLog_Click);

        menuStrip.Items.Add(fileMenu);
        menuStrip.Items.Add(archiveMenu);
        menuStrip.Items.Add(toolsMenu);

        // Панель інструментів
        toolStrip = new ToolStrip();
        toolStrip.Items.Add(new ToolStripButton("Create", null, CreateArchive_Click));
        toolStrip.Items.Add(new ToolStripButton("Open", null, OpenArchive_Click));
        toolStrip.Items.Add(new ToolStripSeparator());
        toolStrip.Items.Add(new ToolStripButton("Add Files", null, AddFiles_Click));
        toolStrip.Items.Add(new ToolStripButton("Delete", null, DeleteFiles_Click));
        toolStrip.Items.Add(new ToolStripSeparator());
        toolStrip.Items.Add(new ToolStripButton("Test", null, TestArchive_Click));
        toolStrip.Items.Add(new ToolStripButton("Extract", null, ExtractAll_Click));

        // Рядок стану
        statusStrip = new StatusStrip();
        statusLabel = new ToolStripStatusLabel("Ready");
        statusStrip.Items.Add(statusLabel);

        // Додати елементи керування до форми
        this.Controls.Add(splitContainer);
        this.Controls.Add(toolStrip);
        this.Controls.Add(menuStrip);
        this.Controls.Add(statusStrip);
        this.MainMenuStrip = menuStrip;
    }

    private void LoadSettings()
    {
        _settings = AppSettings.Load();
        _logger = new FileLogger(_settings.LogDirectory);
        Log("Application started");
    }

    private void InitializeDatabase()
    {
        try
        {
            // Звільнити існуючий контекст, якщо є
            _dbContext?.Dispose();

            var optionsBuilder = new DbContextOptionsBuilder<ArchiverDbContext>();
            optionsBuilder.UseNpgsql(_settings.ConnectionString);

            _dbContext = new ArchiverDbContext(optionsBuilder.Options);
            _dbContext.Database.EnsureCreated();

            _repository = new ArchiveRepository(_dbContext);
            _archiveManager = new ArchiveManager(_repository);

            Log("Database connection established");
        }
        catch (Exception ex)
        {
            Log($"Database connection failed: {ex.Message}");
            MessageBox.Show("Database connection failed. Some features may not work. Please check settings.",
                "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            
            // Створюємо ArchiveManager без репозиторія, якщо база даних недоступна
            _archiveManager = new ArchiveManager(null);
        }
    }

    private async void CreateArchive_Click(object? sender, EventArgs e)
    {
        using var folderDialog = new FolderBrowserDialog { Description = "Select folder to archive" };
        if (folderDialog.ShowDialog() != DialogResult.OK) return;

        using var saveDialog = new SaveFileDialog
        {
            Filter = "ZIP Archive|*.zip|TAR Archive|*.tar|TAR.GZ Archive|*.tar.gz",
            Title = "Create Archive"
        };

        if (saveDialog.ShowDialog() != DialogResult.OK) return;

        try
        {
            var files = Directory.GetFiles(folderDialog.SelectedPath, "*", SearchOption.AllDirectories);
            await ExecuteOperationAsync(async (progress, token) =>
            {
                var checksum = await _archiveManager!.CreateArchiveAsync(
                    saveDialog.FileName, files, progress, token);
                
                Log($"Archive created: {saveDialog.FileName}");
                Log($"Checksum: {checksum}");
                
                _currentArchivePath = saveDialog.FileName;
                await LoadArchiveContents();
            });

            MessageBox.Show("Archive created successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Log($"Error creating archive: {ex.Message}");
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void OpenArchive_Click(object? sender, EventArgs e)
    {
        using var openDialog = new OpenFileDialog
        {
            Filter = "Archive Files|*.zip;*.tar;*.tar.gz|All Files|*.*",
            Title = "Open Archive"
        };

        if (openDialog.ShowDialog() != DialogResult.OK) return;

        _currentArchivePath = openDialog.FileName;
        await LoadArchiveContents();
    }

    private void CloseArchive_Click(object? sender, EventArgs e)
    {
        _currentArchivePath = null;
        listViewArchiveContents.Items.Clear();
        statusLabel.Text = "Archive closed";
        Log("Archive closed");
    }

    private async void AddFiles_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_currentArchivePath))
        {
            MessageBox.Show("Please open an archive first.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var openDialog = new OpenFileDialog
        {
            Multiselect = true,
            Title = "Select files to add"
        };

        if (openDialog.ShowDialog() != DialogResult.OK) return;

        try
        {
            await ExecuteOperationAsync(async (progress, token) =>
            {
                var checksum = await _archiveManager!.AddFilesToArchiveAsync(
                    _currentArchivePath!, openDialog.FileNames, progress, token);
                
                Log($"Added {openDialog.FileNames.Length} files");
                Log($"New checksum: {checksum}");

                await LoadArchiveContents();
            });

            MessageBox.Show("Files added successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Log($"Error adding files: {ex.Message}");
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void DeleteFiles_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_currentArchivePath))
        {
            MessageBox.Show("Please open an archive first.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (listViewArchiveContents.SelectedItems.Count == 0)
        {
            MessageBox.Show("Please select files to delete.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var result = MessageBox.Show($"Delete {listViewArchiveContents.SelectedItems.Count} file(s)?",
            "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        if (result != DialogResult.Yes) return;

        try
        {
            var fileNames = listViewArchiveContents.SelectedItems
                .Cast<ListViewItem>()
                .Select(item => item.Text)
                .ToList();

            await ExecuteOperationAsync(async (progress, token) =>
            {
                var checksum = await _archiveManager!.DeleteFromArchiveAsync(
                    _currentArchivePath!, fileNames, progress, token);
                
                Log($"Deleted {fileNames.Count} files");
                Log($"New checksum: {checksum}");

                await LoadArchiveContents();
            });

            MessageBox.Show("Files deleted successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Log($"Error deleting files: {ex.Message}");
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void ExtractAll_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_currentArchivePath))
        {
            MessageBox.Show("Please open an archive first.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var folderDialog = new FolderBrowserDialog { Description = "Select extraction folder" };
        if (folderDialog.ShowDialog() != DialogResult.OK) return;

        try
        {
            await ExecuteOperationAsync(async (progress, token) =>
            {
                await _archiveManager!.ExtractArchiveAsync(
                    _currentArchivePath!, folderDialog.SelectedPath, progress, token);
                
                Log($"Extracted to: {folderDialog.SelectedPath}");
            });

            MessageBox.Show("Archive extracted successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Log($"Error extracting archive: {ex.Message}");
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void TestArchive_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_currentArchivePath))
        {
            MessageBox.Show("Please open an archive first.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            TestResult? testResult = null;
            await ExecuteOperationAsync(async (progress, token) =>
            {
                testResult = await _archiveManager!.TestArchiveAsync(_currentArchivePath!, progress, token);
            });

            if (testResult != null)
            {
                Log(testResult.ToString());
                MessageBox.Show(testResult.ToString(),
                    testResult.IsValid ? "Test Passed" : "Test Failed",
                    MessageBoxButtons.OK,
                    testResult.IsValid ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            Log($"Error testing archive: {ex.Message}");
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void CalculateChecksum_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_currentArchivePath))
        {
            MessageBox.Show("Please open an archive first.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            string? checksum = null;
            await ExecuteOperationAsync(async (progress, token) =>
            {
                checksum = await _archiveManager!.CalculateChecksumAsync(_currentArchivePath!, token);
                Log($"Checksum: {checksum}");
            });

            if (checksum != null)
            {
                MessageBox.Show($"SHA256 Checksum:\n{checksum}", "Checksum",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            Log($"Error calculating checksum: {ex.Message}");
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void SplitArchive_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_currentArchivePath))
        {
            MessageBox.Show("Please open an archive first.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var sizeInput = Microsoft.VisualBasic.Interaction.InputBox(
            "Enter volume size in MB:", "Split Archive", _settings.DefaultVolumeSizeMB.ToString());

        if (string.IsNullOrEmpty(sizeInput) || !int.TryParse(sizeInput, out int sizeMB))
            return;

        try
        {
            List<string>? volumes = null;
            await ExecuteOperationAsync(async (progress, token) =>
            {
                long sizeBytes = (long)sizeMB * 1024 * 1024;
                volumes = await _archiveManager!.SplitArchiveAsync(_currentArchivePath!, sizeBytes, progress, token);
                
                Log($"Archive split into {volumes.Count} volumes");
            });

            if (volumes != null)
            {
                MessageBox.Show($"Archive split into {volumes.Count} volumes.", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            Log($"Error splitting archive: {ex.Message}");
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void MergeVolumes_Click(object? sender, EventArgs e)
    {
        using var openDialog = new OpenFileDialog
        {
            Filter = "Volume Files|*.part001",
            Title = "Select first volume (.part001)"
        };

        if (openDialog.ShowDialog() != DialogResult.OK) return;

        try
        {
            await ExecuteOperationAsync(async (progress, token) =>
            {
                await _archiveManager!.MergeArchiveAsync(openDialog.FileName, null, progress, token);
                
                var mergedPath = openDialog.FileName.Replace(".part001", "");
                Log($"Volumes merged to: {mergedPath}");
            });

            MessageBox.Show("Volumes merged successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Log($"Error merging volumes: {ex.Message}");
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ViewLog_Click(object? sender, EventArgs e)
    {
        var logForm = new LogViewerForm(_repository);
        logForm.ShowDialog();
    }

    private void Settings_Click(object? sender, EventArgs e)
    {
        var settingsForm = new SettingsForm(_settings);
        if (settingsForm.ShowDialog() == DialogResult.OK)
        {
            _settings = settingsForm.Settings;
            _settings.Save();
            InitializeDatabase();
            Log("Settings updated");
        }
    }

    private async void ArchiveInfo_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_currentArchivePath))
        {
            MessageBox.Show("Please open an archive first.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            var infoForm = new ArchiveInfoForm(_currentArchivePath, _repository, _archiveManager);
            infoForm.ShowDialog();
        }
        catch (Exception ex)
        {
            Log($"Error showing archive info: {ex.Message}");
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void DeleteArchive_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_currentArchivePath))
        {
            MessageBox.Show("Please open an archive first.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var result = MessageBox.Show(
            $"Delete archive metadata from database?\n\nArchive: {Path.GetFileName(_currentArchivePath)}\n\nNote: This will NOT delete the physical archive file, only the database records.",
            "Delete from Database",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result != DialogResult.Yes) return;

        try
        {
            if (_repository != null)
            {
                await _repository.DeleteArchiveAsync(_currentArchivePath!);
                Log($"Archive metadata deleted from database: {_currentArchivePath}");
                MessageBox.Show("Database records deleted successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Database not available.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            Log($"Error deleting archive metadata: {ex.Message}");
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task LoadArchiveContents()
    {
        if (string.IsNullOrEmpty(_currentArchivePath) || !File.Exists(_currentArchivePath))
            return;

        try
        {
            listViewArchiveContents.Items.Clear();
            
            var entries = await _archiveManager!.GetArchiveEntriesAsync(_currentArchivePath!);
            
            foreach (var entry in entries)
            {
                var item = new ListViewItem(entry.FullName);
                item.SubItems.Add(FormatSize(entry.UncompressedSize));
                item.SubItems.Add(FormatSize(entry.CompressedSize));
                item.SubItems.Add(entry.LastModified.ToString("yyyy-MM-dd HH:mm:ss"));
                item.SubItems.Add(entry.IsDirectory ? "Folder" : "File");
                listViewArchiveContents.Items.Add(item);
            }

            statusLabel.Text = $"Archive: {Path.GetFileName(_currentArchivePath)} ({entries.Count()} items)";
            Log($"Loaded archive: {_currentArchivePath}");
        }
        catch (Exception ex)
        {
            Log($"Error loading archive contents: {ex.Message}");
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task ExecuteOperationAsync(Func<IProgress<int>, CancellationToken, Task> operation)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        var progress = new Progress<int>(percent =>
        {
            if (InvokeRequired)
            {
                Invoke(() => progressBar.Value = Math.Min(percent, 100));
            }
            else
            {
                progressBar.Value = Math.Min(percent, 100);
            }
        });

        progressBar.Visible = true;
        btnCancel.Visible = true;
        progressBar.Value = 0;
        toolStrip.Enabled = false;
        menuStrip.Enabled = false;

        try
        {
            await operation(progress, _cancellationTokenSource.Token);
        }
        finally
        {
            progressBar.Visible = false;
            btnCancel.Visible = false;
            toolStrip.Enabled = true;
            menuStrip.Enabled = true;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    private void BtnCancel_Click(object? sender, EventArgs e)
    {
        _cancellationTokenSource?.Cancel();
        Log("Operation cancelled by user");
    }

    private void Log(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var logMessage = $"[{timestamp}] {message}";
        
        if (InvokeRequired)
        {
            Invoke(() =>
            {
                txtLog.AppendText(logMessage + Environment.NewLine);
                txtLog.SelectionStart = txtLog.Text.Length;
                txtLog.ScrollToCaret();
            });
        }
        else
        {
            txtLog.AppendText(logMessage + Environment.NewLine);
            txtLog.SelectionStart = txtLog.Text.Length;
            txtLog.ScrollToCaret();
        }

        _logger?.Info(message);
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

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _cancellationTokenSource?.Cancel();
        _dbContext?.Dispose();
        _logger?.Info("Application closed");
        base.OnFormClosing(e);
    }
}
