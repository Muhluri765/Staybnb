using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Staybnb.Models.Enums;

namespace Staybnb.Models
{
    public class Booking
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PropertyId { get; set; }

        [ForeignKey(nameof(PropertyId))]
        public virtual HostProperty? Property { get; set; }

        [Required]
        public string GuestId { get; set; } = string.Empty;

        [ForeignKey(nameof(GuestId))]
        public virtual ApplicationUser? Guest { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime CheckInDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime CheckOutDate { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        [Required]
        public BookingStatus Status { get; set; } = BookingStatus.Pending;

        // Navigation Properties
        public virtual Payment? Payment { get; set; }
        public virtual GuestCheckIn? GuestCheckIn { get; set; }

        [NotMapped]
        public int Nights => (CheckOutDate.Date - CheckInDate.Date).Days > 0 ? (CheckOutDate.Date - CheckInDate.Date).Days : 1;
    }
}
