using Microsoft.AspNetCore.Mvc;
using NfgoOrderApi.DTOs;
using NfgoOrderApi.Services;

namespace NfgoOrderApi.Controllers;

/// <summary>Назначения: сотрудник → формирование НФГО + роль.</summary>
[ApiController]
[Route("api/assignments")]
public class AssignmentsController(IAssignmentService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AssignmentDto>>> GetAll() => Ok(await service.GetAllAsync());

    /// <summary>Сотрудники, которые ещё не включены ни в одно формирование.</summary>
    [HttpGet("unassigned-employees")]
    public async Task<ActionResult<IReadOnlyList<EmployeeDto>>> Unassigned() =>
        Ok(await service.GetUnassignedEmployeesAsync());

    [HttpPost]
    public async Task<ActionResult<AssignmentDto>> Create(AssignmentRequest request) =>
        StatusCode(StatusCodes.Status201Created, await service.CreateAsync(request));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AssignmentDto>> Update(int id, AssignmentRequest request) =>
        Ok(await service.UpdateAsync(id, request));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }
}
