using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArchiverInfrastructure.Entities;

/// <summary>
/// Сутність, що представляє файл архіву в базі даних.
/// </summary>
[Table("archives")]
public class Archive
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [MaxLength(500)]
    [Column("file_path")]
    public string FilePath { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    [Column("archive_type")]
    public string ArchiveType { get; set; } = string.Empty;

    [MaxLength(64)]
    [Column("checksum")]
    public string? Checksum { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("modified_at")]
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

    // Навігаційні властивості
    public virtual ICollection<Entry> Entries { get; set; } = new List<Entry>();
    public virtual ICollection<Operation> Operations { get; set; } = new List<Operation>();
}
