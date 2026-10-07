using Microsoft.AspNetCore.Mvc;
using NfgoOrderApi.DTOs;
using NfgoOrderApi.Services;

namespace NfgoOrderApi.Controllers;

/// <summary>Базовый CRUD-контроллер для простых справочников. Ошибки переводит ExceptionHandlingMiddleware.</summary>
[ApiController]
public abstract class CrudController<TDto>(ICrudService<TDto> service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TDto>>> GetAll() => Ok(await service.GetAllAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TDto>> Get(int id) => Ok(await service.GetAsync(id));

    [HttpPost]
    public async Task<ActionResult<TDto>> Create(TDto dto) => StatusCode(StatusCodes.Status201Created, await service.CreateAsync(dto));

    [HttpPut("{id:int}")]
    public async Task<ActionResult<TDto>> Update(int id, TDto dto) => Ok(await service.UpdateAsync(id, dto));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }
}

/// <summary>Сотрудники.</summary>
[Route("api/employees")]
public class EmployeesController(ICrudService<EmployeeDto> service) : CrudController<EmployeeDto>(service);

/// <summary>Нештатные формирования (звенья, посты, группы).</summary>
[Route("api/units")]
public class UnitsController(ICrudService<UnitDto> service) : CrudController<UnitDto>(service);

/// <summary>Справочник видов СИЗ.</summary>
[Route("api/siz-types")]
public class SizTypesController(ICrudService<SizTypeDto> service) : CrudController<SizTypeDto>(service);
