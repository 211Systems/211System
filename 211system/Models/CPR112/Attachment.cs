using System;
using System.ComponentModel.DataAnnotations;

namespace CPR112.Models;

public class Attachment
{
    [Key]
    public Guid Id { get; set; }

    public string PathToFile { get; set; }

    [MaxLength(255)]
    public string FileName { get; set; }

    [MaxLength(100)]
    public string ContentType { get; set; }

    public long FileSizeBytes { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public Guid IncidentId { get; set; }
    public Incident Incident { get; set; }
}