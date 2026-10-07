using System.Linq.Expressions;
using NfgoOrderApi.Models;

namespace NfgoOrderApi.Repositories;

public interface IRepository<T> where T : class, IEntity
{
    Task<List<T>> GetAllAsync();
    Task<T?> GetByIdAsync(int id);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
    Task AddAsync(T entity);
    void Remove(T entity);
    Task SaveAsync();
}

public interface IRoleRepository : IRepository<NfgoRole>
{
    Task<List<NfgoRole>> GetAllWithNormsAsync();
    Task<NfgoRole?> GetWithNormsAsync(int id);
}

public interface IAssignmentRepository : IRepository<NfgoAssignment>
{
    /// <summary>Назначения вместе с сотрудником, формированием, ролью и нормами СИЗ роли.</summary>
    Task<List<NfgoAssignment>> GetAllWithDetailsAsync();
    Task<NfgoAssignment?> GetWithDetailsAsync(int id);
}

public interface IOrderRepository : IRepository<NfgoOrder>
{
    Task<List<NfgoOrder>> GetAllWithMembersAsync();
    Task<NfgoOrder?> GetWithDetailsAsync(int id);
    Task<bool> NumberExistsAsync(string number, int? excludeId = null);
}
