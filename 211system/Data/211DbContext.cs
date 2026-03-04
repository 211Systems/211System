using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using FireDepartment;
using Police;
using CPR112.Models;
using _211system.Models.Hospital;
using System.Linq.Expressions;
public class _211DbContext : IdentityDbContext<IdentityUser>
{   
    public DbSet<Operator112> Operators { get; set; }
    public DbSet<Incident> Incidents { get; set; }
    public DbSet<Location> Locations { get; set; }
    public DbSet<Attachment> Attachments { get; set; }
    public DbSet<FinalReport> FinalReports { get; set; }
    public DbSet<NasaFlarePoint> NasaFlarePoints { get; set; }
    public DbSet<Enc> Encs { get; set; }

    public DbSet<FDepartment> FDepartments { get; set; }
    public DbSet<PoliceCar> PoliceCars { get; set; }
    public DbSet<Hospital> Hospitals { get; set; }  
    public DbSet<Ambulance> Ambulances { get; set; }
    public DbSet<AmbulanceEquipment> AmbulanceEquipments { get; set; }
    public DbSet<FireDepartmentOperation> FireDepartmentOperations { get; set; }
    public DbSet<PoliceOperation> PoliceOperations { get; set; }
    public DbSet<Fireman> Firemen { get; set; }
    public DbSet<DispatcherComment> DispatcherComments { get; set; }
    public DbSet<FireTruck> FireTrucks { get; set; }
    public DbSet<GivenMedicine> GivenMedicines { get; set; }
    public DbSet<Patient> Patients { get; set; }
    public DbSet<Paramedic> Paramedics { get; set; }
    public DbSet<HospitalWard> HospitalWards { get; set; }
    public DbSet<MedicalOperation> MedicalOperations { get; set; }
    public DbSet<PDepartment> PDepartments { get; set; }
    public DbSet<Policeman> Policemen { get; set; }
    public DbSet<PoliceEquipment> PoliceEquipments { get; set; }



    public _211DbContext(DbContextOptions<_211DbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureIdentityRelationship<Operator112>(modelBuilder, "IdentityUserId");
        ConfigureIdentityRelationship<Policeman>(modelBuilder, "IdentityUserId");
        ConfigureIdentityRelationship<Fireman>(modelBuilder, "IdentityUserId");
        ConfigureIdentityRelationship<Paramedic>(modelBuilder, "IdentityUserId");

        modelBuilder.Entity<PoliceOperation>()
            .HasKey(po => new { po.IncidentId, po.PolicemanId });
            
        modelBuilder.Entity<PoliceOperation>()
            .HasOne(po => po.Incident)
            .WithMany(i => i.PoliceOperations)
            .HasForeignKey(po => po.IncidentId);
            
        modelBuilder.Entity<PoliceOperation>()
            .HasOne(po => po.Policeman)
            .WithMany(p => p.PoliceOperations) 
            .HasForeignKey(po => po.PolicemanId);


        modelBuilder.Entity<FireDepartmentOperation>()
            .HasKey(fo => new { fo.IncidentId, fo.FiremanId });
            
        modelBuilder.Entity<FireDepartmentOperation>()
            .HasOne(fo => fo.Incident).WithMany(i => i.FireDepartmentOperations).HasForeignKey(fo => fo.IncidentId);
        modelBuilder.Entity<FireDepartmentOperation>()
            .HasOne(fo => fo.Fireman).WithMany(f => f.FireDepartmentOperations).HasForeignKey(fo => fo.FiremanId);

        modelBuilder.Entity<MedicalOperation>()
            .HasKey(mo => new { mo.IncidentId, mo.ParamedicId });
            
        modelBuilder.Entity<MedicalOperation>()
            .HasOne(mo => mo.Incident).WithMany(i => i.MedicalOperations).HasForeignKey(mo => mo.IncidentId);
        modelBuilder.Entity<MedicalOperation>()
            .HasOne(mo => mo.Paramedic).WithMany(p => p.MedicalOperations).HasForeignKey(mo => mo.ParamedicId);

        
        modelBuilder.Entity<Incident>()
            .HasOne(i => i.FinalReport)
            .WithOne(fr => fr.Incident)
            .HasForeignKey<FinalReport>(fr => fr.IncidentId)
            .OnDelete(DeleteBehavior.Cascade);


        modelBuilder.Entity<Policeman>()
            .HasOne(p => p.PDepartment)
            .WithMany() 
            .HasForeignKey(p => p.PDepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Fireman>()
            .HasOne(f => f.FDepartment)
            .WithMany()
            .HasForeignKey(f => f.FDepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Paramedic>()
            .HasOne(p => p.Hospital)
            .WithMany()
            .HasForeignKey(p => p.HospitalId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Incident>()
            .HasOne(i => i.Operator112)
            .WithMany()
            .HasForeignKey(i => i.Operator112Id)
            .OnDelete(DeleteBehavior.Restrict);
    }

 private void ConfigureIdentityRelationship<TEntity>(ModelBuilder builder, string foreignKeyName) 
    where TEntity : class
{
    builder.Entity<TEntity>()
        .HasOne<IdentityUser>()
        .WithMany()
        .HasForeignKey(foreignKeyName)
        .IsRequired(false) 
        .OnDelete(DeleteBehavior.SetNull); 
}
}