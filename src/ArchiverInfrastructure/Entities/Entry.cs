using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArchiverInfrastructure.Entities;

/// <summary>
/// Сутність, що представляє запис файлу в архіві.
/// </summary>
[Table("entries")]
public class Entry
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("archive_id")]
    public int ArchiveId { get; set; }

    [Required]
    [MaxLength(500)]
    [Column("file_name")]
    public string FileName { get; set; } = string.Empty;

    [Column("file_size")]
    public long FileSize { get; set; }

    [Column("modified_date")]
    public DateTime ModifiedDate { get; set; }

    [Column("is_directory")]
    public bool IsDirectory { get; set; }

    // Навігаційна властивість
    [ForeignKey("ArchiveId")]
    public virtual Archive Archive { get; set; } = null!;
}
