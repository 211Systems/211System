using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using _211system.Data;
using _211system.Models;
using _211system.Models.Aviation;
using _211system.Models.Hospital;
using _211system.Services;
using FireDepartment;
using Microsoft.EntityFrameworkCore;
using Police;
using Xunit;

namespace _211system.Tests
{
    public class HelipadHelperTests
    {
        private _211DbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<_211DbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new _211DbContext(options);
        }

        [Fact]
        public void ResolveHasHelipad_DeptFlagTrue_ReturnsTrue()
        {
            var airbases = new List<Airbase>();

            var result = HelipadHelper.ResolveHasHelipad(
                true, 52.1, 21.0, ServiceType.Police, airbases);

            Assert.True(result);
        }

        [Fact]
        public void ResolveHasHelipad_MatchingAirbase_ReturnsTrue()
        {
            const double lat = 52.2297;
            const double lng = 21.0122;

            var airbases = new List<Airbase>
            {
                new Airbase
                {
                    Id = Guid.NewGuid(),
                    Name = "Baza",
                    IcaoCode = "EPWA",
                    ServiceType = ServiceType.Medical,
                    Latitude = lat + 0.0001,
                    Longitude = lng - 0.0001
                }
            };

            var result = HelipadHelper.ResolveHasHelipad(
                false, lat, lng, ServiceType.Medical, airbases);

            Assert.True(result);
        }

        [Fact]
        public void ResolveHasHelipad_NoMatch_ReturnsFalse()
        {
            var airbases = new List<Airbase>
            {
                new Airbase
                {
                    Id = Guid.NewGuid(),
                    Name = "Baza",
                    IcaoCode = "EPKR",
                    ServiceType = ServiceType.Fire,
                    Latitude = 50.0,
                    Longitude = 19.0
                }
            };

            var result = HelipadHelper.ResolveHasHelipad(
                false, 52.2297, 21.0122, ServiceType.Police, airbases);

            Assert.False(result);
        }

        [Fact]
        public async Task SyncDepartmentHelipadAsync_Medical_SetsHasHelipad()
        {
            var context = GetInMemoryDbContext();
            var hospitalId = Guid.NewGuid();

            context.Hospitals.Add(new Hospital
            {
                Id = hospitalId,
                Name = "Szpital",
                Address = "ul. Test 1",
                HasSOR = true,
                Latitude = 52.1,
                Longitude = 21.0,
                HasHelipad = false
            });
            await context.SaveChangesAsync();

            await HelipadHelper.SyncDepartmentHelipadAsync(
                context, ServiceType.Medical, 52.1002, 21.0002);

            var hospital = await context.Hospitals.FindAsync(hospitalId);
            Assert.True(hospital!.HasHelipad);
        }

        [Fact]
        public async Task SyncDepartmentHelipadAsync_Police_SetsFlag()
        {
            var context = GetInMemoryDbContext();
            var deptId = Guid.NewGuid();

            context.PoliceDepartments.Add(new PDepartment
            {
                PDepartmentId = deptId,
                Name = "KPP",
                Address = "Adres",
                District = "Wwa",
                Latitude = 52.2,
                Longitude = 21.1,
                HasHelipad = false
            });
            await context.SaveChangesAsync();

            await HelipadHelper.SyncDepartmentHelipadAsync(
                context, ServiceType.Police, 52.2001, 21.1001);

            var dept = await context.PoliceDepartments.FindAsync(deptId);
            Assert.True(dept!.HasHelipad);
        }

        [Fact]
        public async Task SyncDepartmentHelipadAsync_Fire_SetsFlag()
        {
            var context = GetInMemoryDbContext();
            var deptId = Guid.NewGuid();

            context.FireDepartments.Add(new FDepartment
            {
                FDepartmentId = deptId,
                Name = "JRG",
                Address = "Adres",
                District = "Wwa",
                Latitude = 50.05,
                Longitude = 19.94,
                HasHelipad = false
            });
            await context.SaveChangesAsync();

            await HelipadHelper.SyncDepartmentHelipadAsync(
                context, ServiceType.Fire, 50.0503, 19.9402);

            var dept = await context.FireDepartments.FindAsync(deptId);
            Assert.True(dept!.HasHelipad);
        }
    }
}