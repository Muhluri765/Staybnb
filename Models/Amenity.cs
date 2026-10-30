using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Staybnb.Models
{
    public class Amenity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string IconClass { get; set; } = string.Empty;

        // Many-to-Many with HostProperty
        public virtual ICollection<HostProperty> Properties { get; set; } = new List<HostProperty>();
    }
}
