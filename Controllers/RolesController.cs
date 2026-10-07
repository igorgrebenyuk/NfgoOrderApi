using Microsoft.AspNetCore.Mvc;
using NfgoOrderApi.DTOs;
using NfgoOrderApi.Services;

namespace NfgoOrderApi.Controllers;

/// <summary>Роли в НФГО и нормы выдачи СИЗ для каждой роли.</summary>
[ApiController]
[Route("api/roles")]
public class RolesController(IRoleService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RoleDto>>> GetAll() => Ok(await service.GetAllAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RoleDto>> Get(int id) => Ok(await service.GetAsync(id));

    [HttpPost]
    public async Task<ActionResult<RoleDto>> Create(RoleDto dto) =>
        StatusCode(StatusCodes.Status201Created, await service.CreateAsync(dto));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<RoleDto>> Update(int id, RoleDto dto) => Ok(await service.UpdateAsync(id, dto));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }

    /// <summary>Полностью заменить нормы СИЗ для роли (список { sizTypeId, quantity }).</summary>
    [HttpPut("{id:int}/siz-norms")]
    public async Task<ActionResult<RoleDto>> SetNorms(int id, List<RoleNormDto> norms) =>
        Ok(await service.SetNormsAsync(id, norms));
}
