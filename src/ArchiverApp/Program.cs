namespace ArchiverApp;

static class Program
{
    /// <summary>
    ///  Головна точка входу для додатку.
    /// </summary>
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new Forms.MainForm());
    }
}
