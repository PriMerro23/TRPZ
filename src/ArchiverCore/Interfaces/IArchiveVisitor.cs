namespace ArchiverCore.Interfaces;

/// <summary>
/// Інтерфейс патерну Відвідувач для виконання операцій над записами архіву.
/// Дозволяє додавати нові операції без зміни класів записів.
/// </summary>
public interface IArchiveVisitor 
{
    /// <summary>
    /// Відвідує запис архіву та виконує операцію.
    /// </summary>
    /// <param name="entry">Метадані запису архіву</param>
    /// <param name="contentStream">Потік для читання вмісту запису (може бути null для директорій)</param>
    void Visit(Models.ArchiveEntry entry, Stream? contentStream);

    /// <summary>
    /// Повертає результат операції відвідувача (формат залежить від реалізації відвідувача).
    /// </summary>
    object GetResult();
}
