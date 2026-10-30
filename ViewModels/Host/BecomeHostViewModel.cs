using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Staybnb.Models;

namespace Staybnb.ViewModels.Host
{
    public class BecomeHostViewModel
    {
        [Required(ErrorMessage = "Property title is required")]
        [StringLength(100, ErrorMessage = "Title cannot exceed 100 characters")]
        [Display(Name = "Listing Title")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Price per night is required")]
        [Range(50, 200000, ErrorMessage = "Price must be between R50 and R200,000 per night")]
        [Display(Name = "Price Per Night (ZAR)")]
        public decimal PricePerNight { get; set; }

        [Required(ErrorMessage = "Property address is required")]
        [StringLength(255)]
        [Display(Name = "Full Street Address")]
        public string Address { get; set; } = string.Empty;

        [Display(Name = "Amenities")]
        public List<int> SelectedAmenityIds { get; set; } = new List<int>();

        public List<Amenity> AvailableAmenities { get; set; } = new List<Amenity>();

        [Required(ErrorMessage = "At least one property photo is mandatory for host application submission.")]
        [Display(Name = "Upload Property Photos")]
        public List<IFormFile> ImageFiles { get; set; } = new List<IFormFile>();
    }
}
