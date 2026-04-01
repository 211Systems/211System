using System.Collections.Generic;
using _211system.Models;
using _211system.Models.Hospital;
using CPR112.Models;
using FireDepartment;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Police;

namespace _211system.Data 
{
    public class _211DbContext : IdentityDbContext<ApplicationUser>
    {
        public _211DbContext(DbContextOptions<_211DbContext> options)
            : base(options)
        {
        }

        public DbSet<NasaFlarePoint> NasaFlarePoints { get; set; }

        // CPR 112
        public DbSet<Attachment> Attachments { get; set; }
        public DbSet<DispatcherComment> DispatcherComments { get; set; }
        public DbSet<Enc> Encs { get; set; } 
        public DbSet<FinalReport> FinalReports { get; set; }
        public DbSet<Incident> Incidents { get; set; }
        public DbSet<Operator112> Operators112 { get; set; }
        public DbSet<PeriodicReport> PeriodicReports { get; set; }
        public DbSet<StatusHistory> StatusHistories { get; set; }

        // Straz pozarna
        public DbSet<FDepartment> FireDepartments { get; set; }
        public DbSet<FireDepartmentOperation> FireOperations { get; set; }
        public DbSet<FireEquipment> FireEquipments { get; set; }
        public DbSet<Fireman> Firemen { get; set; }
        public DbSet<FireTruck> FireTrucks { get; set; }
        
        // Szpital
        public DbSet<Ambulance> Ambulances { get; set; }
        public DbSet<AmbulanceEquipment> AmbulanceEquipments { get; set; }
        public DbSet<GivenMedicine> GivenMedicines { get; set; }
        public DbSet<Hospital> Hospitals { get; set; }
        public DbSet<HospitalWard> HospitalWards { get; set; }
        public DbSet<MedicalOperation> MedicalOperations { get; set; }
        public DbSet<Paramedic> Paramedics { get; set; }
        public DbSet<Patient> Patients { get; set; }

        // Policja
        public DbSet<PDepartment> PoliceDepartments { get; set; }
        public DbSet<PoliceCar> PoliceCars { get; set; }
        public DbSet<PoliceEquipment> PoliceEquipments { get; set; }
        public DbSet<Policeman> Policemen { get; set; }
        public DbSet<PoliceOperation> PoliceOperations { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Relacja Operator112 -> Konto użytkownika
            builder.Entity<Operator112>()
                 .HasOne(o => o.OpAccount)
                 .WithMany()
                 .HasForeignKey(o => o.OpAccountId)
                 .OnDelete(DeleteBehavior.SetNull);

            // Relacja Fireman -> Konto użytkownika
            builder.Entity<Fireman>()
                .HasOne(f => f.FireAccount)
                .WithMany()
                .HasForeignKey(f => f.FireAccountId)
                .OnDelete(DeleteBehavior.SetNull);

            // Relacja Paramedic -> Konto użytkownika
            builder.Entity<Paramedic>()
                .HasOne(p => p.ParaAccount)
                .WithMany()
                .HasForeignKey(p => p.ParaAccountId)
                .OnDelete(DeleteBehavior.SetNull);

            // Relacja Policeman -> Konto użytkownika
            builder.Entity<Policeman>()
                .HasOne(p => p.PoliceAccount)
                .WithMany()
                .HasForeignKey(p => p.PoliceAccountId)
                .OnDelete(DeleteBehavior.SetNull);

            // POPRAWIONA RELACJA: Incident -> Enc (Placówka)
            // To rozwiązuje błąd FK_Incidents_Locations_LocationId
            builder.Entity<Incident>()
                .HasOne(i => i.Location) // Właściwość nawigacyjna w klasie Incident (typu Enc)
                .WithMany()
                .HasForeignKey(i => i.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            // Policja - Relacje
            builder.Entity<Policeman>()
                .HasOne(p => p.Department)
                .WithMany(d => d.Policemen)
                .HasForeignKey(p => p.PDepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PoliceOperation>()
                .HasOne(po => po.Department)
                .WithMany()
                .HasForeignKey(po => po.PDepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Straż - Relacje
            builder.Entity<Fireman>()
                .HasOne(f => f.Department)
                .WithMany(d => d.Firemen)
                .HasForeignKey(f => f.FDepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<FireDepartmentOperation>()
                .HasOne(fo => fo.Department)
                .WithMany()
                .HasForeignKey(fo => fo.FDepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Medycy - Relacje
            builder.Entity<Paramedic>()
                .HasOne(p => p.Hospital)
                .WithMany(h => h.Paramedics)
                .HasForeignKey(p => p.HospitalId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<MedicalOperation>()
                .HasOne(mo => mo.Paramedic)
                .WithMany(p => p.MedicalOperations)
                .HasForeignKey(mo => mo.ParamedicId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}