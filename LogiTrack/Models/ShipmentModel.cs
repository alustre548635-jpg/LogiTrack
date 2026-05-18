using Microsoft.AspNetCore.Mvc;

namespace LogiTrack.Models
{
    public class ShipmentModel
    {
        public int Id { get; set; }
        public string TrackingNumber { get; set; }
        public string Origin { get; set; }
        public string Destination { get; set; }
        public int Quantity { get; set; }
        public decimal Weight { get; set; }
        public string Status { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
