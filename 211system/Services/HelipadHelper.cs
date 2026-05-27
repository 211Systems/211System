using _211system.Data;
using _211system.Models;
using _211system.Models.Aviation;
using Microsoft.EntityFrameworkCore;

namespace _211system.Services
{
    public static class HelipadHelper
    {
        private const double CoordTolerance = 0.0005;

        public static bool ResolveHasHelipad(bool deptFlag, double lat, double lng, ServiceType serviceType, IEnumerable<Airbase> airbases)
        {
            if (deptFlag) return true;
            if (lat == 0 && lng == 0) return false;

            return airbases.Any(a =>
                a.ServiceType == serviceType &&
                Math.Abs(a.Latitude - lat) < CoordTolerance &&
                Math.Abs(a.Longitude - lng) < CoordTolerance);
        }

        public static async Task SyncDepartmentHelipadAsync(_211DbContext context, ServiceType serviceType, double lat, double lng)
        {
            if (lat == 0 && lng == 0) return;

            var minLat = lat - CoordTolerance;
            var maxLat = lat + CoordTolerance;
            var minLng = lng - CoordTolerance;
            var maxLng = lng + CoordTolerance;

            switch (serviceType)
            {
                case ServiceType.Medical:
                    var hospital = await context.Hospitals
                        .Where(h => h.Latitude >= minLat && h.Latitude <= maxLat &&
                                    h.Longitude >= minLng && h.Longitude <= maxLng)
                        .FirstOrDefaultAsync();
                    if (hospital != null && !hospital.HasHelipad)
                    {
                        hospital.HasHelipad = true;
                        await context.SaveChangesAsync();
                    }
                    break;

                case ServiceType.Police:
                    var police = await context.PoliceDepartments
                        .Where(p => p.Latitude >= minLat && p.Latitude <= maxLat &&
                                    p.Longitude >= minLng && p.Longitude <= maxLng)
                        .FirstOrDefaultAsync();
                    if (police != null && !police.HasHelipad)
                    {
                        police.HasHelipad = true;
                        await context.SaveChangesAsync();
                    }
                    break;

                case ServiceType.Fire:
                    var fire = await context.FireDepartments
                        .Where(f => f.Latitude >= minLat && f.Latitude <= maxLat &&
                                    f.Longitude >= minLng && f.Longitude <= maxLng)
                        .FirstOrDefaultAsync();
                    if (fire != null && !fire.HasHelipad)
                    {
                        fire.HasHelipad = true;
                        await context.SaveChangesAsync();
                    }
                    break;
            }
        }
    }
}
