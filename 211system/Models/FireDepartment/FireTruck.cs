using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FireDepartment
{
    public class FireTruck
    {
        [Key]
        public Guid Id { get; set; }
        public string LicensePlate { get; set; }

        public Guid FDepartmentId { get; set; }
        public virtual FDepartment Department { get; set; }

        public Guid? FiremanId { get; set; }
        
        [ForeignKey(nameof(FiremanId))]
        public virtual Fireman Fireman { get; set; }

        public Guid FireEquipmentid { get; set; }
        public virtual ICollection<FireEquipment> FireEquipment { get; set; } = new List<FireEquipment>();

        public bool IsAvailable { get; set; } = true;
        public Guid? CurrentIncidentId { get; set; }

        public FireTruck()
        {
        }
    }
}