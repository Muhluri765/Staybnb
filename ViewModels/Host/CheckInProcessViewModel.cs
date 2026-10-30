using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Staybnb.ViewModels.Host
{
    public class CheckInStepDto
    {
        public int StepNumber { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Instruction { get; set; } = string.Empty;
    }

    public class CheckInProcessViewModel
    {
        public int PropertyId { get; set; }
        public string PropertyTitle { get; set; } = string.Empty;

        [Required(ErrorMessage = "Check-in instructions title is required")]
        [StringLength(200)]
        [Display(Name = "Check-in Title / Policy Name")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please provide check-in instructions or steps")]
        [Display(Name = "Check-in Steps (JSON or structured text)")]
        public string StepsJson { get; set; } = string.Empty;

        public List<CheckInStepDto> Steps { get; set; } = new List<CheckInStepDto>();
    }
}
