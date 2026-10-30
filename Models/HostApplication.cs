using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Staybnb.Models.Enums;

namespace Staybnb.Models
{
    public class HostApplication
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ApplicationUserId { get; set; } = string.Empty;

        [ForeignKey(nameof(ApplicationUserId))]
        public virtual ApplicationUser? ApplicationUser { get; set; }

        [Required]
        public int PropertyId { get; set; }

        [ForeignKey(nameof(PropertyId))]
        public virtual HostProperty? Property { get; set; }

        [Required]
        public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;

        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;

        [StringLength(500)]
        public string? AdminNotes { get; set; }
    }
}
