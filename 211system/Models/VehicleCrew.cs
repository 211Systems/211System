using System;
using System.ComponentModel.DataAnnotations;

namespace _211system.Models
{
    public class VehicleCrew
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid VehicleId { get; set; }

        [MaxLength(20)]
        public string VehicleType { get; set; }

        public Guid MemberId { get; set; }

        [MaxLength(150)]
        public string MemberName { get; set; }
    }
}
