using NfgoOrderApi.Common;
using NfgoOrderApi.DTOs;
using NfgoOrderApi.Models;
using NfgoOrderApi.Repositories;

namespace NfgoOrderApi.Services;

public interface IAssignmentService
{
    Task<IReadOnlyList<AssignmentDto>> GetAllAsync();
    Task<IReadOnlyList<EmployeeDto>> GetUnassignedEmployeesAsync();
    Task<AssignmentDto> CreateAsync(AssignmentRequest request);
    Task<AssignmentDto> UpdateAsync(int id, AssignmentRequest request);
    Task DeleteAsync(int id);
}

/// <summary>Привязка сотрудников к формированиям и ролям (а через роль — к нормам СИЗ).</summary>
public class AssignmentService(
    IAssignmentRepository assignments,
    IRepository<Employee> employees,
    IRepository<NfgoUnit> units,
    IRepository<NfgoRole> roles) : IAssignmentService
{
    public async Task<IReadOnlyList<AssignmentDto>> GetAllAsync() =>
        (await assignments.GetAllWithDetailsAsync()).Select(a => a.ToDto()).ToList();

    public async Task<IReadOnlyList<EmployeeDto>> GetUnassignedEmployeesAsync()
    {
        var assignedIds = (await assignments.GetAllAsync()).Select(a => a.EmployeeId).ToHashSet();
        return (await employees.GetAllAsync()).Where(e => !assignedIds.Contains(e.Id)).Select(e => e.ToDto()).ToList();
    }

    public async Task<AssignmentDto> CreateAsync(AssignmentRequest request)
    {
        await ValidateAsync(request, null);
        var entity = new NfgoAssignment
        {
            EmployeeId = request.EmployeeId, UnitId = request.UnitId, RoleId = request.RoleId
        };
        await assignments.AddAsync(entity);
        await assignments.SaveAsync();
        return (await assignments.GetWithDetailsAsync(entity.Id))!.ToDto();
    }

    public async Task<AssignmentDto> UpdateAsync(int id, AssignmentRequest request)
    {
        var entity = await assignments.GetWithDetailsAsync(id)
                     ?? throw new NotFoundException($"Назначение с id={id} не найдено.");
        await ValidateAsync(request, id);
        entity.EmployeeId = request.EmployeeId;
        entity.UnitId = request.UnitId;
        entity.RoleId = request.RoleId;
        await assignments.SaveAsync();
        return (await assignments.GetWithDetailsAsync(id))!.ToDto();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await assignments.GetByIdAsync(id)
                     ?? throw new NotFoundException($"Назначение с id={id} не найдено.");
        assignments.Remove(entity);
        await assignments.SaveAsync();
    }

    private async Task ValidateAsync(AssignmentRequest r, int? selfId)
    {
        if (await employees.GetByIdAsync(r.EmployeeId) is null)
            throw new BusinessRuleException($"Сотрудник с id={r.EmployeeId} не найден.");
        if (await units.GetByIdAsync(r.UnitId) is null)
            throw new BusinessRuleException($"Формирование с id={r.UnitId} не найдено.");
        if (await roles.GetByIdAsync(r.RoleId) is null)
            throw new BusinessRuleException($"Роль с id={r.RoleId} не найдена.");
        if (await assignments.AnyAsync(a => a.EmployeeId == r.EmployeeId && (selfId == null || a.Id != selfId)))
            throw new ConflictException("Этот сотрудник уже состоит в НФГО. Измените существующее назначение.");
    }
}
