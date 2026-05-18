# Real-World API Integrations Plan

Since your logistics system already has a solid foundation for managing Shipments, Routes, and Warehouses, integrating real-world APIs is the perfect next step to make it production-ready.

Based on the typical needs of a logistics platform, here are two high-value integration options.

## User Review Required

> [!IMPORTANT]
> **Please review the options below and let me know which one you'd like to implement first!**

### Option 1: Interactive Maps & Routing (High Visual Impact)
Currently, the Route Optimization page uses an AI-assisted mockup algorithm to calculate distances. We can integrate a real mapping API to show visual routes and calculate actual drive times.

**Proposed Integration: Leaflet.js with OpenStreetMap (Free) or Mapbox (Requires API Key)**
*   **What it does:** Replaces the static right side of the `Views/Route/Index.cshtml` page with a beautiful, interactive map. When you select a Start Hub and End Hub, it will draw the actual driving route on the map.
*   **Changes Required:** 
    *   Add Leaflet.js / Mapbox GL JS to `_ModuleLayout.cshtml`.
    *   Update `RouteController.cs` to fetch real geocodes (lat/long) for the Hubs.
    *   Draw the route polyline dynamically based on the selected route.

### Option 2: Automated Email Notifications (High Functional Impact)
Currently, the system relies on users actively logging in to check status. We can integrate an Email API to automatically notify Clients when their shipment status changes (e.g., from "Pending" to "In Transit").

**Proposed Integration: SendGrid or Standard SMTP**
*   **What it does:** Automatically sends a branded, professional HTML email to the Client associated with a Shipment whenever a new `TrackingEvent` is added or the `Shipment` status changes.
*   **Changes Required:**
    *   Create an `IEmailService` and `SendGridEmailService` in the `Services/` folder.
    *   Hook into the `ShipmentController` and `TrackingController` to trigger emails upon status updates.
    *   (Requires you to provide a SendGrid API Key or SMTP credentials).

## Open Questions

> [!QUESTION]
> 1. Do you prefer **Option 1 (Interactive Maps)** or **Option 2 (Email Notifications)** to start? 
> 2. If Option 1, do you have a Mapbox API Key, or would you prefer the free Leaflet/OpenStreetMap approach?
> 3. If Option 2, do you have a SendGrid account/API key ready?

## Proposed Changes (Example for Option 1 - Maps)

### Route Visualization Component
#### [MODIFY] `Views/Route/Index.cshtml`
- Add a `<div id="map"></div>` container to replace or sit alongside the summary stats.
- Write JavaScript to initialize Leaflet and draw markers for the Start Hub, Waypoints, and End Hub.
#### [MODIFY] `Controllers/RouteController.cs`
- Add a static dictionary mapping Hub Names to actual Coordinates (Latitude/Longitude) so the map knows where to place markers.

## Verification Plan

### Automated Tests
- Build and run the project to ensure no C# compilation errors.

### Manual Verification
- **For Maps:** Navigate to the Route Optimization page, select a shipment, and verify that the map renders, pins drop on the correct hubs, and a line is drawn connecting them.
- **For Emails:** Trigger a status change on a test shipment and verify the email arrives in the inbox.
