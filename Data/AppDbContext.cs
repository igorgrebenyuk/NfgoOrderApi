using Microsoft.EntityFrameworkCore;
using NfgoOrderApi.Models;

namespace NfgoOrderApi.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<SizType> SizTypes => Set<SizType>();
    public DbSet<NfgoUnit> Units => Set<NfgoUnit>();
    public DbSet<NfgoRole> Roles => Set<NfgoRole>();
    public DbSet<RoleSizNorm> RoleSizNorms => Set<RoleSizNorm>();
    public DbSet<NfgoAssignment> Assignments => Set<NfgoAssignment>();
    public DbSet<NfgoOrder> Orders => Set<NfgoOrder>();
    public DbSet<NfgoOrderMember> OrderMembers => Set<NfgoOrderMember>();
    public DbSet<NfgoOrderMemberSiz> OrderMemberSizItems => Set<NfgoOrderMemberSiz>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<RoleSizNorm>(e =>
        {
            e.HasIndex(n => new { n.RoleId, n.SizTypeId }).IsUnique();
            e.HasOne(n => n.Role).WithMany(r => r.Norms).HasForeignKey(n => n.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(n => n.SizType).WithMany().HasForeignKey(n => n.SizTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<NfgoAssignment>(e =>
        {
            // один сотрудник — одно назначение
            e.HasIndex(a => a.EmployeeId).IsUnique();
            e.HasOne(a => a.Employee).WithMany().HasForeignKey(a => a.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.Unit).WithMany().HasForeignKey(a => a.UnitId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(a => a.Role).WithMany().HasForeignKey(a => a.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<NfgoOrder>(e =>
        {
            e.HasIndex(o => o.Number).IsUnique();
            e.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
            e.HasMany(o => o.Members).WithOne().HasForeignKey(m => m.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<NfgoOrderMember>(e =>
        {
            e.HasMany(m => m.SizItems).WithOne().HasForeignKey(s => s.OrderMemberId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
