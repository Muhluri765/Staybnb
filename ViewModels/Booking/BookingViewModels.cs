using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Staybnb.Models;
using Staybnb.Models.Enums;

namespace Staybnb.ViewModels.Booking
{
    public class BookingCheckoutViewModel
    {
        public int PropertyId { get; set; }
        public string PropertyTitle { get; set; } = string.Empty;
        public string PropertyAddress { get; set; } = string.Empty;
        public string? PropertyImageUrl { get; set; }
        public string HostName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Check-in date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Check-in Date")]
        public DateTime CheckInDate { get; set; } = DateTime.Today.AddDays(1);

        [Required(ErrorMessage = "Check-out date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Check-out Date")]
        public DateTime CheckOutDate { get; set; } = DateTime.Today.AddDays(3);

        public decimal PricePerNight { get; set; }
        public int Nights => (CheckOutDate.Date - CheckInDate.Date).Days > 0 ? (CheckOutDate.Date - CheckInDate.Date).Days : 1;
        public decimal RoomTotal => PricePerNight * Nights;
        public decimal CleaningFee { get; set; } = 350.00m;
        public decimal ServiceFee => Math.Round(RoomTotal * 0.12m, 2);
        public decimal TotalPrice => RoomTotal + CleaningFee + ServiceFee;
    }

    public class GuestCheckInViewModel
    {
        public int BookingId { get; set; }
        public string PropertyTitle { get; set; } = string.Empty;
        public string PropertyAddress { get; set; } = string.Empty;
        public string HostName { get; set; } = string.Empty;
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public CheckInStatus Status { get; set; }
        public string? HostRemarks { get; set; }

        public string ProcessTitle { get; set; } = string.Empty;
        public string ProcessStepsJson { get; set; } = string.Empty;

        public List<GuestDocument> UploadedDocuments { get; set; } = new List<GuestDocument>();

        [Required(ErrorMessage = "Please specify document type (e.g. National ID, Passport, Driver's License)")]
        [Display(Name = "Document Type")]
        public string DocumentType { get; set; } = "National ID / Passport";

        [Required(ErrorMessage = "Please select a verification document to upload")]
        [Display(Name = "Verification Document (PDF, JPG, PNG)")]
        public IFormFile? DocumentFile { get; set; }
    }
}
