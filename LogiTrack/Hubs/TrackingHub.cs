using Microsoft.AspNetCore.SignalR;

namespace LogiTrack.Hubs
{
    public class TrackingHub : Hub
    {
        /// <summary>
        /// Broadcasts a driver's GPS location update to all connected clients
        /// watching the specified shipment.
        /// </summary>
        public async Task SendLocationUpdate(string shipmentCode, double lat, double lng, double bearing)
        {
            await Clients.All.SendAsync("locationUpdate", new
            {
                shipmentCode,
                lat,
                lng,
                bearing
            });
        }

        /// <summary>
        /// Allows a client to join a group for a specific shipment so they
        /// only receive updates relevant to that shipment.
        /// </summary>
        public async Task WatchShipment(string shipmentCode)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, shipmentCode);
        }

        /// <summary>
        /// Removes a client from a shipment watch group.
        /// </summary>
        public async Task UnwatchShipment(string shipmentCode)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, shipmentCode);
        }
    }
}
