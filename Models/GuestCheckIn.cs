using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Staybnb.Models.Enums;

namespace Staybnb.Models
{
    public class GuestCheckIn
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BookingId { get; set; }

        [ForeignKey(nameof(BookingId))]
        public virtual Booking? Booking { get; set; }

        [Required]
        public int CheckInProcessId { get; set; }

        [ForeignKey(nameof(CheckInProcessId))]
        public virtual CheckInProcess? CheckInProcess { get; set; }

        [Required]
        public CheckInStatus Status { get; set; } = CheckInStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(500)]
        public string? HostRemarks { get; set; }

        // Navigation Properties
        public virtual ICollection<GuestDocument> GuestDocuments { get; set; } = new List<GuestDocument>();
    }
}
