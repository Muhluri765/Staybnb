using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Staybnb.Models
{
    public class HostProperty
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 1000000.00)]
        public decimal PricePerNight { get; set; }

        [Required]
        public string HostId { get; set; } = string.Empty;

        [ForeignKey(nameof(HostId))]
        public virtual ApplicationUser? Host { get; set; }

        [Required]
        [StringLength(255)]
        public string Address { get; set; } = string.Empty;

        public bool IsActive { get; set; } = false;

        // Navigation Properties
        public virtual ICollection<PropertyImage> PropertyImages { get; set; } = new List<PropertyImage>();
        public virtual ICollection<Amenity> Amenities { get; set; } = new List<Amenity>();
        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
        public virtual ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();
        public virtual ICollection<HostApplication> HostApplications { get; set; } = new List<HostApplication>();
        public virtual CheckInProcess? CheckInProcess { get; set; }
    }
}
