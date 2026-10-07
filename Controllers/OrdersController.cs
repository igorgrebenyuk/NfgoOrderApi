using Microsoft.AspNetCore.Mvc;
using NfgoOrderApi.DTOs;
using NfgoOrderApi.Services;

namespace NfgoOrderApi.Controllers;

/// <summary>Приказы о создании НФГО: автоматическое формирование, статусы, выгрузка в Word.</summary>
[ApiController]
[Route("api/orders")]
public class OrdersController(IOrderService service) : ControllerBase
{
    private const string DocxContentType =
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderSummaryDto>>> GetAll() => Ok(await service.GetAllAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderDto>> Get(int id) => Ok(await service.GetAsync(id));

    /// <summary>
    /// Автоматически сформировать приказ: в него попадают все текущие назначения сотрудников,
    /// а СИЗ подтягиваются по нормам их ролей.
    /// </summary>
    [HttpPost("generate")]
    public async Task<ActionResult<GenerateResultDto>> Generate(OrderCreateRequest request) =>
        StatusCode(StatusCodes.Status201Created, await service.GenerateAsync(request));

    /// <summary>Пересобрать состав приказа по актуальным назначениям (только для статуса «Проект»).</summary>
    [HttpPost("{id:int}/regenerate")]
    public async Task<ActionResult<GenerateResultDto>> Regenerate(int id) => Ok(await service.RegenerateAsync(id));

    /// <summary>Сменить статус: Draft → Signed / Cancelled, Signed → Cancelled.</summary>
    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<OrderDto>> SetStatus(int id, OrderStatusRequest request) =>
        Ok(await service.SetStatusAsync(id, request.Status));

    /// <summary>Скачать приказ в формате Word (.docx).</summary>
    [HttpGet("{id:int}/document")]
    public async Task<IActionResult> Document(int id)
    {
        var (content, fileName) = await service.BuildDocumentAsync(id);
        return File(content, DocxContentType, fileName);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }
}
