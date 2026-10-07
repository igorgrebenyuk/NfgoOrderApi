using NfgoOrderApi.Common;
using NfgoOrderApi.DTOs;
using NfgoOrderApi.Models;
using NfgoOrderApi.Repositories;

namespace NfgoOrderApi.Services;

public interface ICrudService<TDto>
{
    Task<IReadOnlyList<TDto>> GetAllAsync();
    Task<TDto> GetAsync(int id);
    Task<TDto> CreateAsync(TDto dto);
    Task<TDto> UpdateAsync(int id, TDto dto);
    Task DeleteAsync(int id);
}

/// <summary>Общий CRUD для простых справочников: маппинг и проверка «используется ли объект» — в наследниках.</summary>
public abstract class CrudService<TEntity, TDto>(IRepository<TEntity> repo) : ICrudService<TDto>
    where TEntity : class, IEntity, new()
{
    protected abstract string DisplayName { get; }
    protected abstract TDto ToDto(TEntity entity);
    protected abstract void Apply(TEntity entity, TDto dto);
    protected virtual Task<bool> IsInUseAsync(int id) => Task.FromResult(false);

    public async Task<IReadOnlyList<TDto>> GetAllAsync() =>
        (await repo.GetAllAsync()).Select(ToDto).ToList();

    public async Task<TDto> GetAsync(int id) => ToDto(await FindAsync(id));

    public async Task<TDto> CreateAsync(TDto dto)
    {
        var entity = new TEntity();
        Apply(entity, dto);
        await repo.AddAsync(entity);
        await repo.SaveAsync();
        return ToDto(entity);
    }

    public async Task<TDto> UpdateAsync(int id, TDto dto)
    {
        var entity = await FindAsync(id);
        Apply(entity, dto);
        await repo.SaveAsync();
        return ToDto(entity);
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await FindAsync(id);
        if (await IsInUseAsync(id))
            throw new ConflictException($"{DisplayName} используется и не может быть удалён(а).");
        repo.Remove(entity);
        await repo.SaveAsync();
    }

    private async Task<TEntity> FindAsync(int id) =>
        await repo.GetByIdAsync(id) ?? throw new NotFoundException($"{DisplayName} с id={id} не найден(а).");
}

public class EmployeeService(IRepository<Employee> repo) : CrudService<Employee, EmployeeDto>(repo)
{
    protected override string DisplayName => "Сотрудник";
    protected override EmployeeDto ToDto(Employee e) => e.ToDto();

    protected override void Apply(Employee e, EmployeeDto d)
    {
        e.FullName = d.FullName.Trim();
        e.Position = d.Position.Trim();
        e.Department = d.Department?.Trim();
        e.PersonnelNumber = d.PersonnelNumber?.Trim();
    }
}

public class UnitService(IRepository<NfgoUnit> repo, IRepository<NfgoAssignment> assignments)
    : CrudService<NfgoUnit, UnitDto>(repo)
{
    protected override string DisplayName => "Формирование";
    protected override UnitDto ToDto(NfgoUnit u) => u.ToDto();

    protected override void Apply(NfgoUnit u, UnitDto d)
    {
        u.Name = d.Name.Trim();
        u.Purpose = d.Purpose?.Trim();
    }

    protected override Task<bool> IsInUseAsync(int id) => assignments.AnyAsync(a => a.UnitId == id);
}

public class SizTypeService(IRepository<SizType> repo, IRepository<RoleSizNorm> norms)
    : CrudService<SizType, SizTypeDto>(repo)
{
    protected override string DisplayName => "Вид СИЗ";
    protected override SizTypeDto ToDto(SizType s) => s.ToDto();

    protected override void Apply(SizType s, SizTypeDto d)
    {
        s.Name = d.Name.Trim();
        s.Unit = d.Unit.Trim();
        s.Description = d.Description?.Trim();
    }

    // Вид СИЗ нельзя удалить, пока он указан в нормах какой-либо роли.
    protected override Task<bool> IsInUseAsync(int id) => norms.AnyAsync(n => n.SizTypeId == id);
}
