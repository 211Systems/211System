using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using _211system.Models;

namespace Police
{
    public class PoliceCar
    {
        [Key]
        public Guid Id { get; set; }
        public string LicensePlate { get; set; }

        public Guid PDepartmentId { get; set; }
        public virtual PDepartment PDepartment { get; set; }

        public Guid? PolicemanId { get; set; }
        
        [ForeignKey(nameof(PolicemanId))]
        public virtual Policeman Policeman { get; set; }

        public Guid PoliceEquipmentId { get; set; }
        public virtual ICollection<PoliceEquipment> PoliceEquipment { get; set; } = new List<PoliceEquipment>();

        public bool IsAvailable { get; set; } = true;
        public Guid? CurrentIncidentId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public VehicleOperationalStatus Status { get; set; } = VehicleOperationalStatus.InBase;

        public PoliceCar()
        {
        }
    }
}