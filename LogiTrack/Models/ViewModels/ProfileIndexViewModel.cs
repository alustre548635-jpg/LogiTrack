using LogiTrack.Models;

namespace LogiTrack.Models.ViewModels
{
    public class ProfileIndexViewModel
    {
        public User User { get; set; }
        public Staff? Staff { get; set; }
        public Driver? Driver { get; set; }
        
        // Form models for updates
        public string? NewPassword { get; set; }
        public string? ConfirmPassword { get; set; }
        public string? CurrentPassword { get; set; }
    }
}
