using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Staybnb.Models.Enums;

namespace Staybnb.Models
{
    public class GuestDocument
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int GuestCheckInId { get; set; }

        [ForeignKey(nameof(GuestCheckInId))]
        public virtual GuestCheckIn? GuestCheckIn { get; set; }

        [Required]
        [StringLength(100)]
        public string DocumentType { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        public DocumentStatus Status { get; set; } = DocumentStatus.Pending;

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}
