using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArchiverInfrastructure.Entities;

/// <summary>
/// Сутність, що представляє операцію, виконану над архівом (журнал аудиту).
/// </summary>
[Table("operations")]
public class Operation
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("archive_id")]
    public int? ArchiveId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("operation_type")]
    public string OperationType { get; set; } = string.Empty;

    [Column("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [Required]
    [MaxLength(50)]
    [Column("result")]
    public string Result { get; set; } = string.Empty;

    [Column("metadata")]
    public string? Metadata { get; set; }

    // Навігаційна властивість
    [ForeignKey("ArchiveId")]
    public virtual Archive? Archive { get; set; }
}
