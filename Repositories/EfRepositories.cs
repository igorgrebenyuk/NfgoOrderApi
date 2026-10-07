using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using NfgoOrderApi.Data;
using NfgoOrderApi.Models;

namespace NfgoOrderApi.Repositories;

public class Repository<T> : IRepository<T> where T : class, IEntity
{
    protected readonly AppDbContext Db;
    protected readonly DbSet<T> Set;

    public Repository(AppDbContext db)
    {
        Db = db;
        Set = db.Set<T>();
    }

    public Task<List<T>> GetAllAsync() => Set.OrderBy(e => e.Id).ToListAsync();
    public Task<T?> GetByIdAsync(int id) => Set.FirstOrDefaultAsync(e => e.Id == id);
    public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate) => Set.AnyAsync(predicate);
    public async Task AddAsync(T entity) => await Set.AddAsync(entity);
    public void Remove(T entity) => Set.Remove(entity);
    public Task SaveAsync() => Db.SaveChangesAsync();
}

public class RoleRepository(AppDbContext db) : Repository<NfgoRole>(db), IRoleRepository
{
    public Task<List<NfgoRole>> GetAllWithNormsAsync() =>
        Set.Include(r => r.Norms).ThenInclude(n => n.SizType)
            .OrderBy(r => r.Priority).ThenBy(r => r.Id).ToListAsync();

    public Task<NfgoRole?> GetWithNormsAsync(int id) =>
        Set.Include(r => r.Norms).ThenInclude(n => n.SizType)
            .FirstOrDefaultAsync(r => r.Id == id);
}

public class AssignmentRepository(AppDbContext db) : Repository<NfgoAssignment>(db), IAssignmentRepository
{
    public Task<List<NfgoAssignment>> GetAllWithDetailsAsync() =>
        Set.Include(a => a.Employee)
            .Include(a => a.Unit)
            .Include(a => a.Role).ThenInclude(r => r!.Norms).ThenInclude(n => n.SizType)
            .OrderBy(a => a.Id).ToListAsync();

    public Task<NfgoAssignment?> GetWithDetailsAsync(int id) =>
        Set.Include(a => a.Employee)
            .Include(a => a.Unit)
            .Include(a => a.Role).ThenInclude(r => r!.Norms).ThenInclude(n => n.SizType)
            .FirstOrDefaultAsync(a => a.Id == id);
}

public class OrderRepository(AppDbContext db) : Repository<NfgoOrder>(db), IOrderRepository
{
    public Task<List<NfgoOrder>> GetAllWithMembersAsync() =>
        Set.Include(o => o.Members).OrderByDescending(o => o.OrderDate).ThenByDescending(o => o.Id).ToListAsync();

    public Task<NfgoOrder?> GetWithDetailsAsync(int id) =>
        Set.Include(o => o.Members).ThenInclude(m => m.SizItems)
            .FirstOrDefaultAsync(o => o.Id == id);

    public Task<bool> NumberExistsAsync(string number, int? excludeId = null) =>
        Set.AnyAsync(o => o.Number == number && (excludeId == null || o.Id != excludeId));
}
