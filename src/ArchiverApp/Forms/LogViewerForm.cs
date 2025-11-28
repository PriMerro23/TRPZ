using ArchiverInfrastructure.Repositories;

namespace ArchiverApp.Forms;

public partial class LogViewerForm : Form
{
    private readonly ArchiveRepository? _repository;
    private ListView listView;
    private Button btnRefresh;
    private Button btnClose;

    public LogViewerForm(ArchiveRepository? repository)
    {
        _repository = repository;
        InitializeComponent();
        LoadOperations();
    }

    private void InitializeComponent()
    {
        this.Text = "Operations Log";
        this.Size = new Size(800, 500);
        this.StartPosition = FormStartPosition.CenterParent;

        listView = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true
        };

        listView.Columns.Add("Time", 150);
        listView.Columns.Add("Archive", 250);
        listView.Columns.Add("Operation", 100);
        listView.Columns.Add("Result", 80);
        listView.Columns.Add("Metadata", 200);

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
        btnRefresh.Click += (s, e) => LoadOperations();

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
        this.Controls.Add(buttonPanel);
    }

    private async void LoadOperations()
    {
        if (_repository == null)
        {
            MessageBox.Show("Database not available.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            listView.Items.Clear();
            var operations = await _repository.GetRecentOperationsAsync(null, 100);

            foreach (var op in operations)
            {
                var item = new ListViewItem(op.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
                item.SubItems.Add(op.Archive?.FilePath ?? "N/A");
                item.SubItems.Add(op.OperationType);
                item.SubItems.Add(op.Result);
                item.SubItems.Add(op.Metadata ?? "");
                listView.Items.Add(item);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading operations: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
