using NfgoOrderApi.Common;
using NfgoOrderApi.DTOs;
using NfgoOrderApi.Models;
using NfgoOrderApi.Repositories;

namespace NfgoOrderApi.Services;

public interface IRoleService
{
    Task<IReadOnlyList<RoleDto>> GetAllAsync();
    Task<RoleDto> GetAsync(int id);
    Task<RoleDto> CreateAsync(RoleDto dto);
    Task<RoleDto> UpdateAsync(int id, RoleDto dto);
    Task DeleteAsync(int id);
    /// <summary>Полностью заменить нормы СИЗ роли.</summary>
    Task<RoleDto> SetNormsAsync(int id, IReadOnlyList<RoleNormDto> norms);
}

public class RoleService(
    IRoleRepository roles,
    IRepository<SizType> sizTypes,
    IRepository<NfgoAssignment> assignments) : IRoleService
{
    public async Task<IReadOnlyList<RoleDto>> GetAllAsync() =>
        (await roles.GetAllWithNormsAsync()).Select(r => r.ToDto()).ToList();

    public async Task<RoleDto> GetAsync(int id) => (await FindAsync(id)).ToDto();

    public async Task<RoleDto> CreateAsync(RoleDto dto)
    {
        var role = new NfgoRole();
        Apply(role, dto);
        await roles.AddAsync(role);
        await roles.SaveAsync();
        return role.ToDto();
    }

    public async Task<RoleDto> UpdateAsync(int id, RoleDto dto)
    {
        var role = await FindAsync(id);
        Apply(role, dto);
        await roles.SaveAsync();
        return role.ToDto();
    }

    public async Task DeleteAsync(int id)
    {
        var role = await FindAsync(id);
        if (await assignments.AnyAsync(a => a.RoleId == id))
            throw new ConflictException("Роль назначена сотрудникам и не может быть удалена.");
        roles.Remove(role); // нормы удалятся каскадом
        await roles.SaveAsync();
    }

    public async Task<RoleDto> SetNormsAsync(int id, IReadOnlyList<RoleNormDto> norms)
    {
        var role = await FindAsync(id);

        if (norms.GroupBy(n => n.SizTypeId).Any(g => g.Count() > 1))
            throw new BusinessRuleException("Один вид СИЗ нельзя указать в нормах роли дважды.");
        if (norms.Any(n => n.Quantity < 1))
            throw new BusinessRuleException("Количество СИЗ в норме должно быть не меньше 1.");

        var known = (await sizTypes.GetAllAsync()).ToDictionary(s => s.Id);
        foreach (var n in norms)
            if (!known.ContainsKey(n.SizTypeId))
                throw new BusinessRuleException($"Вид СИЗ с id={n.SizTypeId} не найден.");

        role.Norms.Clear();
        foreach (var n in norms)
            role.Norms.Add(new RoleSizNorm { SizTypeId = n.SizTypeId, Quantity = n.Quantity });
        await roles.SaveAsync();

        // перечитываем вместе с SizType, чтобы вернуть названия
        return (await FindAsync(id)).ToDto();
    }

    private static void Apply(NfgoRole role, RoleDto dto)
    {
        role.Name = dto.Name.Trim();
        role.Description = dto.Description?.Trim();
        role.Priority = dto.Priority;
    }

    private async Task<NfgoRole> FindAsync(int id) =>
        await roles.GetWithNormsAsync(id) ?? throw new NotFoundException($"Роль с id={id} не найдена.");
}
